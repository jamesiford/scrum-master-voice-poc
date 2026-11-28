# ADR 0037: Guarded demo re-seed publication

## Context

Film reset must restore the seed roster/cast and existing seed items, hide demo
sessions/follow-ups/cards, and remove explicitly selected demo registry debris,
without guessing which teams are disposable. `ResetDemoCommand` previously
cleared global data; `IDemoAdoReset` removes comments, and standalone
`tools/SeedAdo` performs privileged bootstrap writes. Neither provides a safe
team-scoped transaction and neither is invoked by this operation.

## Decision

New Admin-only `/api/demo/reseed` stages a durable operation and a **pending**
follow-up. Require all selected existing registry entries to have `IsDemo=true`
and `DemoTenantId` matching both the validated caller and `Demo:ResetTenantId`,
and their exact ids in `Demo:ResetTeamIds`. Defaults are false/null/empty;
old teams are **not** silently marked by names, seed matches or failed connections.
Admin `/api/demo/reseed-scope` accepts the prepared exact opt-in manifest. It
checks the configured tenant, sandbox organisation, canonical server seed
fingerprints and individually named test-debris ids, then atomically persists
their markers. Fingerprint/tenant changes refuse the whole manifest. No unknown
team is selected by display name; removed debris is not recreated on replay.
The target's organisation/project/team/exact area must match its configured seed;
the Azure credential tenant must match the demo tenant. Missing/duplicate seed ids
or missing Sprint 43 refuse staging. No projects, iterations or items are created
or deleted. Sprint 43's seeded items are returned to their original current-sprint
or backlog paths; iteration dates/settings themselves are not changed.
Unselected teams whose ADO areas overlap the target also refuse staging, including
the legacy visualstudio.com organisation alias. New overlapping registrations
cannot enter that guarded area during the operation.

The normal approval API, audit record, `FollowUp.GetApprovedChanges`,
`ApplyApprovedChangesHandler`, delegated approver token and `IWorkItemWriter`
perform ADO writes.
The reset proposal additionally requires a portal app Admin at the existing
approval handler; an ordinary Approver or a configured Teams administrator
cannot approve reset changes through an alternate decision surface.
Each existing item receives one approved baseline patch with
an exact project/area/`zava-demo`/seed-key check, revision test, and operation tag
in the **same patch**. If a response/checkpoint is lost, replay skips the already
tagged item; unapplied items retry. Approval attribution/comments are retained.
Assigned sandbox items return to the Admin's validated UPN; cast ownership stays
in replaced `demo-owner-*` tags, and originally unassigned items are cleared.
Model calls, direct seeder writes and `IDemoAdoReset` are not used.

After the exact proposal is Applied, `/commit` atomically publishes one
`demo-publication` document containing canonical collection overrides, demo
session overrides/tombstones and the completed journal/approval snapshot:

- Files: exclusive cross-process lock, flushed same-volume staged file, atomic
  rename. An interrupted mutation leaves the previous publication readable.
- Cosmos: one item in the existing collection container, partition key
  `demo-publication`, create/ETag compare-and-swap; no cross-partition batch.
  Refuse above the conservative 1.8 MB publication payload limit.

All repository reads use the publication overlays. Legacy files/items remain
physically intact, so a deletion is logical and survives host restart.
Startup's extra-team seeder reads completed removal journal entries and never
recreates the cleaned Identity/Catalogue connections.
Its dedicated removal-history port reads only the publication store, not reset
planning/ADO credentials. Completed removals remain authoritative when new
resets are disabled.
During staging, selected demo teams are paused, their collection/session writes are
guarded, and pre-reset stale collection writes are refused by epoch. Other-team
writes remain allowed. Approval snapshots cannot change the staged proposal hash.
The routed approved writer checks the active operation before touching ADO:
old ordinary approvals for the guarded area are refused, and reset writes must
match the staged per-item baseline.
The complete ADO organisation/project/team/area fingerprint of every selected
team is pinned by the journal. Normal registry edits are guarded; apply and
publication independently refuse any changed fingerprint, including an edited
organisation with otherwise identical project/area names. Reset writes resolve
the journal by operation id, never by the edited connection.
A single-host async gate serialises in-flight approved writes with staging
(the PoC API is configured for one replica); this is
not a distributed lease or a multi-host ADO transaction.
Commit preserves every unselected team's data and removes only the selected demo
team-owned records; the target becomes Active with its seed roster. Cast YAML
files remain immutable, and sessions/cast progress restart empty.

