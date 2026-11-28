# Knowledge persona probe

Dependency-free Node.js 22+ operator tool. It signs the actual persona in
by device code for the portal's `access_as_user` scope, checks the real
application's team authorisation and sends goldens to the application's
real team-scoped retrieval boundary. It never exchanges tokens with
Graph/Search itself, impersonates a fixture user or sends an admin token
in place of that person. Tokens stay in memory; redirects are refused.

**Current limitation:** the application has no knowledge retrieval route.
The required operator fixture file is also absent. This tool does not
create that route, perform OBO, provision a knowledge base or establish
Foundry IQ acceptance. A 404 is recorded and stops the run; it is not
permission proof. ADR0046 records the mandatory spike before consumers.
Do not run a persona sign-in until approved fixtures and the real boundary
exist. No stub route is supplied.

The existing Azure CLI public client is already pre-authorised by the
greenfield portal hook for this exact API scope and allowed by Easy Auth.
Device code uses that client, not a new public-client mode on the
confidential portal app. This preserves the deployed OBO trust boundary.
See [DEPLOY](../../docs/DEPLOY.md#knowledge-persona-probe-round-2-adr0046)
for exact operator commands and source approval prerequisites.

The owner supplies private `goldens.json` with these fields:

| Field                                    | Required value                                                                                                                                                                                   |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `version`                                | `1`                                                                                                                                                                                              |
| `api`, `tenantId`, `portalClientId`      | Approved westus3 API origin (trailing slash), tenant and portal registration                                                                                                                     |
| `teamId`, `retrievalPath`                | Actual team and **implemented** `/api/teams/{teamId}/knowledge/ask` boundary; this route does not exist yet                                                                                      |
| `personas.positive`, `personas.negative` | Distinct actual Entra object ids, verified team members                                                                                                                                          |
| `knowledgeBases[]`                       | Approved shared/team bases, each with `id`, `questions[]`, `permission`                                                                                                                          |
| `questions[]`                            | At least forty owner-approved `{id, question, expectedSourceIds}` entries per base                                                                                                               |
| `permission`                             | `{restrictedSourceId, questionId, controlQuestionId, deniedMarkers}`; exact restricted document markers, positive restricted-source question and distinct readable control for the negative user |

No generated questions or example live personas are included. Save
goldens/reports privately outside tracked source; snippets can contain
information the persona is allowed to read. Never commit them or share
the negative user's restricted fixture markers as application input.
The probe sends only the question and knowledge base id.

The future real application's response must carry `knowledgeBaseId`,
`answer`, `noneFound`, `refusalReason` and `citations[]` containing
`sourceId`, `title`, `uri`, `snippet`, `lastModified` (timestamp or null).
This is a probe input contract, **not an existing API declaration**.
When the real spike chooses the supported response shape, keep this
parser consistent in the same implementation change.

Run tool-only checks:

```powershell
node --test tools\KnowledgeProbe\probe.test.mjs
node --check tools\KnowledgeProbe\probe.mjs
```

`validate` performs no network or sign-in. `probe` makes no direct
downstream calls, persists HTTP/refusal/latency evidence and stops on
the first failed, malformed or uncited retrieval without retries. `evaluate` rejects incomplete,
duplicate, changed-fixture or wrong-persona reports, measures correct citations per base,
checks negative response text and snippets against exact leakage markers,
and requires positive restricted-document proof plus a successful readable
negative control. Empty/error responses alone never establish trimming.
Malformed citations or fewer than90% correct sources fail retrieval.
Pre-brief added time and live G1 remain `not_run`: HTTP query latency
cannot stand in for either measurement.

Platform references checked 28 November 2026:
[device code](https://learn.microsoft.com/entra/identity-platform/v2-oauth2-device-code),
[public client requirement](https://learn.microsoft.com/entra/msal/dotnet/acquiring-tokens/desktop-mobile/device-code-flow),
[remote SharePoint](https://learn.microsoft.com/azure/search/agentic-knowledge-source-how-to-sharepoint-remote).
