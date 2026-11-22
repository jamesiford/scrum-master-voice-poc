# Zava AI Scrum Master Agent: Requirements Audit

> **Question:** does the PoC (repo `jamesiford/scrum-master-voice-poc`, `main` at `36d25ad`) do what Rajeev asked for, in the areas the PoC covers?
>
> **Sources:**
>
> - **T:** the Teams transcript of *Zava // AI Agent for Scrum operations*, November 13, 2026, 2:00 PM PT (38 minutes).
> - **E:** the email thread *looking for AI guidance*, November 16 to 20, 2026, including Rajeev's inline **[RM]** replies.
> - **D:** Rajeev's attachment *Scrum Master Activities.docx* (the 13-item "AI Scrum Agent" list).
>
> **Method:** each requirement was traced to the code, prompts and docs in the repo. Code was read where a gap was suspected (flag rules, follow-up planner, facilitator and turn prompts, Teams messenger, KPI folder, app manifest). Items marked *not found* were not seen in the files read; Copilot should confirm them before building.
>
> **Date:** November 22, 2026. **Update, same day:** the statuses in section 2 were re-checked against the M9 code (ceremony kinds, unblock loop, commitment carry-over, `AtRiskRule`, recap routing, Graph email option; ADR 0010). Section 1 is kept as the verdict at `36d25ad`; section 1.1 says where each gap stands now. "Built" here means in code and unit tested, **not yet run in a live Teams meeting** (AS-BUILT section 11).

## 1. Verdict

**The PoC matches the heart of the ask.** Rajeev's own "first step" is an agent that *"join[s] the team meeting, host[s] the team meeting… go[es] round by round to every developer… [with] access to [Azure DevOps] on their sprint"* (T), and his November 19 email asks only one thing: *"you all are connected that I am asking for a voice agent?"* (E). The PoC does exactly that, in a real Teams meeting, by voice. The rest is in place too: Azure DevOps grounding, follow-ups, recap, approvals, KPIs and the monthly report.

**Seven gaps matter for the demo.** They are all in areas the PoC already claims to cover, so Rajeev will notice them:

1. **Outlook and Calendar are named requirements, and the PoC uses neither.** Rajeev narrowed scope to *"Azure DevOps only at this point with Bot Access to Teams, Calender, and Outlook"* (E, [RM]). Email follow-ups only go to an outbox, and nothing reads or writes the calendar.
2. **The agent cannot schedule ceremonies.** *"The real thing is it can schedule team meetings, it can get in a call, it should be voice bot"* (T). The scheduler only starts standups that someone configured in the portal.
3. **Blocker follow-up stops short.** The human Scrum Master *"do[es] certain follow ups… over an email or a team chat… whom to reach out, whom to send an email… make sure that gets clarified"* (T). The PoC messages the blocked person, and escalates to the Scrum Master only when a blocker is older than N days *and* was raised again. It never reaches the person who can unblock.
4. **There is no day-to-day continuity.** *"Everyday stand up… follow the same process unless the story is done"* (T). Yesterday's commitments are not carried into today's questions. This was not found in the flag rules or the follow-up planner.
5. **"Stalled" means more than the PoC checks.** *"If they see the dates are coming but percentage is 50%… identify those and share those"* (T). The PoC has Blocked, Overdue, Stale, No recent update and Unassigned. There is no "due soon, not far enough along" rule.
6. **The ceremony names don't match.** Rajeev describes his *sprint review* as *"what went well, what could be done better, what was the missing points"* (T). That is retrospective content. He also says *"sprint plan in the start and retrospective in the end and… between… daily stand up or escalation meeting"* (T). Our catalogue's Review and Retro split, and the missing *escalation meeting*, need confirming with him.
7. **Sprint-end KPIs come from a fixture.** He wants *"reporting from Azure DevOps… velocity… turnover rate and… 5-6 metrics"* (T). The PoC computes velocity, say/do, carry-over, blocker age, attendance and follow-up closure, but from illustrative history. We don't yet have his metric list.

None of these needs a new architecture. All of them reuse the patterns already built: prep pack, voice facilitation and approval-gated write-back. Section 5 gives the build list.

### 1.1 Where the seven gaps stand after M9 (22 November 2026)

