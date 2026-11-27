# Work signals privacy note

**Status:** consent and evidence contracts only (M11 8B, ADR 0027).
No Work IQ adapter, portal connection page or live signal feature is enabled.

Work signals are optional per team and default off. A deployment Admin must
enable them. A named Scrum Master/TeamLead who belongs to that team must
connect with their own delegated sign-in. The connection page, when built,
will show the owner and connection date and offer Revoke to the owner/Admin.
Admin consent does not make an administrator the team's signal owner.

Reads are limited to that team's explicitly selected Teams channel, existing
notes from its stored meetings and topics linked to its current board.
**No private mail, one-to-one chats, broad personal search, live transcript
listener or recording.** A source-type filter alone is not sufficient to
exclude private chats: the live spike must prove exact pre-retrieval scope.
Source protections and access are checked by Work IQ and again when a person
opens a reference. Evidence is untrusted content, never an instruction.

Only the current named owner's validated assertion may be exchanged through
OBO. The contracts require current team-lead membership before and after
retrieval. Missing/expired sign-in, revocation, membership loss and disabled
signals mean **No signals available**, with a visible reconnect banner when
the UI is built. There is no service, administrator, agent-identity or other
member fallback. Consent metadata is not a persistent credential.
Scheduled standups without owner sign-in continue on the existing path
without signals, not an unattended read of personal data.

The owner must approve which team channel/meeting content can be displayed
to the team's existing authorised audience. Owner access alone does not
permit republishing private or protected material to that audience. The
positive/negative fixture must establish team-wide audience access; otherwise
do not publish it. Every displayed excerpt includes its source, title, link
and timestamp. Chase wording can acknowledge only a verified update authored
by that member. Helper suggestions require a human choice.

Store only references and short excerpts, keyed by team, owner and correlation
id with an explicit deletion deadline. Retention will be configurable (positive
and at most thirty days), with excerpts at most one thousand characters.
Delete expired references and references from a revoked connection. Do not
store raw Work IQ responses, whole messages, transcripts, credentials or MCP
conversation history. Do not log assertions or excerpts. A future encrypted
delegated cache must be separate from evidence and scoped to tenant/team/owner
and connection; revocation invalidates it.

Evidence never changes Azure DevOps by itself. Review feedback may later
produce cited draft backlog items, but the existing person's approval,
`FollowUp.GetApprovedChanges()` and `IWorkItemWriter` remain mandatory.
No proposal conversion or M9c closure is built in this contract-only step.

Consent requires review of Work IQ's broad `WorkIQAgent.Ask` permission
(it includes write access even though this integration must remain read-only).
Billing assignment also requires a responsible owner. Exact human actions
are DEPLOY.md WORK-SIGNALS-CONSENT / WORK-SIGNALS-BILLING /
WORK-SIGNALS-FIXTURE. No tenant policy or billing change is made by this step.
