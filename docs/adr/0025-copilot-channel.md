# 0025. Copilot channel

Status: accepted for implementation; live G5 acceptance remains unrun.

## Context

Prompt 8 requires five useful read-only skills in Microsoft 365 Copilot, with
the asker's Azure DevOps permissions. The existing `AnswerTeamQuestionQuery`
reads cached team facts prepared under the managed identity and is unsuitable
for this permission boundary. ADR 0015 S1/S2 retains our Azure-managed SSO bot,
not Foundry's directly published, newly generated identity.

## Decision

Keep the API-hosted Agents SDK bot and add `AnswerCopilotQuestionQuery` through
the real mediator. `ITeamResolver.ForUserAsync` uses memberships, even for an
administrator: no membership means no data; multiple memberships require the
user to repeat `team <id>: <question>`. Every turn reauthorises that choice.
The channel actor, SSO assertion and deployment tenant must match before OBO.
Reuse the existing federated `OboAdoTokenSource`; the new
`IDelegatedWorkItemReader` creates an uncached REST client carrying only that
asker's token. There is no agent-reader dependency or credential fallback.
Sprint WIQL is restricted to the team's area as well as its current iteration.

`CopilotFactsBuilder` reuses Domain blocker/risk rules and computes counts and
five-line limits. Personal items match actual `AssignedTo` against a uniquely
Entra-mapped roster entry, never demo owner tags or display names. Commitments
include only that member's unretracted events in that team whose references
are all in the asker's readable current sprint; an itemless or partly
unreadable commitment is omitted. Today's status is lifecycle metadata in the
team time zone, not a transcript or native meeting recap.
The model phrases only a short introduction from a computed summary plus the
authorised team identifier; code owns facts, citations and URLs.
`IResolvedTeamRequest` provides typed result metadata to the existing logging
behaviour, so an automatically selected team is not traced or counted as
`none`. The REST/phraser adapters tag that same use-case activity when using
their team-scoped source. No runtime reflection or new processor.
Numeric, id-like or URL-bearing introductions
are discarded with an explicit fallback. Model failure is also stated.
Validate the complete answer before the SDK streams it and attach the source
card to that same final message. No unvalidated model token is streamed.

Keep legacy Teams reporting/planning/approval outside the skill route.
Unknown Copilot questions show the five starters, never fall through to the
managed-identity reporting handler. Copilot turns are not added to the global
proactive conversation store, preventing those broader cards being sent there.
No approval, Domain floor or native non-recording invariant changes.

## Platform evidence and differences

Learn and official schema checked on 26 November 2026:

- [Enable Copilot](https://learn.microsoft.com/microsoftteams/platform/m365-apps/enable-agent-app-copilot):
  personal plus copilot scopes and `customEngineAgents` referencing the same bot.
- [Prompt starters](https://learn.microsoft.com/microsoftteams/platform/bots/how-to/conversations/prompt-starters)
  describes `type: prompt` and `prompt` fields, but the actual
  [1.25 schema](https://developer.microsoft.com/json-schemas/teams/v1.25/MicrosoftTeams.schema.json)
  permits only command title/description. Keep schema 1.25, put the full five
  executable questions in command titles and the welcome text; do not add
  invalid newer-schema fields or a declarative-agent manifest.
- SDK 1.9 has `IStreamingResponse.IsStreamingChannel`,
  `QueueInformativeUpdateAsync`, `QueueTextChunk`, `FinalMessage` and
  `EndStreamAsync` ([reference](https://learn.microsoft.com/dotnet/api/microsoft.agents.builder.istreamingresponse)).
  The newer Teams SDK streaming tutorial is not a reason to replace our SDK.
  The installed SDK defines `M365Copilot=msteams:COPILOT` and
  `M365CopilotSubChannel=COPILOT`; route by SDK channel/subchannel, not a guessed
  `m365copilot` string.
- [App upgrades](https://learn.microsoft.com/microsoftteams/apps-update-experience):
  user consent can propagate across existing contexts where that user manages
  the app. It is not always one manual update per context. A team-owner update
  does not replace the user's consent in other contexts. Administrators cannot
  force a consent-required user update.
- Source was 1.0.5, not 1.0.4, and has no personal board tab. Upgrade to 1.1.0
  preserving `OnlineMeeting.ReadBasic.Chat`; do not invent a tab or RSC grant.
  The zavaw3 package uses its existing bot id `bed81a44-b97d-4687-9ad3-c4cf546f6689`,
  not the protected old mcm id in the source template. The hook sets app, bot
  and Copilot ids together, SSO information and exact deployed host domains.

## Measurements and consequences

Before deployment, one Direct Simulated Room run has 12 timed turns:
p50/p95 3272/4379 ms, 387 s, 19 transcript pieces and zero talk-overs.
Final runtime `d041890` passes all eight mandatory deployment checks and
62 focused checks. Its one final Direct run has 12 turns, 379 s,
19 transcript pieces, zero talk-overs and p50/p95 3304/4503 ms:
+0.98%/+2.83% against before. These fallback point estimates hold the
historical regression thresholds, not the 1 s absolute target or formal
native G1. The initial `29f87e7` after-run p95 failure (+67.69%) remains
recorded. Hop measurements and the real approver/audit G3 subset are in
`gates/M11-gates.md`; live Copilot G5 still has n=0.
Microsoft 365 Agents Toolkit accepts the source 1.25/1.1.0 manifest.
Actual signed-in Graph catalogue read returned Forbidden for missing
AppCatalog scopes; no retry or unrelated grant was attempted.
Existing portal AllPrincipals Azure DevOps `user_impersonation` consent was
read back successfully; no new Entra permission was required or granted.
The Global Administrator window expired before this session.
Catalogue rollout and real Copilot/OBO permission-negative/streaming acceptance
are exact gates in DEPLOY.md, not inferred from local tests.

Public privacy and terms are real static pages served by the API deployment;
only these two paths are added to Easy Auth exclusions, idempotently.
No new Azure component, broad role, hosted-agent version, secret or fixed
hourly cost. Existing Acs/Share/Direct and native acceptance failures remain.
The public-store validation rubric is not a claim of Teams Store certification;
this package is for the authorised tenant catalogue demo.
