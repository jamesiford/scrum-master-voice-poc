# 0036: Film evidence surfaces are product reads

## Context

Prompt 10 item 6 requires an audit page, readable participant names and
per-hop Live counters. Existing approval comments already contain the
approver's display name and the `zsa-` correlation tag; rewriting them
would duplicate provenance rather than add a missing feature.

## Decision

The portal reads the existing `GET /api/audit/{correlationId}`. The DTO now
includes stored meeting lifecycle identity and join URL, never an invented
meeting link. The page shows absent evidence explicitly and displays proposal
hashes, actors, decision references, revisions and card outbox records.
Stored-resource authorisation takes precedence over any `teamId` query
parameter, preventing a different team's query value from bypassing a
session or audit chain's actual owning team.

`GetSessionLatencyQuery` summarises existing Timing events with Domain
nearest-rank maths. Talk-over markers and uncalled model placeholders do not
become samples. Absent intervals have no row, not a fabricated zero.
The Live console polls this team-authorised read outside the audio hot path.
Browser send intervals remain ephemeral, session-owned SSE aggregates so
sampling adds no Cosmos write to the audio path. Those intervals measure
local send/playback, not network delivery; overlapping hop percentiles
must not be summed.

The ACS adapter separately reports participant identifiers for presentation.
Current [Microsoft Learn identifier documentation](https://learn.microsoft.com/javascript/api/@azure/communication-common/microsoftteamsuseridentifier)
states that a non-anonymous `microsoftTeamsUserId` is the Entra object id.
Only a unique matching team roster id supplies the roster name; duplicate,
anonymous and unmapped participants are labelled guests. Display-name
matching remains unchanged for the existing ACS floor/presence path and
never authorises an approval. Native media remains non-recording.

## Consequences and evidence

Focused Domain/Application tests cover unique identities, guest/duplicate
names, actual timing sample counts, missing and malformed intervals and
real-mediator audit meeting links. Route architecture and negative API tests
cover team scope and query-parameter override refusal. Two portal tests
exercise readable roster names and prevent display-name identity guessing.
Aggregate and live measurement evidence is recorded in
[M11 gates](../gates/M11-gates.md) after validation, not predicted here.
No gate threshold, model, voice default or approved-write rule changes.

Actual deployed339bb67 counter has six measured rows matching the unchanged
zavaw3 harness: total3140/4719ms,n12; real model142/223ms,n10. Baseline total
3419/4912ms,n12 and after are independent point estimates; questions worsen
3645 to3957ms median,n9, so this is no sub-second or formal gate pass.
Actual verify8/8 includes the complete zsa-d5870d3e2a68 chain and item1/
revision68. The unchanged comment already contains the real System
Administrator display name, correlation tag and actual approver author UPN.
Six Sent follow-ups/eight outbox cards confirm automatic end handling,
not Teams delivery. Anonymous audit/latency return401; an authorised Admin
querying team-b still resolves/authorises owning Payments, not the query.
Real negative-user/tenant fixture n0 is not claimed from the integration
tests or local UI. Local actual-API Edge checks show readable roster/guest
labels, populated samples and neutral missing evidence without page errors.
