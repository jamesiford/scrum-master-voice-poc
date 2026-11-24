# Zava AI Scrum Master Agent: Ceremony Catalogue

> **Purpose:** the single definition of every Scrum ceremony and recurring activity the agent is expected to cover. For each one it says what the agent does before, during and after, what it reads and writes in Azure DevOps, what is built in the PoC today, how it is demonstrated, and what production adds.
>
> **Companions:** *PoC Build Specification* (v0.6, as built), *PoC As-Built*, *Production Specification* (v0.4) and *Solution Architecture*. This catalogue replaces the short "Ceremony playbooks" table in Solution Architecture section 7 as the detailed reference.
>
> **Owner:** James Ford (Microsoft). **Version 0.2**, November 23, 2026 (aligned with the Requirements Audit; model-assisted utterance classification moved from production to the PoC in M10). Status: draft for confirmation with Rajeev.

---

## 1. How to read this catalogue

The agent covers four Scrum events, one ongoing Scrum activity and three recurring operating jobs. Each one is built from the same three reusable patterns, which is why proving a few end to end proves the rest can be replicated.

| Pattern | What it is | Where it is already proven in the PoC |
|---|---|---|
| **P1 Live facilitation by voice** | The agent joins the Teams meeting, holds the floor through a deterministic state machine, calls on people, listens, links what they say to Azure DevOps items in code, keeps the timebox, recaps | Daily Scrum (real Teams meeting and Simulated Room) |
| **P2 Prep pack** | Facts are computed in code from Azure DevOps and history; a model only phrases a summary and suggested questions; the pack goes to the human Scrum Master in Teams and the portal | Daily Scrum pre-brief, KPIs and reports, planning prep, retrospective draft |
| **P3 Close the loop with approval** | The agent proposes a change, a named person approves it in Teams or the portal, and only then is it written to Azure DevOps with "Approved by ..." | Standup follow-ups (comments) |

**Agent roles in a ceremony**

| Role | Meaning |
|---|---|
| **Prepare** | Builds the prep pack before the ceremony and sends it to the human Scrum Master |
| **Facilitate** | Runs the ceremony by voice in the Teams meeting (P1) |
| **Co-facilitate** | Joins the ceremony, keeps time and captures outcomes, while the human Scrum Master leads |
| **Follow up** | After the ceremony, sends outcomes and proposed Azure DevOps changes for approval (P3) |

**Status key:** **Built** = in the PoC and demonstrated · **Partial** = some of it is built · **Planned (M9)** = in the next PoC milestone (see the M9 prompt) · **Production** = in the Production Specification only.

**Timeboxes** follow the Scrum Guide maximums for a one-month sprint, scaled for Zava's two-week sprints. The two-week figures are assumptions used for capacity planning in the Production Specification (60 / 45 / 45 minutes for planning, review and retrospective) and must be confirmed with Rajeev.

---

### Rajeev's own description (November 13 call)

*"Sprint plan in the start and retrospective in the end and then between the sprint, it just the daily stand up or escalation meeting."* He described the **sprint review** as *"what went well, what could be done better, what was the missing points"*, which is retrospective content. Until he confirms, treat C3 (Sprint Review) as optional and C4 (Retrospective) as the end-of-sprint ceremony. Scope he set by email: **Azure DevOps, with bot access to Teams, Calendar and Outlook**. See *Requirements Audit*.

## 2. Summary

