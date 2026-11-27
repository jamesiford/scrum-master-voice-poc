# ADR 0027: Work IQ stays behind a delegated advisory evidence boundary

Date: 2026-11-26
Status: Accepted contract and design; live integration blocked

**27 November 2026 redo:** ADR0034 records verified AllPrincipals
WorkIQAgent.Ask consent and matching declarations, n=1 each. Genuine
administrator OBO exchanges n=2 and MCP initialisations n=2 succeed;
tools/list n=1 returns HTTP 200 and 12 tools. The complete discovery
probe took 9.352 s, not content retrieval latency, using the existing
Easy Auth credential only in memory. Real request-local MI-OBO and
metadata-discovery adapters are built but unregistered, and deployed
managed-identity federation is unproved. Approved Copilot Credits
assignment, named lead, exact channel/meeting UID and negative-user
fixtures remain open. G11 content, scoping, leakage and latency are
not_run, n=0; consent and administrator sign-in must not be reopened.

**20:00PT operator update:** Scout granted and read back actual zavaw3
portal AllPrincipals WorkIQAgent.Ask with matching declaration.
WORK-SIGNALS-CONSENT is completed; no Act/settings/application permission.
Billing/spending owner, consenting lead, scoped fixtures and live content
retrieval remain open, n=0. Earlier absence of grant below is historical. ADR0031 preserves
the approved declaration without enabling the unbuilt signal adapter.

## Evidence and current Learn

Prompt 8B does not depend on the unbuilt Foundry IQ layer. Read the current
[API overview](https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/api-overview),
[MCP overview](https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/mcp/overview),
[permissions](https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/permissions),
[tenant setup](https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/enable-work-iq),
[tool reference](https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/mcp/tool-reference)
and [retrieve contract](https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/mcp/retrieve)
on 26 November 2026, before coding.

Work IQ supports user-delegated OBO, not application-only authentication.
An agent may orchestrate a delegated request; an agent identity is not a
replacement for the person's token. This is a documented restriction, not
an app-only experiment. Work IQ uses its own resource
`api://workiq.svc.cloud.microsoft`, not the missing Graph retrieval scopes
from 8A. `retrieve` currently requires admin-consented `WorkIQAgent.Ask`
(`0b1715fd-f4bf-4c63-b16d-5be31f9847c2`). Despite its name, that permission
includes read and write access. `WorkIQ.Read` exists but Learn does not
establish that it authorises `retrieve`. Do not substitute it or request
`WorkIQ.Act` or settings permissions. The MCP overview's four-permission
description differs from the three entries in the permission reference and
the six scopes actually exposed by this tenant; discover rather than guess.

Historical read-only Graph metadata from the original 8B attempt established:

- Work IQ service principal already exists: app
  `fdcc1f02-fc51-4226-8753-f668596af7f7`, object
  `ca4a440d-9d38-41a8-a140-f5ff230ac975`; six delegated scopes, no app roles.
- The actual zavaw3 portal app `265790b8-88a7-4975-bb8f-3fe381bb19ea`
  declares only Graph `User.Read` and Azure DevOps `user_impersonation`.
  Its service principal `2b69cbf8-a7e7-432c-8441-726b72a9fffc` has grants
  for Graph `User.Read openid profile`, Azure DevOps `user_impersonation`
  and its own `access_as_user`, **no Work IQ grant**.
- The earlier administrator's Copilot licence is established, but no named
  consenting team lead, current owner assertion, Work IQ spending-policy
  assignment or team channel/meeting positive and negative fixture is
  supplied. Do not infer these from a licence or tenant service principal.
  The authorised admin window ended at 14:27 PT before this session.

Tenant setup now requires usage-based Copilot Credits billing and assignment
of the calling user to a spending policy including Work IQ API. A Copilot
licence alone is not enough. No billing plan, policy, permission, role or
resource was changed. Explicit Graph resource was used on all reads.

Original 8B spike OBO calls **n=0**, channel questions **n=0**, helper questions **n=0**,
meeting-note questions **n=0**, negative-user/private-source checks **n=0**.
Latency and G11 leakage are not measured, not zero or passing.
There is no invented authentication failure: prerequisite metadata succeeded.

## Decision and independent implementation

Complete the operator-requested independent contract, not a fake shipped
adapter. Domain `WorkSignalConsent` records the team's named lead and
connection/revocation times without credentials. `WorkSignalAccess` checks
the default-off team flag, deployment tenant, exact current owner assertion,
current team-lead membership and revocation. Admin access never substitutes
for owner membership. Existing roles have TeamLead, not a separate Scrum
Master role; the Scrum Master must hold that team role.

