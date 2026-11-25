# 0020. Delegated Azure DevOps writes

Status: accepted for the code and the hooks; not yet verified live (see "Measured and not measured"). Prompt 5A.

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
- **Not measured: the live portal write and G3.** The `zavaw3` deployment was attempted from a clean commit and stopped at `entra-portal-app.ps1`: the signed-in deployer
  `admin@` now holds only Global Reader, Agent ID Administrator, Agent ID Developer and Agent Registry Administrator, so any application update returns
  `Authorization_RequestDenied` (it fails on a no-op patch of the app registration too). Nothing was provisioned or changed in Azure. The Entra roles the deployer needs are in `docs/DEPLOY.md`.
- **Not measured: Teams SSO, a second Azure DevOps organisation, latency.** The change adds no work to the voice turn, but the gate harness was not re-run against a deployment, so no before and after numbers are claimed.