| # | Ceremony or activity | Type | Agent role | Patterns | Timebox (assumed, two-week sprint) | PoC status | Demo level |
|---|---|---|---|---|---|---|---|
| C1 | **Daily Scrum** (standup) | Scrum event | Prepare, **facilitate**, follow up | P2, P1, P3 | 15 min | **Built** | **End to end, live by voice in Teams** |
| C2 | **Sprint Planning** | Scrum event | Prepare, co-facilitate, follow up | P2, P3 | 60 min (Scrum Guide max 8 h per month) | **Built (M9a)**, not voice-led | Plan approval in the portal or Teams: items move to the next sprint and get owners |
| C3 | **Sprint Review** | Scrum event | Prepare, co-facilitate, follow up | P2, P3 | 45 min (max 4 h per month) | **Partial (M9):** review pack and approved comments; no feedback loop | Pack card with delivered points and per-story comments |
| C4 | **Sprint Retrospective** | Scrum event | Prepare, **facilitate**, follow up | P2, P1, P3 | 45 min (max 3 h per month) | **Built (M9b) in the Simulated Room; Teams meeting not checked:** voice-led retro, approved action tasks | Voice retro, summary and action card; approved tasks created in Azure DevOps |
| C5 | **Backlog Refinement** | Ongoing activity | Prepare, follow up | P2, P3 | Rolling; typically 30 to 60 min a week | Partial (readiness gaps, Story Writer draft) | Readiness check and story draft |
| O1 | **Sprint KPIs and stakeholder status report** | Operating job | Prepare | P2 | n/a | Built (team-level) | Shown in portal and Teams |
| O2 | **Ceremony scheduling** | Operating job | Prepare, follow up | P2, P3 | n/a | **Built (M9d)** | Proposed sprint calendar |
| O3 | **Risk escalation** | Continuous | Follow up | P3 | n/a | Built (long-blocked items, whether or not raised again; missed commitments) | Shown after the standup |
| O4 | **Escalation meeting** (Rajeev's term, to confirm) | Ad hoc | Prepare, follow up | P2, P3 | Short, ad hoc | Not built | Not in the demo until defined |

---

## 3. Ceremony definitions

### C1. Daily Scrum

| Aspect | Definition |
|---|---|
| **Purpose** | Inspect progress toward the sprint goal and adapt the plan for the next day. For Zava: make sure every person's items are covered, surface blockers early, and remove the Scrum Master's note-taking and chasing |
| **When** | Every working day at the team's time, in a recurring Teams meeting. The scheduler prepares at T-10 minutes and starts at T-1 minute |
| **Who** | Developers (required), human Scrum Master (present; can take over at any time), Product Owner (optional) |
| **Timebox** | 15 minutes. Warning at 80 percent; recap and close at the limit |
| **Agent role** | Prepare, **facilitate by voice**, follow up |
| **Before (P2)** | Pre-brief computed in code: sprint and goal, burndown status, per-person open items and flags (blocked, overdue, stale, no recent update, unassigned), suggested questions phrased by a model with invented ids removed. Sent to the Scrum Master as a Teams card and shown in the portal |
| **During (P1)** | Greets, states the sprint goal, calls on each person by name, links what they say to their items in code, asks about the most urgent flagged item not yet covered (up to three questions per person), parks side conversations, handles chime-ins, step-aways (skip and circle back), silence (present but no response), corrections (retract), and one person giving another's update. Answers item questions from live Azure DevOps. Shares a standup board on screen |
| **After (P3)** | Recap (blockers, parked topics, absent and silent members, flagged items nobody covered). Per-person follow-up with proposed Azure DevOps comments, approve or reject in Teams or the portal. "Missed standup" and "no response" messages. Escalation of long-blocked items to the Scrum Master |
| **Reads from Azure DevOps** | Current iteration, work items (state, assigned to, tags, story points, remaining work, state change date, changed date, finish or target date), comments |
| **Writes to Azure DevOps** | Comments proposed from what people said, only after approval, with "Approved by {name}". The writer also supports state and tag changes |
| **Outputs** | Session timeline and transcript, recap, follow-ups, escalations, attendance, latency metrics |
| **KPIs fed** | Attendance, follow-up closure, blocker age, coverage of flagged items |
| **PoC status** | **Built** and run in a real Teams meeting and in the Simulated Room (as-built sections 3 to 5) |
| **Demo** | End to end live: pre-brief, voice standup in Teams with the shared board, follow-ups, approval, comment visible in Azure DevOps |
| **Production adds** | Per-speaker audio or Work IQ transcripts for attribution, multilingual support, one-second latency target, Agent 365 identity, scale to ten teams |
| **Open with Rajeev** | Standup time per team and location; does the Scrum Master want the agent to lead every day or alternate; which changes beyond comments the agent may propose (state, remaining work) |

### C2. Sprint Planning

| Aspect | Definition |
|---|---|
| **Purpose** | Agree why the sprint is valuable (sprint goal), what can be done (selected backlog items) and how (the plan), within the team's real capacity |
| **When** | First day of each sprint, in a recurring Teams meeting |
| **Who** | Developers, Product Owner (owns ordering and the goal), human Scrum Master (leads) |
| **Timebox** | Assumed 60 minutes for a two-week sprint |
| **Agent role** | Prepare, co-facilitate (keeps time, records decisions), follow up |
| **Before (P2)** | **Planning pack** computed in code: capacity per person (velocity range from the last three to six sprints, adjusted for availability), carry-over from the current sprint, candidate items in backlog order, Definition-of-Ready gaps (no acceptance criteria, no estimate, no owner), a proposed selection that fits capacity, suggested owners for unassigned items based on load and past area, and a draft sprint goal phrased by a model from the selected items |
| **During** | Production: joins the meeting, walks the proposed selection, flags over-commitment against the velocity range, records the agreed goal and scope. PoC: the Scrum Master walks the pack on screen; voice co-facilitation is production scope |
| **After (P3)** | **Plan for approval:** move selected items into the next iteration, set suggested owners, add a comment "Planned in Sprint {n} planning, approved by {name}". Nothing is written until the Scrum Master or Product Owner approves. Planning summary posted to the team channel |
| **Reads** | Backlog (ordered), iterations, capacity settings, KPI history, item readiness fields |
| **Writes** | Iteration path, assigned to, comment; all approval-gated |
| **Outputs** | Planning pack, agreed sprint goal, planned scope, risks, approved assignments |
| **KPIs fed** | Committed points, planned versus capacity, readiness rate |
| **PoC status** | **Built (M9a), not voice-led:** the Planning page and `POST /api/planning/prepare` build a plan in code: capacity from the velocity range (illustrative fixture) split by member and scaled by editable availability; carry-over first, then ready backlog items in order; not-ready items listed with why; suggested owners; over- and under-commitment flags; a model-phrased draft goal with a code fallback. **Approve plan** (portal or Teams card; chat "prepare sprint planning" for the Scrum Master) writes `SetIteration` and `Assign` through the approval guard, each with an "Approved by {name} in {Teams or the portal}, proposed by Zava Scrum Agent from the {date} sprint planning" comment; **Reject** writes nothing. Checked live against the sandbox except the Teams card actions and the chat command. Not built: the proposed selection does not fit per-person capacity (it warns), child tasks do not move with their story, calendar capacity, voice co-facilitation |
| **Demo** | Reset, **Prepare pack**, the card arrives, **Approve plan**: the selected items are in Sprint 43 with the suggested owners and an "Approved by" comment; a rejected plan writes nothing (see `DEMO.md`, "Sprint planning (M9a)") |
| **Production adds** | Calendar-aware capacity (out-of-office from Microsoft 365), voice co-facilitation, Fabric history |
| **Open with Rajeev** | Does Zava record capacity in Azure DevOps; who approves the plan (Scrum Master, Product Owner or both); story points or hours |

### C3. Sprint Review

| Aspect | Definition |
|---|---|
| **Purpose** | Inspect the outcome of the sprint with stakeholders and decide what to adapt in the product backlog |
| **When** | Last day of the sprint, before the retrospective |
| **Who** | Scrum team and invited stakeholders |
| **Timebox** | Assumed 45 minutes for a two-week sprint |
| **Agent role** | Prepare, co-facilitate (agenda and time, captures feedback), follow up |
| **Before (P2)** | **Review pack** computed in code: sprint goal met or not, completed versus committed, items to demo (done this sprint), carry-over and why, key KPIs. A model writes a short stakeholder summary from these figures |
| **During** | Production: keeps the agenda and captures stakeholder feedback. PoC: the Scrum Master presents the pack; feedback is typed into the portal or Teams |
| **After (P3)** | Each piece of feedback becomes a **draft backlog item** (Story Writer); approved drafts are created in Azure DevOps with a "From Sprint {n} review" tag. Stakeholder summary posted to the channel |
| **Reads** | Iteration items and history, KPIs, sprint goal |
| **Writes** | New backlog items, only after approval |
| **Outputs** | Review pack, stakeholder summary, new backlog items |
| **KPIs fed** | Say/do ratio, goal attainment |
| **PoC status** | **Partial (M9, built):** `POST /api/ceremonies/SprintReview/pack` computes points delivered against committed and the completed stories and bugs in code, and sends one Scrum Master card. After approval a comment is added to each completed item. Not built: stakeholder feedback to draft backlog items, the goal-met judgement, voice or agenda co-facilitation |
| **Demo** | Review pack card, approve, comments on the completed stories in Azure DevOps |
| **Production adds** | Voice co-facilitation, stakeholder distribution lists, Power BI view |
| **Open with Rajeev** | Who attends reviews at Zava; does Zava run reviews per team or combined |

### C4. Sprint Retrospective

| Aspect | Definition |
|---|---|
| **Purpose** | Inspect how the sprint went (people, process, tools, Definition of Done) and commit to improvements |
| **When** | Last day of the sprint, after the review |
| **Who** | Scrum team (Developers, Product Owner, Scrum Master) |
| **Timebox** | Assumed 45 minutes for a two-week sprint; 15 minutes in the demo |
| **Agent role** | Prepare, **facilitate by voice**, follow up |
| **Before (P2)** | **Retro pack** computed in code: sprint KPIs, recurring blockers, parked topics, missed commitments, readiness gaps, last retro's action items and whether they were done. A model writes the opening summary |
| **During (P1)** | Opens with the summary, then runs a round-robin in a chosen format (start-stop-continue by default): asks each person for one thing in each category, records items, reads back the themes, then asks for up to three action items and an owner for each. Same floor control, step-away, silence and correction handling as the standup |
| **After (P3)** | Action items proposed as new Azure DevOps tasks tagged `retro-action` in the next sprint, with the agreed owner. Created only after the Scrum Master approves. Retro summary posted to the channel. Next sprint's standup pre-brief tracks the actions |
| **Reads** | Sprint items, standup events, KPIs, previous retro actions |
| **Writes** | New tasks for action items, only after approval |
| **Outputs** | Retro board (themes), action items with owners, summary |
| **KPIs fed** | Retro action completion rate |
| **PoC status** | **Built (M9b), checked in the Simulated Room; not yet in a Teams meeting.** The agent runs the retrospective by voice on the standup's gateway and floor control (`SPEC.md` 10.7): a round for what went well, what to do better and what is missing (or start-stop-continue), a clarifying question for unclear notes, a themes read-back, up to three actions with an owner each, then a Scrum Master follow-up whose `CreateWorkItem` tasks (`retro-action`, next sprint, owner) are created only after approval, with the attribution as the first comment. Before M9b the Retrospective page and `POST /api/ceremonies/Retrospective/pack` already built a code-computed draft and a one-card pack; both remain. Not built: other formats, anonymous contributions, tracking last retro's actions in the next standup |
| **Demo** | Prepare retro, run it in the Simulated Room (about 5 minutes), approve the follow-up, action tasks appear in Azure DevOps with owners; `docs/DEMO.md`, "Retrospective by voice (M9b)" |
| **Production adds** | More formats (4Ls, sailboat), anonymous contributions on request, sentiment trend without individual scoring |
| **Open with Rajeev** | Preferred retro format; does the team want the agent to lead or only capture |

### C5. Backlog Refinement (ongoing activity)

| Aspect | Definition |
|---|---|
| **Purpose** | Keep the top of the backlog ready: clear, estimated, small enough, with acceptance criteria |
| **When** | Continuously, often a weekly session |
| **Who** | Product Owner and Developers |
| **Agent role** | Prepare, follow up |
| **Before (P2)** | Readiness report: items in the next two sprints' worth of backlog that miss acceptance criteria, an estimate, or a clear description; oversized items |
| **After (P3)** | Story Writer drafts user stories and acceptance criteria (Rajeev's "step 2"). Approved drafts update or create items in Azure DevOps |
| **PoC status** | Partial: readiness gaps on the Planning page; Story Writer drafts from a parked topic (never written) |
| **Demo** | Show readiness gaps and a story draft; writing approved drafts comes with M9c |
| **Production adds** | Refinement session co-facilitation, splitting suggestions, link to GitHub after repo migration |

---

## 4. Recurring operating jobs

| Job | Definition | PoC status | Demo |
|---|---|---|---|
| **O1 Sprint KPIs and stakeholder status report** | KPIs computed in code (say/do, velocity, carry-over, blocker age, attendance, follow-up closure); sprint summary and a monthly report over three to six sprints; a model writes only the narrative. Production adds a leadership roll-up across teams and Scrum Master time saved | Built for one team; history is an illustrative fixture because the sandbox has no past sprints | Reports page and "How is attendance?" in Teams |
| **O2 Ceremony scheduling** | From the iteration dates, propose the sprint's calendar: daily standups, planning on day one, retro (and review if confirmed) on the last day. After approval, create the Teams meetings and invite the roster. Rajeev: *"it can schedule team meetings"* | **Built (M9d), checked live in the demo tenant (meetings created, scheduler joined the lobby):** the scheduler prepares and starts standups; a proposed calendar card creates the sprint's Teams meetings through Graph after approval and stores their join URLs for the scheduler | Proposed calendar card, approve, meetings in Outlook, next standup auto-joins; `.ics` fallback |
| **O3 Risk escalation** | Items blocked or stale beyond the team's thresholds are escalated to the Scrum Master (and optionally a manager) with context | Built inside standup follow-ups | Shown after the standup |

---

## 5. Rajeev's capability list mapped to ceremonies

| Capability (Rajeev's list) | Ceremony or job | Pattern | PoC status | How it is demonstrated |
|---|---|---|---|---|
| Conduct Daily Scrum | C1 | P1 | **Built** | Live in Teams |
| Prepare Sprint Planning | C2 | P2, P3 | **Built (M9a)** | Planning pack and plan approval |
| Prepare Sprint Review | C3 | P2 | **M9c** | Review pack |
| Prepare Retrospective | C4 | P2, P1 | **Built (M9b)** | Retro pack, then voice retro |
| Review Azure DevOps backlog | C2, C5 | P2 | Partial | Readiness gaps; planning selection |
| Identify overdue/stalled work items | C1 | P2 | **Built** | Pre-brief flags and live questions |
| Follow up on commitments | C1 | P3 | **Built** | Follow-up cards with approval |
| Generate sprint KPIs | O1 | P2 | **Built** | Reports page |
| Prepare stakeholder status reports | O1, C3 | P2 | Partial (team monthly report); M9c adds the review summary | Monthly report; review summary |
| Schedule Scrum ceremonies | O2 | P2, P3 | **Built (M9d)** | Proposed sprint calendar |
| Capture actions/decisions | C1, C4 | P1 | **Built** (standup and retro) | Recap and retro actions |
| Recommend assignments | C2 | P2, P3 | Partial (advice only); **M9a** writes after approval | Suggested owners approved in the plan |
| Escalate delivery risks | O3 | P3 | **Built** | Escalation after the standup |

---

## 6. Demo plan

| Order | Demo | Status | Why it is in the demo |
|---|---|---|---|
| 1 | **Daily Scrum end to end** (C1) | Built | The hard, novel part: voice facilitation in a real Teams meeting, grounded in Azure DevOps, with approval-gated write-back |
| 2 | **Sprint Planning end to end** (C2) | M9a, light lift | Shows the prep-pack and approval patterns on a second ceremony, and covers planning, backlog review and assignment recommendations with real Azure DevOps writes |
| 3 | **Sprint Retrospective by voice** (C4) | **Built (M9b)** | Shows that the voice engine is reusable: a different ceremony on the same floor control, with action items written back after approval |
| Optional | Sprint Review pack and feedback loop (C3), sprint calendar (O2) | M9c, M9d, light | Completes the "prepare" list and stakeholder reporting |

**The line for the room:** "We built the hard part, the live voice standup, end to end. Planning and the retrospective run on the same three patterns, so every ceremony on your list is an instance of something you have just seen work."