Application owns typed `IWorkSignals` with channel activity, helper evidence
and existing meeting notes, plus consent, source-scope and short-reference
retention ports. `WorkSignalReader` reuses `ITeamResolver.ForUserAsync` and
`IUserAssertionAccessor`, not the cached service reader. It refuses before
scope/provider calls when disabled, revoked, unsigned or unauthorised; checks
board/meeting ids; rechecks consent and membership after provider completion;
returns source-labelled evidence only. The excerpt policy rejects wrong
channel/group/meeting/source/roster metadata, non-HTTPS links, future/stale
evidence and oversized snippets. Helpers are distinct evidenced roster
identities, at most three, never assignments. Provider failures remain typed
failures, distinct from a genuine successful empty response.

These contracts and the reader are **not registered in production DI**.
There is no new production command/query, endpoint, portal page, store
implementation, credential cache, scheduler or content integration.
ADR0034 adds unregistered real MI-OBO/metadata-discovery adapters, not a
content provider or an enabled signal runtime.
Tests register only a test presentation query in the real mediator and fake
the external ports; the actual reader, rules and pipeline execute. This is
contract evidence, not a golden pass for the four live product integrations.
The default-off field remains reserved in the deployed team registry.
No `IKnowledge` or 8A citation is assumed.

## Resume design, privacy and scope

The [privacy note](../WORK-SIGNALS-PRIVACY.md) defines the owner, visibility,
revocation and retention contract. An Admin enables the team; the named lead
connects using their own portal assertion. Show who, when and Revoke.
Connection metadata alone does not authorise future requests: a fresh
validated owner assertion is required. No scheduled background read or
refresh under another identity; missing/expired tokens show a banner.
Any future delegated cache must be encrypted, keyed by tenant/team/owner and
connection, and invalidated on revoke, removal or role change.

Consent and live `tools/list` discovery are complete. Obtain DEPLOY.md's
remaining billing and real fixture gates, rediscover evolving schemas and
verify a server-enforced selected channel and
meeting scope before querying. A `TeamsMessages` capability alone may include
private chats; filtering after retrieval does not meet the no-private-read
requirement. If exact pre-retrieval scoping is unsupported, do not ship that
use. Do not call broad `ask`, `fetch /me/messages`, private-chat paths or any
mutation tool. `retrieve` returns
`structuredContent["application/vnd.ms-workiq.retrieval"].results`, not
top-level Markdown or the `content` array. Both MCP `isError` and terminal
`stoppedReason` must be checked. Missing/blocked protection metadata is not an
allow decision. Use structured source ids, never parse model Markdown as ids.

Build only uses the spike proves: linked pre-brief evidence; a channel update
from the exact absent/silent member before changing chase wording; up to three
roster helper suggestions requiring a human choice; review notes converted
into cited `CreateWorkItem` proposals in the existing ceremony follow-up.
No evidence alone writes to Azure DevOps. Review conversion, M9c closure,
cards and UI remain unbuilt until the live excerpt/provenance fixture passes.

Persistence is a separate command, not a read-query side effect. Store only
team/owner/correlation, verified references and bounded excerpts with an
expiry; configurable positive retention at most thirty days and snippets
at most one thousand characters. Implement purge on expiry and revoke.
All runtime adapters, optional consent declarations, scope configuration,
store roles and lifecycle hooks must accompany the resumed greenfield PR.
No broad permission is automatically granted by this contract-only step.

## Consequences and gates

G11 is blocked at its prerequisites, not failed by a measured leakage sample.
Local consent/revoke and real-mediator boundary tests are separate from live
acceptance; counts and build evidence are recorded in M11-gates.md.
G1 has no new turns because the voice/runtime paths are unchanged.
The original contract-only attempt made no deployment. ADR0034's real
metadata adapters are included in API image `git-3d24f8e`, revision 85,
deployed only to zavaw3 with unchanged configuration. Baseline verify
passed 8/8; after verify initially passed 7/8 because external governance
deallocated the native VM. One scoped restart and focused media check
passed 5/5, n=1; this is not a second full 8/8 run or native acceptance.
Existing zavaw3 stays running; Acs/Share/Direct,
native non-recording and inherited failed gates are preserved.
Added fixed resource cost **USD 0/hour**, Work IQ content retrieval calls
**n=0**. Successful metadata discovery does not prove billing approval or
that all existing compute/model consumption is free.
