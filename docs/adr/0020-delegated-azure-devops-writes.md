# 0020. Delegated Azure DevOps writes

Status: accepted. Portal path verified live in `zavaw3`; Teams path not verified (see "Measured and not measured"). Prompt 5A.

## Context

Until prompt 5A every approved change was written by the API's managed identity, so Azure DevOps showed the managed identity as the author of
the revision and only the comment text named the approver (ADR 0017). G3 asks for the approver to be the author, with the correlation id, for both
the portal and Teams. ADR 0015 S4 showed that a delegated user token creates and patches a work item with the user as creator and changer.

## Decision

1. **Write as the approver, with their own token.** `IWorkItemWriter.ApplyAsync` takes a `DelegatedAdoAccess` (an Azure DevOps access token for
   one person). `RoutedWorkItemWriter` builds a client that carries only that token (`IAdoClientFactory.DelegatedWriterFor`). The cached
   managed-identity writer is removed, so no write path can use the service identity. Reads, the scope check, the connection test and the demo
   reset stay on the managed identity: they are not approved-change writes.
2. **The rule is Domain code.** `ApproverAssertion.Check(approver, tokenOid, tokenTid)` refuses a write when there is no token
   (`identity.delegated_token_required`) or the token belongs to anyone other than the recorded approver (`identity.delegated_token_not_approver`).
   A retry by another person is therefore refused, and the follow-up stays approved and unapplied for the approver to retry.
3. **Early refusal.** `DelegatedAccessProvider` (Application) runs the rule and the on-behalf-of exchange. The apply handler uses it before any write,
   and `ApproveFollowUpHandler` and `ApprovePlanHandler` use it before recording a decision that has changes, so nothing is left approved but unwritable.
   Rejections need no token. Calendar approvals write to Microsoft Graph, not Azure DevOps, and are unchanged.
4. **On-behalf-of without a secret.** `OboAdoTokenSource` uses `OnBehalfOfCredential` with the portal app registration as the client and the API's
   user-assigned managed identity as the client assertion (a federated credential on the app registration, audience `api://AzureADTokenExchange`).
   Scope `499b84ac-1321-427f-aa17-267ca6975798/.default`. When the exchange fails the write is refused with a message that says what to do; nothing falls back.
5. **Portal.** `UserAssertionMiddleware` runs after authentication and gives the Application layer the validated bearer token and its `oid` and `tid`
   for that request. Nothing is stored.
6. **Teams.** Card actions that approve use Agents SDK user authorisation: handler `sso`, an Azure Bot OAuth connection of type
   "Azure Active Directory v2 with Federated Credentials" (no client secret), `autoSignInHandlers` on the `approve` and `approve-plan` routes. The agent reads
   the token with `GetTurnTokenAsync`, takes `oid` and `tid` from it (`SsoToken`), and the same rule and exchange apply. Without the connection configured there
   is no sign-in, so an approve is refused, not faked.
7. **No ACS secret.** The bridge's connection-string branch is deleted. It signs in to Azure Communication Services with Entra only.

## Additions after review against the full 5A requirements

- `IWorkItemWriter.ApplyAsync(change, target, DelegatedWriteContext, ct)`: the context carries the approver `ActorIdentity` and a token provider, and the port has one method (architecture test).
- The Azure DevOps client types are `internal` to Infrastructure; an architecture test proves nothing public exposes them and the Api project does not use them. A source scan guards the future agent host (no Azure DevOps credentials).
- A 401 or 403 from Azure DevOps on a write is `ado.approver_cannot_write`; Entra interaction, consent or Conditional Access answers become `delegation.interaction_required` with an instruction to sign in again. Neither falls back.
- VERIFY markers resolved against Learn: federated subject format and Bot Service Token Store id (`9ExAW52n_ky4ZiS_jhpJIQ`), tenant encoding (base64url of the GUID bytes, checked against the Learn MSIT example), provider "AAD v2 with Federated Credentials" (ARM provider id `Aadv2WithFic`), Teams client ids, `api://botid-<id>` and the redirect `https://token.botframework.com/.auth/web/redirect` (added to the hook; it was missing).
- Difference: Learn lets the bot OAuth connection use any single-tenant Entra app, so the portal app is used (not a second bot app); this still needs a live Teams run.

