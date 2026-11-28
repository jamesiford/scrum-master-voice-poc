# ADR 0040: team-partitioned cards, follow-ups and delivery records

Date: 27 November 2026. Status: accepted; live rollout measurements are in
`docs/gates/r2-storage-M11.json`.

## Context

The three whole-document collections can reach Cosmos NoSQL's 2 MB item
limit after repeated standups. Resetting the demo hides the problem.
ADR 0037 also redirected demo sessions and follow-ups into a publication
document, reintroducing growth despite ADR 0021's session items.

Current Microsoft Learn confirms a 2 MB UTF-8 JSON item limit, immutable
partition keys, and point-read cost determined by item size rather than
history length. Indexed equality queries cost according to the records
returned. Full history lists cannot have flat cost as their output grows.

References:
- https://learn.microsoft.com/azure/cosmos-db/concepts-limits
- https://learn.microsoft.com/azure/cosmos-db/optimize-cost-reads-writes
- https://learn.microsoft.com/azure/cosmos-db/partitioning

## Decision

Use one `TeamRecords` item per card, follow-up and delivered bot activity,
partitioned by `/teamId`. Co-locate the three types, with indexed collection,
session, status and key fields; exclude the payload from indexing. SHA-256
ASCII ids derive from collection and natural key: follow-up id, card dedupe
key, or the unambiguous JSON tuple of dedupe key and conversation key.
Create-only delivery records preserve the first successful send on retries;
updates replace only their own card/follow-up. New public card ids are also
deterministic and team-owned. Existing card ids, payloads, sorting, endpoints,
approval rules and selectable adapters remain unchanged.

File mode uses one atomic JSON file per record in a hashed team directory.
Existing collection constructors remain compatible with legacy test fixtures;
both production DI modes select the item adapter. Repository session/status
queries are indexed, and known-team/key lookups are point reads. An id-only
public lookup still needs an indexed cross-partition query to discover its
team; authorisation remains at the existing application boundary.

Before requests/schedulers, create-only migration copies authoritative
publication overrides, not superseded raw collections. A schema marker is
published only after all copies succeed, and growing arrays are removed
from the publication document. Legacy sources and a read-only export remain
intact. Partial copies can be replayed without replacing newer items.
Legacy teamless bot activities are attributed only through an unambiguous
stored card; unmatched evidence is logged and retained without replay or
invented ownership.

Demo reset switches only per-team record generations and session cutoffs
atomically, keeping physical historical items for recovery. Sessions continue
in the existing item store instead of growing publication arrays. Staged
reset follow-ups also have their own items; the guarded operation snapshot
remains authoritative during recoverable approval/apply/publication phases.
Completed/aborted reset proposals are not resurfaced as open follow-ups.
Old or in-flight writes cannot become records of the new generation.
The existing reset journal/registry is control state, not a standup history
store; its pre-existing safe-size refusal remains explicit.

The additive rollout provisions only this container and its application
managed identity's container-scoped contributor role, using the same Bicep
module as empty-environment provisioning. Temporary deployer read-only export
and new-container load grants are removed after measurement. No account
keys, Entra changes, other environment lifecycle work or hosted-agent update.

## Evidence and consequences

Initial export: Sessions n=20/1,079,343 bytes; ApprovalRecords n=35/74,280
bytes; collections n=5/1,388,641 bytes. Actual deployed source before change
passes mandatory verification 8/8 and native readiness 5/5: Direct p50/p95
3449/3987 ms, n=8; questions 3449/3987 ms, n=6; zero talkovers; approver
revision76 and complete audit. These are independent point estimates, not
Teams acceptance or a passed historical G1 gate.

The final idle predeployment export includes 37 approval records and
1,445,645 collection bytes. API source be487f7 is digest-pinned and ready in
zavaw3, with configuration parity. Read-only preservation proof confirms
5 authoritative follow-ups, 6 cards and 2 reset follow-ups, 21 retained
sessions, 4 unchanged raw legacy documents and all 37 unchanged approval
records. There are zero unattributed activities and zero growing publication
arrays. Superseded histories remain in the retained raw sources, not replayed
over the authoritative reset view.

Focused file-adapter and real-mediator tests cover team/session/status
isolation, concurrent delivery dedupe, card identity, partial migration,
authoritative empty overrides, activity conversation keys and atomic reset.
The live probe uses actual snapshots, the shipped renderer/envelope/adapter,
300 simulated standups and 5,400 records, with zero HTTP 413. Maximum item
size is 8,672 bytes; total 19,262,100 bytes; creates consume 49,830 RU.
Writing and measurement take 683.4913 seconds, excluding cleanup.

Actual RUs are unchanged at 1, 100 and 300 standups, n=10 per collection
per checkpoint (rounded to two decimals):

| Collection | Point read RU | Session query RU | Composed repository query RU |
| --- | --- | --- | --- |
| Cards (5 per session) | 1.33 | 3.64 | 13.60 |
| Follow-ups (3 per session) | 1.14 | 3.23 | 13.18 |
| Activities (10 per session) | 1.00 | 3.29 | 13.26 |

The last column includes the real publication-metadata read, not an
artificially cheap stand-in. Full-history lists still scale with output.
These synthetic items are not delivered to Teams and never apply ADO changes.
Deployment and postdeployment evidence are in the gate file.

No new fixed hourly compute charge: serverless container RUs and stored
bytes are variable usage. Roles have no separate hourly charge. Retained
legacy bytes are a deliberate additive-migration storage cost.
