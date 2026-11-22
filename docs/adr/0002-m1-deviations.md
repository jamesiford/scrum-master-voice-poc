# 0002. M1 deviations: Foundry over plain HTTP, demo ownership tags, sprint goal in team settings

## Context
M1 builds the pre-brief against the real Azure DevOps sandbox (Agile process, one real user) and the real Foundry project. Four things in the SPEC did not fit that reality:

1. SPEC 10.2 and the stack rules call for a Microsoft Agent Framework agent over the Foundry v2 SDK. The M1 agent only phrases a summary and one question per member; it has no tools, memory or loop.
2. SPEC 9.2 seeds items for several real users. The sandbox org has exactly one user, so every item would be assigned to the same person.
3. SPEC 9.2 puts the sprint goal in the iteration description. Azure DevOps iteration nodes have no description field.
4. SPEC 9.3's WIQL excludes `Closed`, but burndown needs completed points. Also Agile `Task` has `FinishDate`, not `TargetDate`.

## Decision
1. `FoundryBacklogRiskAgent` calls the Foundry OpenAI-compatible chat route (`/openai/v1/chat/completions`) with `HttpClient` and an Entra token from `DefaultAzureCredential` (scope `https://cognitiveservices.azure.com/.default`; no keys). It sits behind `IBacklogRiskAgent`, returns `null` on any failure, and the handler then keeps deterministic questions built in Domain (`FallbackQuestions`). Model output is still filtered by `WorkItemIdGuard` (rule 2). We move this adapter to a Microsoft Agent Framework agent in M2, when the voice agents need tools and structured multi-step reasoning; only this adapter changes.
2. Seeded items are assigned to the signed-in user and also tagged `demo-owner-{key}`. The roster comes from `demo/seed-ado.json` (`SeedFileRosterSource`). Ownership resolves from the tag when `Demo:HonorOwnerTags=true`, else from `AssignedTo` (DEMO; PROD uses `AssignedTo` and the Azure DevOps team roster only). The same applies to `demo-age-N` / `demo-quiet-N` tags, which shift `StateChangeDate` / `ChangedDate` back because Azure DevOps cannot backdate them (`Demo:HonorAgeTags`).
3. The sprint goal is a field on team settings, editable on the portal Team page, with a seeded default in configuration.
4. The sprint query drops the state filter except `Removed` so completed items count toward burndown. Overdue reads `FinishDate`, falling back to `TargetDate`.

Also: `Azure:TenantId` is passed to `DefaultAzureCredential` because a machine signed in to several tenants otherwise picks the wrong account for Azure DevOps (observed: an unauthorised identity was chosen over the `az login` user).

## Consequences
- Rules 2 and 3 hold: numbers and flags are code, model text is checked for invented ids, and the demo works with the model offline.
- Real per-person assignment in Azure DevOps is not demonstrated until the sandbox has more users; the seed file and tags are the only place that changes.
- The agent adapter will be replaced in M2 without touching Application or Domain.

> **Superseded in part by ADR 0009b:** the `HttpClient` chat call described here was replaced by Microsoft Agent Framework chat-client agents over the Foundry v2 SDK. The ports and the code fallbacks are unchanged.