Abort releases an undecided/rejected operation atomically. Once Approved, an
operation may have written externally and **must resume**, not abort.
All four reseed routes carry actual team Read/Operate metadata and an
authenticated app-Admin policy. Journal ownership is resolved before caller
`teamId`; a mismatched override is forbidden. Stage, external apply and commit
refuse running sessions. Persistent session tombstones and preparation-time
cutoffs prevent an old take or a concurrently prepared stale take from returning
after publication/restart. Guarded prepare/start writes return explicit 409
conflicts rather than being dropped or classified as an unexpected failure.

## Exact boundary and consequences

**Only publication of local application state is atomic.** Azure DevOps and the
append-only approval record store are outside that transaction. ADO can be
partially restored while the old local state remains visible and guarded. Resume
the existing operation, approve/apply or retry apply, then commit; never claim
distributed ACID or rollback. ADO edits made externally after an item's marker
are outside this maintenance guard. Multiple independent repository reads may
span the publication instant; each read sees a complete document, not a mixed
partially committed document.

The legacy `/api/demo/reset` remains but now atomically clears local records
only for the configured, marked demo tenant/team set. It preserves registry,
rosters, other-team data and append-only approvals, and refuses live takes or an
in-flight re-seed. It is no longer a global wipe. Reset-ado remains a separate
historical comment-cleanup function, not part of this approved restoration path.
The new path retains approval history and never deletes ADO
discussion comments. The size limit bounds demo use; this is not a production
multi-team transaction design.

`infra/hooks/demo-reseed.ps1` stages by default; `-Approve` explicitly approves and
resumes apply/commit, and `-OperationId` resumes an interruption. `-Abort` releases
an unapproved operation only. No token is persisted.

Initial read-only zavaw3 inventory is in
`.m11-plan/run-log/film-demo-inventory.json`: 202 open follow-ups. The later
tenant-verified snapshot in `demo/film-reset-scope.json` records 214 open after
another take. User-authorised scope is precisely canonical seed ids
`team-payments`, `team-identity`, `team-catalogue` and individually named test
debris `team-b`, `team-c`; their exact persisted fingerprints are in that file.
The two failing connections are Identity/Catalogue's missing projects; no
unknown live team was observed or selected. Before deployment all five live
entries were unmarked, and actual Admin staging refused403 without mutation.
`infra/hooks/demo-reseed-scope.ps1` previews this prepared manifest by default,
`-ConfigureEnvironment` sets local declarative azd parameters only, and `-Apply`
posts markers after the parent validates/deploys configuration. It never
approves or performs a reset.

Source339bb67 is deployed only to zavaw3 API86/bridge10 with validated
digests and unchanged unrelated configuration. Explicit marker readback
passes5/5. The first live operation86f3a14f-bc60-4b54-9540-51a27d24e88e
reviews29 exact seed keys/fields, refuses unapproved apply403/premature
commit409/wrong-team403, then completes through the real Admin approval.
All29 staged fields match actual ADO readback:19 current-sprint items and
10 root-backlog items; Sprint43 is empty, not populated with invented work.
Application reads show only the active primary, seed roster and no old
sessions/follow-ups. Cleanup hides226 old follow-ups (214 open),20 sessions
and four reviewed connections. Completed replay changes no ADO revisions
and preserves the new verification take. Immutable Approved/Applied records
and29 revisions remain accessible after cleanup; the ADO comment contains
the real System Administrator display name andzsa-86f3a14fbc60 tag.
This is actual sandbox proof, not a real other-tenant/user fixture or a
distributed-atomicity claim. Full aggregate/after evidence belongs in M11 gates.

After mandatory verify8/8/evidence capture, final operation
`67ad9b4c-2614-4af5-965b-d6b84e938a34` completes the clean next-take baseline. Actual marker
replay uses removal history, without recreating four absent connections.
One exact clean API86 restart proves startup durability: only the Active
primary, seed roster and zero sessions/follow-ups remain; all29 final fields
match, completed replay changes no ADO revision, and earlier verification
approval records/comment text are unchanged. API/bridge stay running.
