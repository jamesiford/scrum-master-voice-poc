# ADR 0026: Foundry IQ must pass the delegated permission spike first

Date: 2026-11-26
Status: Accepted prerequisite; implementation blocked, not rejected

**20:00PT operator update:** Scout granted and read back the actual zavaw3
portal Graph AllPrincipals Files.Read.All/Sites.Read.All permissions with
matching declarations. KNOWLEDGE-CONSENT is completed, not an open human
gate. Approved corpus/two-user entitlement/permission fixture and the actual
OBO spike remain open, n0. Evidence below is historical. ADR0031 preserves
these declarations during baseline hook reconciliation without enabling IQ.

## Context and evidence

Prompt 8A requires remote SharePoint and indexed Blob retrieval through a
knowledge base MCP endpoint, ten real spike questions and a permission-negative
user before integration. The session began at 16:33 PT, after the explicitly
authorised Global Administrator window ended at 14:27 PT. No renewed privilege
or interactive user sign-in is available to this unattended session.

Read-only live checks against zavaw3 and its tenant established:

- The SharePoint root is reachable as the signed-in Azure CLI user (HTTP 200).
  This proves neither the portal application's OBO access nor document trimming.
- The signed-in administrator has `Microsoft_365_Copilot` in `licenseDetails`.
  This does not establish a second user's entitlement or pay-as-you-go billing.
- The portal application's declared Graph delegated permission is `User.Read`.
  Its actual Graph AllPrincipals grant is `User.Read openid profile`.
  Neither declaration nor grant includes `Files.Read.All` or `Sites.Read.All`.
  The existing Azure DevOps grant is separate and cannot authorise SharePoint.
- The exact resource group `rg-zava-scrum-zavaw3` contains 25 resources and no
  `Microsoft.Search/searchServices`. No approved standards corpus, source
  scope, document-level denied-user fixture or signed-in second user has been
  established. The SharePoint root is not an approved organisation-wide source.
- The existing API remains image `git-d041890`, latest and ready revision
  `--0000082`; its health endpoint returns HTTP 200. No resources, Entra grants,
  application configuration or runtime were changed.

Retrieval questions **n=0**, OBO retrieval requests **n=0**, permission-negative
queries **n=0**, golden evaluations **n=0**. Correct citation rate, uncited answer
rate, leakage and retrieval latency are **not measured**, not zero or passing.
This is a prerequisite block, not an observed permission-trimming failure.
No authentication failure is invented: the metadata reads succeeded.

## Current Learn contract

Read on 26 November 2026:

- [Foundry IQ](https://learn.microsoft.com/azure/foundry/agents/concepts/what-is-foundry-iq):
  capability availability depends on API version; not everything remains preview.
- [Remote SharePoint](https://learn.microsoft.com/azure/search/agentic-knowledge-source-how-to-sharepoint-remote):
  remote retrieval remains preview and currently documents Search REST
  `2026-11-01-preview`. It uses the Copilot Retrieval API, not a synchronised
  Search index. Each user needs Copilot licensing or explicitly enabled
  pay-as-you-go. Invalid KQL can be ignored, so a filter alone is not a team
  authorisation boundary.
- [Connect to agents](https://learn.microsoft.com/azure/foundry/agents/how-to/foundry-iq-connect):
  MCP endpoint `/knowledgebases/{name}/mcp?api-version=2026-11-01-preview`;
  ARM project connection `2025-10-01-preview`, category `RemoteTool`,
  authentication `ProjectManagedIdentity`. The project identity requires
  Search Index Data Reader; a model-backed Search service also needs access
  to the Foundry model. Source-user authorisation is separate from this
  service-to-service authentication.
- [Retrieval API](https://learn.microsoft.com/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval):
  SharePoint requires **both delegated `Files.Read.All` and `Sites.Read.All`**.
  Application permissions are not supported. The reference now documents
  both Graph `v1.0` and `beta`; do not assume every retrieval API is preview.

These are versions reviewed, not versions exercised in a live knowledge base.
The `azure-ai` skill redirects Foundry IQ to a separate `foundry-iq` plugin;
that skill is not available in this session. Current Learn was read directly.

## Decision

Stop before provisioning or shipping the dependent knowledge implementation.
Do not substitute Azure CLI access, an admin token, application permissions,
repo Markdown, fabricated citations or Blob-only retrieval for the required
caller-OBO permission proof. Do not label absence of a query as a successful
zero-leakage test. DEPLOY.md names the exact consent, corpus and user gates.

After those prerequisites, resume prompt 8A in two stages: first create the
real Search/source/knowledge base infrastructure through Bicep and idempotent
hooks, connect MCP and prove the ten-question and denied-user spike; then
implement the team-scoped port, portal and four integrations with at least
40 source-labelled questions per knowledge base. All runtime, roles, source
configuration and connections belong in the same greenfield templates/hooks.
If genuine OBO permission trimming fails, report that failure and do not ship.

## Consequences

`KnowledgeBaseIds` remains reserved. No `IKnowledge`, Knowledge page, standards
chat skill, knowledge-enriched card, Story Writer rule citation or retrospective
working-agreement list is implemented. Knowledge stays off the live voice path.
Advisory answers must never create item links, proposals or changes; the
existing approval and code-generated item-id boundaries remain unchanged.

G10 is not run; G1 has no new sample. Preserve the previous prompt's Direct
fallback measurements, including its initial tail failure, without relabelling
them as prompt 8A measurements. All three existing builds pass; no full suites
were rerun. Added fixed cost is **USD 0/hour**, with no retrieval consumption.
The step result is blocked, not done. The docs and draft stacked PR preserve
the evidence and the exact resume boundary rather than claiming implementation.