| Gap | Now | Evidence | Still open |
|---|---|---|---|
| 1. Outlook and Calendar | **Built, Graph unchecked** | `GraphEmailSender` sends follow-up email through Graph `sendMail` when `Email:Mode=Graph` (the outbox stays the default and the record); the sprint calendar (M9d, ADR 0013) creates Teams meetings through Graph after approval | Neither Graph call has been run against a live tenant (needs `Mail.Send`, a calendar permission and an application access policy) |
| 2. Schedule ceremonies | **Built, Graph unchecked** | **Propose calendar** plans the next sprint's standups, planning, retro (and review) in code; the Scrum Master approves; Graph creates the Teams meetings and the standup join URLs are stored on the schedule; `.ics` fallback. Checked without Graph (plan, `.ics`, card, a refused approval) | The Graph calls, Outlook invites and auto-join from a calendar meeting. One event per meeting, no update or cancel when dates change |
| 3. Blocker follow-up | **Built** | Next turn asks "Who can help unblock this?"; `UnblockerResolver` matches one roster member in code; that member gets an `Unblock` card and email; the next pre-brief lists still-blocked items; escalation to the Scrum Master after `Flags:EscalateBlockerAfterDays` whether or not it was raised again | Voice turns tested at use-case level only, not in a live Voice Live session |
| 4. Day-to-day continuity | **Built** | `CommitmentCarryOver` carries open commitments from the last three started standups into `MemberBrief.PreviousCommitments`; the call-on turn asks about them first; open two standups later is `MissedCommitment` and is escalated | Same as above |
| 5. "Stalled" | **Built** | `AtRiskRule` (due within `Flags:AtRiskDays`, more than `Flags:AtRiskRemainingPct` remaining, or still New); order is blocked, overdue, at-risk, stale, no recent update. Seeded by "Prepare reconciliation report for audit" | At-risk items are flagged and asked about; they do not raise an escalation card |
| 6. Ceremony names | **Open** | Question for Rajeev (section 5) | Confirm whether his "review" is the retrospective |
| 7. KPIs from a fixture | **Open** | Unchanged: illustrative history | His metric list; Azure DevOps Analytics once there is history |

## 2. Requirement-by-requirement traceability

Status: ✅ meets · 🟡 partial · ❌ gap · ⏭ out of PoC scope (production spec).

### 2.1 The daily standup (Rajeev's "first step")

| # | What Rajeev asked (source) | PoC today | Status | Action |
|---|---|---|---|---|
| R1 | A **voice** bot that joins and hosts the Teams meeting (T; E, Nov 19 "voice agent") | Joins a real Teams meeting as a guest through ACS; Voice Live; Simulated Room fallback | ✅ | None |
| R2 | Goes *"round by round to every developer"*, in order, from the current sprint (T) | Calls each roster member; shared board shows the current sprint | ✅ | None |
| R3 | Basic questions: *"any blocker, are you good, are you done"* (T) | `turn-call-on.md` asks what they finished, what is next, and whether anything is blocking them | ✅ | None |
| R4 | *"Sprint one, backlog one. Hey, where is the status? Is it updated?"* (T): checks Azure DevOps is current | Flags NoRecentUpdate and Stale; asks about the most urgent flagged item (up to three per person) | ✅ | None |
| R5 | If blocked, *"asks for some information and then… follow up later"* (T) | Records the blocker, asks who can help unblock it, messages that person, and escalates after `Flags:EscalateBlockerAfterDays` whether or not it was raised again (M9-R2; code and tests, not run live) | ✅ | Run it in a rehearsal |
| R6 | *"Follow the same process unless the story is done"* (T): daily continuity | Open commitments from the last three standups are carried into the pre-brief and asked about first; a commitment still open two standups later is flagged and escalated (M9-R3; code and tests, not run live) | ✅ | Run it over two standups in a rehearsal |
| R7 | Human Scrum Master stays in the call at launch to check the bot (T) | "Take over", mute, skip and end controls; facilitator defers to a human Scrum Master | ✅ | None |
| R8 | *"Junior Scrum Master"*, not an assistant (T) | The agent leads the meeting; it is not a helper to a human | ✅ | Say this on slide 1 |
| R9 | Teams in San Diego, Costa Rica and India (T) | English rules; latency and accents measured only for US speakers | 🟡 | Ask for the recorded standups (E: Rajeev offered); test with them |

### 2.2 Follow-ups, actions and escalation