## Entra objects and hooks (same PR, greenfield)

- `entra-portal-app.ps1`: requests the Azure DevOps delegated scope `user_impersonation` (id looked up by name; the Azure DevOps service principal is created if the
  tenant has none), grants it for the organisation, adds the identifier URI `api://botid-<app id>`, pre-authorises the two Teams clients on `access_as_user`.
- `entra-post.ps1`: federated credential `zava-api-managed-identity` (subject: the managed identity's principal id), federated credential `zava-bot-token-store`
  (subject built from the tenant and the Bot Service token store application id, with the connection's unique id), the `sso` connection through ARM, and the
  agent settings (`AgentApplication__UserAuthorization__*`).
- `teams-app.ps1`: `webApplicationInfo` in the manifest.
- `verify.ps1`: fails unless the Azure DevOps comment author is the approver (G3).

## Differences from the prompt and from what I expected

- The prompt names Teams SSO for card actions. Learn says SSO for an Adaptive Card `Action.Execute` needs an Azure Bot OAuth connection, and that an AAD v2
  connection can use federated credentials instead of a secret. I used that. The exact federated subject format, the connection parameter names and the pre-authorised
  Teams client ids come from Learn search results and the Agents SDK reference and are marked `VERIFY` in the hooks.
- The bot is a user-assigned managed identity bot, so its id differs from the Entra app that holds the delegated permission. Teams manifests normally set
  `webApplicationInfo.id` to the bot's app id. Whether Teams accepts the portal app id here is **unverified**; if it does not, the Teams path needs a bot-identified
  app registration, which would add a second app that holds the same federated credential and consent.
- The Agents SDK `UserAuthorization.StartOrContinueSignInUserAsync` is not callable from an agent subclass; the supported route is `autoSignInHandlers` on the route, so a pending sign-in
  never reaches our handler.

## Consequences

- Every approver needs access to the project in Azure DevOps (a Contributor). This is a new human gate in `docs/DEPLOY.md`. A person without it gets Azure DevOps' own refusal, recorded on the follow-up.
- A write needs the approver present. A scheduled or system retry cannot apply an approved follow-up; the approver presses Retry. This is the point of the change.
- The portal's Easy Auth secret remains (Container Apps Easy Auth takes a secret only); it is not used for Azure DevOps.
- Known deferrals carried forward: the bridge still matches who is in the room by display name (presence only, never approval; prompt 6); roster verification runs at prepare, not at start.

## Measured and not measured

- Unit and Application tests: Domain 397, Application 148 (through the real mediator, fakes at the ports), Infrastructure 74, Architecture 7, Api 132, bridge 12, all passing, zero warnings.
- **Measured: the live portal write and G3.** `zavaw3` (westus3) after `azd up`: `verify.ps1` 7 of 7. The Azure DevOps comment on #1 is authored by the approving user (`admin@`), ends `Correlation: zsa-466f0ef309e5`, and the audit chain is complete (`docs/gates/m11-p5a-logs/verify-pass.log`). The first attempt stopped because the deployer had lost its Entra write roles (restored by the owner); the next stopped at the bot OAuth connection, whose provider name must be `Aadv2WithFic` (fixed in the hook).
- **Measured: latency.** Deployed, three runs, n 32: total p50 3806 ms, p95 4626 ms, against 3439 to 3470 ms p50 in westus3 before (about +10 %). The change adds nothing to a voice turn and the same build drifted about 25 % between windows in 3B, so it is recorded as within drift, not as proven equal.
- **Not measured: Teams SSO, a live refusal for a second approver without write access, a second Azure DevOps organisation.**