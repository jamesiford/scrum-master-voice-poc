# ADR 0034: Work IQ discovery succeeds; content remains gated by real fixtures

Date: 2026-11-27
Status: Accepted metadata-only implementation; content integration blocked

## Measured evidence

Read current Learn [API overview](https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/api-overview),
[MCP overview](https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/mcp/overview),
[retrieve](https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/mcp/retrieve),
[tenant setup](https://learn.microsoft.com/microsoft-365/copilot/extensibility/work-iq/enable-work-iq)
and [source constraints](https://learn.microsoft.com/plugins/knowledge-sources)
before implementation. Work IQ requires delegated authentication and approved
Copilot Credits spending assignment; a Copilot licence is not billing proof.
Application-only/agent-identity retrieval is unsupported by the documented
contract. We did not test it by requesting application permissions.

Actual Graph reads verify the portal's AllPrincipals WorkIQAgent.Ask grant
to resource SP ca4a440d-9d38-41a8-a140-f5ff230ac975 and the matching
0b1715fd-f4bf-4c63-b16d-5be31f9847c2 declaration, n1 each. No additional
Work IQ permission or policy was requested or changed. The inherited
greenfield merge preserves the actual optional declaration idempotently:
n1 live fixture, two merge applications, zero directory writes.

Two real administrator portal-to-Work IQ OBO exchanges and two MCP
initialisations succeed; one completed tools/list returns HTTP200 and
12 tools, server1.0.744.0, MCP2025-03-26. The complete successful
discovery probe takes9.352s, n1; the preceding initialisation-only probe
takes7.787s. These include CLI/credential acquisition and are **not retrieval
latency**. The existing Easy Auth confidential credential is used only
in memory for the spike and cleared; no token or credential is persisted.
This does not prove the separately implemented deployed MI-federated OBO
adapter. One local status-property error initially misclassified HTTP200;
it was corrected without claiming an external failure or retry.

Live retrieve schema exposes TeamsMessages.urls[].url and
Meetings.items_by_id[].{id,is_series}. Learn confirms URL constraints
can select a channel rather than all private chats, and a meeting must use
its meeting UID, not an assumed onlineMeeting/session id. Omitted/empty
constraints are broad searches. Schema discovery establishes available
request fields, **not measured server enforcement or permission trimming**.

Actual registry n5 has all WorkSignalsEnabled=false and no channel bindings;
the preceding membership read found zero memberships. No approved named
lead, exact channel/meeting/UID, sharing audience, other-team negative user
or spending assignment exists in the supplied fixture. Subscription ARM
inventory finds no matching billing resource, which does not establish
absence of a Microsoft 365 spending policy. Admin-centre readback could not
be completed: Playwright reports missing Chrome; the alternative desktop
browser exposes neither verified URL nor page tree and requires stopping.
Billing therefore remains **not_run**, not absent or consent denied.
All content queries, source-enforcement checks, positive/negative users,
helper/review cases and G11 latency/leakage are **n0, not_run**.

## Decision

Keep all four content uses, connection UI/storage and M9c unbuilt until the
real source/owner/billing spike passes. No broad ask, private mail/chat read,
mutation, invented source or synthetic user stands in for that proof.
There is no automatic assignment or Azure DevOps change from evidence.
FollowUp.GetApprovedChanges and IWorkItemWriter remain the only write path.

Complete independent real metadata plumbing: Application owns the
request-local token port; Infrastructure implements MI-federated owner OBO
and an MCP discovery client that only sends initialise, initialised and
tools/list. JSON/SSE parsing checks matching request id, protocol errors
and negotiated version. The client refuses disabled/mismatched contexts
before token or HTTP calls and never retries policy/authentication failures.
It has no tools/call method, cache, global team state or content persistence.
The classes and existing WorkSignalReader stay **unregistered**, not a
selectable fake retrieval adapter. Production DI/routes/prompts remain
unchanged; there is no new use case or API to add to the catalogue.

Existing greenfield hooks already preserve approved declarations and provide
the managed-identity federation. No new infrastructure, roles, billing,
settings permission or resource is needed for unregistered metadata code.
The step deploys only its committed API artefact into existing zavaw3, retaining
configuration, secrets, Acs/Share/Direct, native non-recording and stable4.
Focused checks cover real-mediator consent/approval guards and pure discovery
parsing/disabled-token boundaries; they are not live product goldens.

## Consequences

WORK-SIGNALS-CONSENT remains completed. DEPLOY.md's billing administrator
and real lead/channel/meeting/negative-user actions are the only remaining
human prerequisites. After them, prove pre-retrieval server constraints and
MI-federated owner OBO, then implement only supported content uses, greenfield
lifecycle, UI, retention and G11. Never turn the team flag on merely because
tools/list works. Fixed added cost USD0/hour; content retrieval calls n0.
Build, deployment and baseline/after measurements are recorded in
[gate evidence](../gates/work-iq-redo-M11.json) and M11-gates.md.