| # | What Rajeev asked (source) | PoC today | Status | Action |
|---|---|---|---|---|
| R10 | Follow up on commitments by **Teams chat or channel**, or in the meeting (T; D) | 1:1 Teams cards; the recap goes to the team's recap channel and the meeting chat, with every stored conversation as the fallback (M9-R5; code and tests, not run live) | ✅ | Add the bot to a channel in rehearsal, or set `PUT /api/teams/{id}/recap-channel` |
| R11 | Follow up by **email / Outlook** (T "email or a team chat"; E "Bot Access to… Outlook") | `IEmailSender` records every email in the outbox (default); `GraphEmailSender` sends it through Graph `sendMail` when `Email:Mode=Graph` (M9-R1) | 🟡 | Not run against a live mailbox: needs `Mail.Send` for the app identity and an application access policy. Run once in the demo tenant |
| R12 | *"Whom to reach out, whom to send an email. And make sure that gets clarified"* (T) | The agent asks "Who can help unblock this?", `UnblockerResolver` picks one roster member in code, that member gets an `Unblock` card and email, and the next pre-brief lists the item with its unblocker (M9-R2; code and tests, not run live) | ✅ | Run it in a rehearsal |
| R13 | *"Captures action decisions, which is technically what someone said"* (T; D) | Events (blocker, commitment, decision, parked topic) with retraction on correction | ✅ | None |
| R14 | *"Escalation is an important part… escalates… and say you take this and go continue"* (T; D) | Escalation card to the Scrum Master for long-blocked items and for missed commitments (M9). At-risk items are flagged but raise no card; a repeated no-response is a follow-up to the member, not an escalation | 🟡 | Add escalation triggers for at-risk items and repeat no-response if Rajeev wants them |
| R15 | Recommend assignments (D) | Suggested owners on the Planning page (advice); Approve plan assigns unowned items (`Assign`; M9a, ADR 0011; checked against the sandbox) | ✅ | None for the PoC |

### 2.3 Azure DevOps and backlog

| # | What Rajeev asked (source) | PoC today | Status | Action |
|---|---|---|---|---|
| R16 | **Azure DevOps only** at this point; not Pega, AWS or MuleSoft (E, [RM]) | Azure DevOps REST only | ✅ | None |
| R17 | Review the backlog, *"looking at the… next sprint"* (T; D) | Planning page and pack (M9a): readiness gaps, carry-over first, then ready items in backlog order up to the velocity range; Approve plan moves the kept items to the next iteration (the seed creates `Sprint 43`) | ✅ | The velocity history is an illustrative fixture |
| R18 | Identify overdue or stalled items: *"dates are coming but percentage is 50%"* (T; D) | Overdue, Stale, Blocked, No recent update, Unassigned, and `AtRiskRule` (due within `Flags:AtRiskDays`, remaining work over `Flags:AtRiskRemainingPct` of the estimate, or still New; M9-R4) | ✅ | Re-run `SeedAdo` so the seeded at-risk task exists |
| R19 | Many teams are low-code (Pega, Genesys, MuleSoft) (T) | Seeder assumes the Agile process (User Story, Bug, Task) | 🟡 | Ask Zava for their process template and work item types |

### 2.4 Ceremonies beyond the standup

| # | What Rajeev asked (source) | PoC today | Status | Action |
|---|---|---|---|---|
| R20 | Four or five pre-scheduled ceremonies: standup, planning, review, retrospective (T) | Standup and **retrospective** voice-led (retro checked in the Simulated Room only); planning is a portal pack with approval-gated write-back; review is a pack (API only) | 🟡 | Voice-led planning and review are not built; retro in a Teams meeting not checked |
| R21 | **Schedule** ceremonies; *"it can schedule team meetings"* (T; D; E "Calender") | Scheduler auto-starts configured standups; **Propose calendar** (M9d, ADR 0013) plans the sprint's meetings and, once approved, creates them as Teams meetings through Graph | 🟡 | Run the Graph calls once in the demo tenant; the scheduler joins standups only |
| R22 | Sprint planning: SM *"organising it… conducting that meeting"*, coaching the team *"don't over promise… not under commit"* (T) | Planning flags over- **and** under-commitment against the velocity range (M9a), and overloaded members | ✅ | The history is illustrative |
| R23 | Sprint review: *"what went well, what could be done better… missing points"* (T) | Our catalogue calls this the retrospective | ❓ | Gap 6: confirm with Rajeev whether Zava's "review" is a retrospective, and whether there is a stakeholder demo review at all |
| R24 | Between ceremonies: *"daily stand up or escalation meeting"* (T) | Not defined | ❌ | Add "Escalation meeting" to the catalogue (an ad hoc call on a blocked item); out of demo scope until confirmed |
| R25 | Prepare the retrospective (D) | The agent runs the retrospective by voice (went well / do better / missing, start-stop-continue sorting, up to three actions with owners) and approved actions become `CreateWorkItem` tasks (M9b, ADR 0012; checked in the Simulated Room) | ✅ | Run it in a Teams meeting |

### 2.5 Reporting

| # | What Rajeev asked (source) | PoC today | Status | Action |
|---|---|---|---|---|
| R26 | End-of-sprint reporting **from Azure DevOps**: velocity, *"turnover rate"*, 5-6 metrics (T) | KPIs computed in code from an illustrative fixture | 🟡 | Gap 7: read Azure DevOps Analytics once the sandbox has history; get Rajeev's metric list (he receives them by email today) |
| R27 | Monthly report for Rajeev over the last 3 to 6 sprints, for stakeholders (T; D) | Monthly report built; narrative written by the model from computed figures | ✅ | Label "illustrative" until real history |
| R28 | Generate sprint KPIs (D) | Built | ✅ | None |

