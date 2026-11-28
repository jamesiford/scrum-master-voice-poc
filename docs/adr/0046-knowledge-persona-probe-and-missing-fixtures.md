# ADR 0046: Knowledge persona probe without fabricated fixtures

Date: 2026-11-28
Status: Accepted independent tooling; retrieval and consumers blocked

## Evidence

The full round2 prompt and prior8A/8A-r results were read. The operator's
required `C:\Projects\m11-conductor\fixtures\8a-fixtures.md` does not exist.
No approved sources, real positive/negative members, restricted document,
ten spike questions or forty source-labelled goldens per base are supplied.
The existing application has no knowledge retrieval route or `IKnowledge`.

Actual Graph readback confirms organisation-wide delegated
`User.Read openid profile Files.Read.All Sites.Read.All` on the zavaw3
portal. The matching declarations are present; current Graph service
principal metadata confirms both scope ids used by the hook. Consent is
complete, not a new human gate. No grants or public-client settings were
changed. The API remains revision102, Running/ready, digest-pinned
`git-abdd4ff`; native VM remains deallocated.

## Decision

Keep ADR0026's mandatory caller-OBO remote SharePoint/Blob/MCP spike before
dependent knowledge consumers. Do not substitute repo documents, direct
administrator Graph reads, fabricated citations or a success-shaped stub
for source-owner approval and actual delegated permission proof.

Build `tools/KnowledgeProbe` independently. Device code authenticates the
real person for the portal API scope through the existing pre-authorised
Azure CLI public client. Making the confidential portal registration a
public client merely for this test would be an unnecessary trust change.
The tool checks the persona's app-audience assertion and then the real
application's authenticated team route; local JWT decoding is not signature
validation. It never obtains a direct Graph token or performs OBO itself.
The future real knowledge API must use the person's assertion and the
deployed managed-identity federation, not a token supplied as a body field.

The reserved probe path `/api/teams/{teamId}/knowledge/ask` is **not an
implemented API**. Until the real spike/boundary exists, the probe cannot
exercise retrieval; a 404 stops and records the missing boundary. Do not
tell the operator that signing in now will prove OBO or permissions.
The owner-supplied private fixture format requires forty questions per base,
expected source ids, restricted-document markers and a readable negative
control. Reports are pinned to the exact fixture SHA256. The evaluator
refuses incomplete/duplicate/wrong-persona results, fails uncited answers
(including misleading `noneFound` answers), checks exact leakage markers,
and requires positive restricted-source proof plus successful negative
retrieval/control. Marker checks do not establish absence of paraphrased
leakage; the source owner must also review the full negative output.
Query HTTP latency does not stand in for pre-brief cost or live G1.

Greenfield reconciliation now declares both delegated Graph scopes on an
empty registration as well as preserving existing declarations. The
explicit, administrator-approved environment option
`KNOWLEDGE_DELEGATED_READS_ENABLED=true` adds the matching grant
idempotently. Its default does not grant new retrieval consent; existing
approved scopes are never revoked. This infrastructure consent option is
not a per-team knowledge runtime flag and does not enable content calls.

## Current platform contract

Current Learn was searched and read in full for the relevant contracts:
[device code](https://learn.microsoft.com/entra/identity-platform/v2-oauth2-device-code),
[public client](https://learn.microsoft.com/entra/msal/dotnet/acquiring-tokens/desktop-mobile/device-code-flow),
[remote SharePoint](https://learn.microsoft.com/azure/search/agentic-knowledge-source-how-to-sharepoint-remote).
The separate `foundry-iq` plugin is unavailable; the azure-ai skill redirects
there, so current Learn was used directly. The Learn MCP fetch failed once
to retrieve the device-code page; the official full page fetched directly
successfully. This was not an authentication failure or a reason to stop.

Remote SharePoint still documents Search2026-11-01-preview, per-user
Copilot licence or approved pay-as-you-go and ignored invalid KQL filters.
Therefore a KQL filter alone cannot authorise a team/source. The reviewed
contracts are not versions exercised by this step. No Search/Blob/MCP
resource, source connection or content adapter was provisioned.

## Consequences and measured boundary

Sixteen focused tool cases and seven pure consent cases/two hook parsers
pass. All three builds pass; .NET has zero warnings/errors. No full suites
were run. G10 retrieval/citation/permission/evaluation/pre-brief samples
remain **not_run,n0**, not passing zero leakage. The tool-only synthetic
records are clearly labelled and are not customer goldens.

`IKnowledge`, shared/team resolver, source-management portal, four cited
integrations, per-team runtime flag and permission-aware infrastructure
remain dependent on the real spike. No new runtime was deployed: default
Acs/Share/Direct, Simulated Room and approval/item-id invariants remain
unchanged. Added fixed cost **USD0/hour**; knowledge usage **n0**.
[DEPLOY](../DEPLOY.md#knowledge-persona-probe-round-2-adr0046)
names the exact owner actions and executable commands after prerequisites.
The result remains blocked only by genuine operator/source/persona inputs;
this is not a gate-failure excuse or a renewed consent request.
