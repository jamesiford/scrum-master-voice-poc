# ADR 0033: Foundry IQ consent redo and real fixture boundary

Date: 2026-11-27
Status: Accepted boundary; mandatory retrieval spike remains blocked

## Context and measured evidence

Conductor8A-r starts on m11-p11a from m11-p10, not the published m11-p8a
branch. ADR0026's original absent-consent evidence is historical.

Actual Graph readback confirms the zavaw3 portal service principal
`2b69cbf8-a7e7-432c-8441-726b72a9fffc` has AllPrincipals delegated
`User.Read openid profile Files.Read.All Sites.Read.All` (n=1 read).
The matching application declarations are present (n=1 read). The separate
Work IQ grant remains `WorkIQAgent.Ask`; no permission was changed.
The inherited greenfield hook merges requiredResourceAccess and preserves
these approved declarations rather than replacing them with its baseline.
It does not enable knowledge or consent optional scopes in other environments.

A real Azure CLI sign-in obtains an access token for the portal application's
`access_as_user` audience, naming administrator object
`aeaec64b-d720-4996-9022-02fa0dcf043d` in the correct tenant. A current licence
metadata read confirms this caller has Microsoft365Copilot (n=1).
These are prerequisite measurements, not permission-trimming evidence.

A genuine OBO exchange succeeds for this portal caller (n=1), requesting
Graph Files.Read.All/Sites.Read.All/User.Read. A Graph `/me` identity-only
read through that issued token returns the same caller object id (n=1).
The bounded probe ran08:23:16.961Z to08:23:22.323Z,5.362s total including
credential acquisition and metadata, **not retrieval latency**.
It reused the existing portal Easy Auth confidential-client credential in
memory, without logging or persisting tokens or credentials. This validates
the portal app's delegated consent and this administrator's token exchange,
not the deployed managed-identity federation, source permissions or a normal
member's access. No secret-based adapter is added to a shipped path.

The actual application registry returns five teams, each with no knowledge
base configured and zero stored memberships. The exact zavaw3 resource group
has no Search service (n=1 inventory). No approved organisation/team
SharePoint source, Blob corpus, restricted document, two consenting entitled
members or source-labelled questions is established. Tenant users, demo cast
names and root-site access are not interchangeable with that fixture.

## Current Learn contract

Reviewed on27November2026, not exercised as a knowledge base:

- [Foundry IQ](https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-foundry-iq):
  some capabilities are GA; version-specific availability wins over a blanket
  preview claim.
- [Remote SharePoint](https://learn.microsoft.com/azure/search/agentic-knowledge-source-how-to-sharepoint-remote):
  Search `2026-11-01-preview`, same-tenant delegated caller, Copilot licence
  or approved pay-as-you-go. Invalid KQL is ignored, so KQL alone is not
  a team authorisation boundary. Remote content has no Search ingestion.
- [Agent connection](https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect):
  MCP `/knowledgebases/{name}/mcp?api-version=2026-11-01-preview`,
  `knowledge_base_retrieve`, ARM connection `2025-10-01-preview`,
  `RemoteTool`/`ProjectManagedIdentity` and Search Index Data Reader.
  Service authentication and source-user authorisation remain distinct.
- [OBO](https://learn.microsoft.com/entra/identity-platform/v2-oauth2-on-behalf-of-flow):
  the incoming user token must target the confidential middle tier; delegated
  scopes do not become application permissions.

The separate foundry-iq skill is unavailable; current Learn was read directly.
No SDK or live knowledge API version is invented.

## Decision

Close KNOWLEDGE-CONSENT, but retain KNOWLEDGE-FIXTURE. Do not provision an
empty or tenant-wide knowledge base merely to simulate the required spike,
ingest unapproved documents, create people, enable billing or call the
administrator a denied user. The real admin's successful delegated token
acquisition cannot establish positive/negative retrieval or member-level trimming.

No permission-trimming request has failed: retrieval questions n=0,
knowledge-MCP calls n=0, denied-user queries n=0 and golden questions n=0.
Citation correctness, uncited answer rate, leakage and retrieval latency
are not measured. Absence of a source is not a successful "I don't know"
knowledge answer; no answer or citation is shipped.

All dependent consumers remain unbuilt until the actual source/permission
spike succeeds. This follows the spike-first contract, not a stop-gap adapter.
DEPLOY.md names the exact owner actions. Resume with approved source scope,
real identities and ten real questions, followed by forty expected-document
questions per knowledge base. Build infrastructure, source/connection hooks,
team-scoped resolution, cited consumers and evaluation in the same resumed PR.

## Consequences

No `IKnowledge`, Knowledge page, standards chat skill, card enrichment,
Story Writer standards or retrospective agreements are claimed implemented.
Advisory knowledge never becomes a proposal, item link or state change;
the existing approval invariant is unchanged. Knowledge remains off live voice.
Acs/Share/Direct, native non-recording, stable hosted4 and inherited failed
G1/G4/G7/G8 remain unchanged. Added resources and fixed cost: none,
USD0/hour; knowledge retrieval consumption n=0.