### 2.6 Outcome and non-functional

| # | What Rajeev asked (source) | PoC today | Status | Action |
|---|---|---|---|---|
| R29 | Success = fewer Scrum Masters (10 to 5, then coaching only) (T) | Not measured | ⏭ | Pilot measure; production spec F16 (SM time saved) |
| R30 | Who monitors it, security, licensing (*"E7?"*), observability (T) | Production specification | ⏭ | Covered there; licensing open with Tyler |
| R31 | Identity: does the bot go into Workday to get Outlook access? (T) | Production spec: Entra Agent ID, not a Workday employee | ⏭ | Covered; Outlook access for the agent is now a PoC need (gap 1) |
| R32 | MCP servers fronted by API Management (Carol, T) | Out of PoC scope | ⏭ | Production spec |
| R33 | Step 2 later: write user stories and acceptance criteria (T) | Story Writer drafts (never written) | ✅ | Right size for "step 2" |

## 3. Things in the PoC that Rajeev did not ask for

Keep these, but don't lead with them:

- **Shared on-screen board, Models page and cost comparison, Foundry voice-agent mode.** These are useful engineering proof, not requirements.
- **Approval gating on every Azure DevOps write.** Rajeev did not ask for this explicitly, but it answers his worry: *"the confidence level… accuracy… very regulated things we work into"* (T). Present it as the answer to that worry.

## 4. Context that changes the demo story

- **Microsoft's other tracks.** Tyler's summary sets up two sessions, Microsoft 365 Copilot with Copilot Studio and GitHub Copilot, and Nate offered the Copilot Scrum Assistant template (E). Rajeev then asked whether we understood he wants a **voice agent** (E, Nov 19). Tyler confirmed and proposed a broader technical call for the week of November 30 (E, Nov 20). **This PoC is the direct answer to that question.** Put it first in that call, and position the Copilot template as an assistant for the human Scrum Master, not the agent itself.
- **Scope Rajeev set:** Azure DevOps, Teams, Calendar and Outlook only (E). Don't show GitHub repo integration as part of this agent.
- **Inputs still owed by Zava:** the recorded standup (*"I will ask team to prepare one and share"*, E), the KPI list, and the process template. The *Activities and Responsibilities* attachment is the 13-item list only, not the "8 or 10 steps" he described (T).

## 5. Build list to close the gaps (ordered for the demo)

Status on 22 November 2026: items 1 to 9 are built (items 1, 5 and 6 with the Graph calls not run against a live tenant; item 9 checked in the Simulated Room only).

| Order | Change | Gap | Size | Fits into |
|---|---|---|---|---|
| 1 | **Real email follow-ups:** Graph `sendMail` adapter behind `IEmailSender` from the agent's demo-tenant mailbox; keep the outbox as fallback | 1 | Small | New M9-R1 |
| 2 | **Unblock loop:** on a blocker, ask "who can help unblock this?"; record `UnblockerMemberId`; send that person a Teams card and email; chase daily until the item leaves Blocked; escalate to the Scrum Master after N days | 3 | Medium | M9-R2 |
| 3 | **Carry-over of commitments:** the pre-brief includes yesterday's commitments per person; the agent asks "yesterday you said you'd finish story 5, done?"; the follow-up planner flags missed commitments | 4 | Small | M9-R3 |
| 4 | **AtRiskRule:** due within `Flags:AtRiskDays` and `RemainingWork / OriginalEstimate` above `Flags:AtRiskRemainingPct` (or state still New); seed one such item | 5 | Small | M9-R4 |
| 5 | **Schedule ceremonies:** from the iteration dates, propose planning, daily standups, retro (and review if confirmed); after approval, create the Teams meetings through Graph in the demo tenant and invite the roster | 2 | Medium | M9d (upgraded) |
| 6 | **Calendar read:** use the meeting series to fill the join link automatically (no pasted link) | 1, 2 | Small | M9d |
| 7 | **Recap routing:** post the recap to the configured team channel (and the meeting chat when the bot is in it) | R10 | Small | M9-R5 |
| 8 | **Planning coaching:** over- **and** under-commitment against the velocity range | R22 | Small | M9a |
| 9 | **Voice retrospective** with "went well / do better / missing" as the default format, matching Rajeev's words | R23, R25 | Medium | M9b |

**Questions for Rajeev** (add to the next call):

1. Is your "sprint review" the same meeting as the retrospective, or is there also a stakeholder demo?
2. Which 5 or 6 sprint metrics do you get today, and how is "turnover rate" defined?
3. What is an "escalation meeting", and who runs it?
4. Which Azure DevOps process template and work item types do the Pega, Genesys and MuleSoft teams use?
5. Can you share the recorded standup, ideally one from India and one from Costa Rica?
