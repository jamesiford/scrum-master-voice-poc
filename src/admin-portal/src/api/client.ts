import { getAccessToken, type PortalConfig } from '../auth/msal';

/**
 * The one typed API client (SPEC 7.4). All HTTP goes through here so components never call fetch.
 * Every call carries the signed-in person's Entra bearer token; the API validates it and takes the caller's identity from it.
 */

/** Health read model returned by `GET /api/health`. */
export interface Health {
  status: string;
  time: string;
  sessionCount: number;
}

/** PoC error body shared by every endpoint. */
export interface ApiErrorBody {
  error: string;
  detail: string;
}

/** Team settings. */
export interface Team {
  id: string;
  name: string;
  timeZone: string;
  standupMinutes: number;
  ado: { org: string; project: string; team: string; areaPath: string };
  behaviours: { staleDays: number; escalateBlockerAfterDays: number };
  sprintGoal: string;
}

/** Editable team settings sent with `PUT /api/teams/{id}`. */
export interface TeamSettingsInput {
  name: string;
  timeZone: string;
  standupMinutes: number;
  project: string;
  adoTeam: string;
  areaPath: string;
  staleDays: number;
  escalateBlockerAfterDays: number;
  sprintGoal: string;
}

/** A roster member. */
export interface Member {
  id: string;
  teamId: string;
  key: string;
  displayName: string;
  adoUniqueName: string | null;
  pronunciation: string;
  speakingOrder: number;
  active: boolean;
  /** Share of a normal sprint the member is available for, 0 to 100. */
  availabilityPct: number;
}

/** Flags raised by the flag rules. */
export type Flag = 'Stale' | 'Blocked' | 'Overdue' | 'Unassigned' | 'NoRecentUpdate';

/** An open work item in a pre-brief. */
export interface BriefItem {
  id: { value: number };
  title: string;
  type: string;
  state: string;
  daysInState: number;
  flags: Flag[];
}

/** One member's slice of the pre-brief. */
export interface MemberBrief {
  memberId: string;
  displayName: string;
  items: BriefItem[];
  suggestedQuestion: string;
}

/** Sprint burndown figures. */
export interface Burndown {
  committedPoints: number;
  completedPoints: number;
  remainingPoints: number;
  daysLeft: number;
  atRisk: boolean;
}

/** Everything the facilitator needs before a standup. */
export interface PreBrief {
  teamId: string;
  iterationPath: string;
  sprintGoal: string;
  daysLeft: number;
  burndown: Burndown;
  members: MemberBrief[];
  unassignedItems: BriefItem[];
  teamRisks: string[];
  summary: string;
}

/** A row in the Sessions list. */
export interface SessionSummary {
  id: string;
  teamId: string;
  status: string;
  createdAt: string;
  flaggedItems: number;
  /** The ceremony: `DailyScrum` or `Retrospective`. */
  kind: string;
}

/** Session detail including the pre-brief. */
export interface Session {
  id: string;
  teamId: string;
  status: string;
  createdAt: string;
  preBrief: PreBrief | null;
  mode: string | null;
  startedAt: string | null;
  endedAt: string | null;
  events: SessionEvent[];
  attendance: Record<string, string>;
  p50LatencyMs: number;
  p95LatencyMs: number;
  questionsAsked: number;
  standupMinutes: number;
  /** The ceremony: `DailyScrum` or `Retrospective`. */
  kind: string;
  /** The W3C trace id carried through the join, each turn, the proposal, the approval and the write. */
  correlationId: string | null;
}
export interface SessionEvent {
  id: string;
  at: string;
  type: string;
  memberId: string | null;
  workItemIds: number[];
  text: string;
  /** For a retrospective note: `Start`, `Stop` or `Continue`. */
  category?: string | null;
  /** For a retrospective action: the roster member who owns it. */
  ownerMemberId?: string | null;
  retracted?: boolean;
}

/** Result of starting a session: where to open the Simulated Room. */
export interface StartResult {
  bridgeToken: string;
  roomUrl: string;
}

/** Error thrown when the API returns a non-success status. */
export class ApiError extends Error {
  /**
   * @param status - HTTP status code.
   * @param body - Parsed PoC error body, when present.
   */
  constructor(
    public readonly status: number,
    public readonly body?: ApiErrorBody,
  ) {
    super(body?.detail ?? `Request failed with status ${status}`);
  }
}

/**
 * Reads the public sign-in values (client id and tenant) the API serves without authentication. Falls back to the build-time
 * `VITE_AAD_*` values so `npm run dev` works against an API that is not serving them.
 * @returns The client id and tenant; both `null` when sign-in is not configured.
 */
export async function getPortalConfig(): Promise<PortalConfig> {
  try {
    const response = await fetch('/portal-config.json');
    if (response.ok) return (await response.json()) as PortalConfig;
  } catch {
    // Fall through to the build-time values.
  }
  return {
    clientId: import.meta.env['VITE_AAD_CLIENT_ID'] || null,
    tenantId: import.meta.env['VITE_AAD_TENANT_ID'] || null,
  };
}

async function headers(json: boolean): Promise<Record<string, string>> {
  const token = await getAccessToken();
  return {
    ...(json ? { 'content-type': 'application/json' } : {}),
    ...(token ? { authorization: `Bearer ${token}` } : {}),
  };
}

async function request<T>(
  method: string,
  path: string,
  body?: unknown,
  signal?: AbortSignal,
): Promise<T> {
  const response = await fetch(path, {
    method,
    signal,
    headers: await headers(body !== undefined),
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    const parsed = (await response.json().catch(() => undefined)) as ApiErrorBody | undefined;
    throw new ApiError(response.status, parsed);
  }
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

/**
 * Reads API health.
 * @param signal - Optional abort signal.
 * @returns The health read model.
 */
export function getHealth(signal?: AbortSignal): Promise<Health> {
  return request<Health>('GET', '/api/health', undefined, signal);
}

/**
 * Reads a team's settings.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The team.
 */
export function getTeam(teamId: string, signal?: AbortSignal): Promise<Team> {
  return request<Team>('GET', `/api/teams/${encodeURIComponent(teamId)}`, undefined, signal);
}

/**
 * Saves a team's settings.
 * @param teamId - Team id.
 * @param input - New settings.
 */
export async function updateTeam(teamId: string, input: TeamSettingsInput): Promise<void> {
  await request<unknown>('PUT', `/api/teams/${encodeURIComponent(teamId)}`, input);
}

/**
 * Reads a team's roster.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The members in speaking order.
 */
export function getRoster(teamId: string, signal?: AbortSignal): Promise<Member[]> {
  return request<Member[]>(
    'GET',
    `/api/teams/${encodeURIComponent(teamId)}/members`,
    undefined,
    signal,
  );
}

/**
 * Refreshes the roster from its source.
 * @param teamId - Team id.
 */
export async function syncRoster(teamId: string): Promise<void> {
  await request<unknown>('POST', `/api/teams/${encodeURIComponent(teamId)}/members/sync`);
}

/**
 * Prepares a standup and returns the new session id.
 * @param teamId - Team id.
 * @returns The session id.
 */
export function prepareStandup(teamId: string): Promise<string> {
  return request<string>('POST', '/api/sessions/prepare', { teamId });
}

/**
 * Lists a team's sessions, newest first.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The session rows.
 */
export function listSessions(teamId: string, signal?: AbortSignal): Promise<SessionSummary[]> {
  return request<SessionSummary[]>(
    'GET',
    `/api/sessions?teamId=${encodeURIComponent(teamId)}`,
    undefined,
    signal,
  );
}

/**
 * Starts a prepared session in the Simulated Room.
 * @param sessionId - Session id.
 * @param standupMinutes - Timebox for this run; omit to use the ceremony's default (`Retro:Minutes` for a retrospective).
 * @returns The room URL to open.
 */
export function startSession(sessionId: string, standupMinutes?: number): Promise<StartResult> {
  return request<StartResult>('POST', `/api/sessions/${encodeURIComponent(sessionId)}/start`, {
    mode: 'simulated',
    standupMinutes,
  });
}

/**
 * Reads one session with its pre-brief.
 * @param sessionId - Session id.
 * @param signal - Optional abort signal.
 * @returns The session.
 */
export function getSession(sessionId: string, signal?: AbortSignal): Promise<Session> {
  return request<Session>(
    'GET',
    `/api/sessions/${encodeURIComponent(sessionId)}`,
    undefined,
    signal,
  );
}

/** Approval lifecycle of a follow-up. */
export type FollowUpStatus = 'Pending' | 'Sent' | 'Approved' | 'Rejected' | 'Applied';

/** A change proposed to Azure DevOps. */
export interface ProposedChange {
  workItemId: number;
  op: 'Comment' | 'State' | 'Tag' | 'SetIteration' | 'Assign' | 'CreateWorkItem';
  value: string;
  rationale: string;
}

/** What a follow-up is for. Pack kinds are ceremony packs for the Scrum Master. */
export type FollowUpKind =
  'Followup' | 'Absent' | 'Escalation' | 'Unblock' | 'PlanningPack' | 'RetroActions' | 'ReviewPack';

/** A follow-up with its proposed changes. */
export interface FollowUp {
  id: string;
  teamId: string;
  sessionId: string;
  memberId: string | null;
  kind: FollowUpKind;
  message: string;
  changes: ProposedChange[];
  status: FollowUpStatus;
  standupDate: string;
  createdAt: string;
  /** Display text derived from the verified identity; `unverified (legacy)` for decisions saved before identities were recorded. */
  approvedBy: string | null;
  rejectedBy: string | null;
  /** The session's W3C trace id; the Azure DevOps comment ends with its `zsa-` short form. */
  correlationId: string;
  decidedAt: string | null;
  appliedAt: string | null;
  applyError: string | null;
}

/** A card the agent sent, with its Adaptive Card JSON. */
export interface SentCard {
  id: string;
  sentAt: string;
  message: { kind: string; title: string; text: string; sessionId: string };
  adaptiveCardJson: string;
}

/**
 * Lists a team's follow-ups.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The follow-ups, newest first.
 */
export function listFollowUps(teamId: string, signal?: AbortSignal): Promise<FollowUp[]> {
  return request<FollowUp[]>(
    'GET',
    `/api/followups?teamId=${encodeURIComponent(teamId)}`,
    undefined,
    signal,
  );
}

/**
 * Approves a follow-up as the signed-in person; the API then writes its changes to Azure DevOps.
 * @param id - Follow-up id.
 */
export async function approveFollowUp(id: string): Promise<void> {
  await request<unknown>('POST', `/api/followups/${encodeURIComponent(id)}/approve`, {});
}

/**
 * Rejects a follow-up as the signed-in person; nothing is written to Azure DevOps.
 * @param id - Follow-up id.
 */
export async function rejectFollowUp(id: string): Promise<void> {
  await request<unknown>('POST', `/api/followups/${encodeURIComponent(id)}/reject`, {});
}

/**
 * Retries writing an approved follow-up to Azure DevOps.
 * @param id - Follow-up id.
 */
export async function applyFollowUp(id: string): Promise<void> {
  await request<unknown>('POST', `/api/followups/${encodeURIComponent(id)}/apply`);
}

/**
 * Lists the cards that were sent.
 * @param signal - Optional abort signal.
 * @returns The cards, newest first.
 */
export function listCards(signal?: AbortSignal): Promise<SentCard[]> {
  return request<SentCard[]>('GET', '/api/cards', undefined, signal);
}

/** A team's schedule. */
export interface Schedule {
  teamId: string;
  enabled: boolean;
  days: string[];
  startTime: string;
  mode: 'simulated' | 'acs';
  joinUrl: string | null;
}

/** Editable schedule sent with `PUT /api/teams/{id}/schedule`. */
export interface ScheduleInput {
  enabled: boolean;
  days: string[];
  startTime: string;
  mode: 'simulated' | 'acs';
  joinUrl: string | null;
}

/**
 * Reads a team's schedule.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The schedule; `startTime` is `HH:mm:ss` from the API.
 */
export function getSchedule(teamId: string, signal?: AbortSignal): Promise<Schedule> {
  return request<Schedule>(
    'GET',
    `/api/teams/${encodeURIComponent(teamId)}/schedule`,
    undefined,
    signal,
  );
}

/**
 * Saves a team's schedule.
 * @param teamId - Team id.
 * @param input - New schedule.
 */
export async function updateSchedule(teamId: string, input: ScheduleInput): Promise<void> {
  await request<unknown>('PUT', `/api/teams/${encodeURIComponent(teamId)}/schedule`, input);
}

/**
 * Lists prompt names.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The names.
 */
export function listPrompts(teamId: string, signal?: AbortSignal): Promise<string[]> {
  return request<string[]>(
    'GET',
    `/api/teams/${encodeURIComponent(teamId)}/prompts`,
    undefined,
    signal,
  );
}

/**
 * Reads a prompt.
 * @param teamId - Team id.
 * @param name - Prompt name.
 * @param signal - Optional abort signal.
 * @returns The prompt text.
 */
export function getPrompt(teamId: string, name: string, signal?: AbortSignal): Promise<string> {
  return request<string>(
    'GET',
    `/api/teams/${encodeURIComponent(teamId)}/prompts/${encodeURIComponent(name)}`,
    undefined,
    signal,
  );
}

/**
 * Saves a prompt; it applies on the next agent turn.
 * @param teamId - Team id.
 * @param name - Prompt name.
 * @param text - New text.
 */
export async function savePrompt(teamId: string, name: string, text: string): Promise<void> {
  await request<unknown>(
    'PUT',
    `/api/teams/${encodeURIComponent(teamId)}/prompts/${encodeURIComponent(name)}`,
    { text },
  );
}

/** Live floor and timebox state of a running standup. */
export interface LiveState {
  floor: string;
  currentSpeaker: string | null;
  agentSpeaking: boolean;
  remainingSec: number;
  parkingLot: string[];
  absent: string[];
  p50: number;
  p95: number;
  running: boolean;
  ended?: boolean;
}

/**
 * Streams a standup's live state (server-sent events) with the bearer token. `EventSource` cannot set headers, so this reads
 * the response body as a stream and parses the `data:` lines. It resolves when the stream ends or is aborted.
 * @param sessionId - The standup.
 * @param onState - Called with each state.
 * @param onConnected - Called with whether the stream is open.
 * @param signal - Aborts the stream.
 */
export async function streamLiveState(
  sessionId: string,
  onState: (state: LiveState) => void,
  onConnected: (connected: boolean) => void,
  signal: AbortSignal,
): Promise<void> {
  try {
    const response = await fetch(`/api/sessions/${encodeURIComponent(sessionId)}/live`, {
      headers: await headers(false),
      signal,
    });
    if (!response.ok || !response.body) return;
    onConnected(true);
    const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
    let buffer = '';
    for (;;) {
      const { done, value } = await reader.read();
      if (done) break;
      buffer += value;
      const parts = buffer.split('\n\n');
      buffer = parts.pop() ?? '';
      for (const part of parts) {
        const data = part
          .split('\n')
          .filter((line) => line.startsWith('data:'))
          .map((line) => line.slice(5).trimStart())
          .join('\n');
        if (data) onState(JSON.parse(data) as LiveState);
      }
    }
  } catch {
    // Aborted or dropped: the page shows "disconnected".
  } finally {
    onConnected(false);
  }
}

/**
 * Steers a running standup.
 * @param sessionId - Session id.
 * @param action - begin, mute, unmute, skip, takeover, release or end.
 */
export async function controlSession(sessionId: string, action: string): Promise<void> {
  await request<unknown>('POST', `/api/sessions/${encodeURIComponent(sessionId)}/control`, {
    action,
  });
}

/**
 * Starts a standup in a Teams meeting.
 * @param sessionId - Session id.
 * @param joinUrl - Teams meeting link.
 * @param standupMinutes - Timebox; omit to use the ceremony's default.
 */
export async function startTeamsSession(
  sessionId: string,
  joinUrl: string,
  standupMinutes?: number,
): Promise<void> {
  await request<unknown>('POST', `/api/sessions/${encodeURIComponent(sessionId)}/start`, {
    mode: 'acs',
    joinUrl,
    standupMinutes,
  });
}

/** Counts of demo records. */
export interface DemoStatus {
  sessions: number;
  followUps: number;
  cards: number;
}

/**
 * Reads how much demo state exists.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The counts.
 */
export function getDemoStatus(teamId: string, signal?: AbortSignal): Promise<DemoStatus> {
  return request<DemoStatus>(
    'GET',
    `/api/demo/status?teamId=${encodeURIComponent(teamId)}`,
    undefined,
    signal,
  );
}

/** Clears sessions, follow-ups and cards. Azure DevOps is not touched. */
export async function resetDemo(): Promise<void> {
  await request<unknown>('POST', '/api/demo/reset');
}

/** Team KPIs over a window of sprints. */
export interface KpiSummary {
  sprints: number;
  sayDoRatio: number;
  velocity: number;
  carryOverItems: number;
  avgBlockerAgeDays: number;
  avgCycleTimeDays: number;
  avgAttendancePct: number;
  avgFollowUpClosurePct: number;
  agentRunStandups: number;
  avgStandupMinutes: number;
}

/** One finished sprint. */
export interface SprintHistory {
  name: string;
  committedPoints: number;
  completedPoints: number;
  carryOverItems: number;
}

/** KPIs and the sprints behind them. */
export interface KpiReport {
  summary: KpiSummary;
  sprints: SprintHistory[];
  illustrative: boolean;
}

/** A written report. */
export interface ReportDocument {
  title: string;
  markdown: string;
  narrative: string;
  narrativeFromModel: boolean;
  illustrative: boolean;
}

/**
 * Reads KPIs.
 * @param teamId - Team id.
 * @param sprints - Window size, three to six.
 * @param signal - Optional abort signal.
 * @returns The KPIs.
 */
export function getKpis(teamId: string, sprints: number, signal?: AbortSignal): Promise<KpiReport> {
  return request<KpiReport>(
    'GET',
    `/api/reports/kpis?teamId=${encodeURIComponent(teamId)}&sprints=${sprints}`,
    undefined,
    signal,
  );
}

/**
 * Writes the sprint summary or monthly report.
 * @param kind - Which report.
 * @param teamId - Team id.
 * @param sprints - Window size for the monthly report.
 * @param signal - Optional abort signal.
 * @returns The report.
 */
export function getReport(
  kind: 'sprint' | 'monthly',
  teamId: string,
  sprints: number,
  signal?: AbortSignal,
): Promise<ReportDocument> {
  return request<ReportDocument>(
    'GET',
    `/api/reports/${kind}?teamId=${encodeURIComponent(teamId)}&sprints=${sprints}`,
    undefined,
    signal,
  );
}

/**
 * Asks the Reporting agent a question.
 * @param teamId - Team id.
 * @param question - The question.
 * @returns The answer text.
 */
export function askAgent(teamId: string, question: string): Promise<string> {
  return request<string>('POST', '/api/ask', { teamId, question });
}

/** One member's open load. */
export interface MemberLoad {
  memberId: string;
  displayName: string;
  openItems: number;
  openPoints: number;
  remainingHours: number;
}

/** Sprint planning prep. */
export interface PlanningPrep {
  loads: MemberLoad[];
  carryOver: {
    workItemId: number;
    title: string;
    state: string;
    points: number | null;
    ownerName: string | null;
  }[];
  notReady: { workItemId: number; title: string; reasons: string[] }[];
  suggestions: { workItemId: number; title: string; displayName: string; reason: string }[];
}

/**
 * Reads the sprint planning prep.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The prep.
 */
export function getPlanning(teamId: string, signal?: AbortSignal): Promise<PlanningPrep> {
  return request<PlanningPrep>(
    'GET',
    `/api/planning?teamId=${encodeURIComponent(teamId)}`,
    undefined,
    signal,
  );
}

/** A drafted user story (draft only; never written to Azure DevOps without approval). */
export interface StoryDraft {
  title: string;
  story: string;
  acceptanceCriteria: string[];
  fromModel: boolean;
}

/** One row of the realtime model comparison. */
export interface ModelRow {
  name: string;
  isCurrent: boolean;
  costPerStandupUsd: number;
  measuredP50Ms: number | null;
  measuredP95Ms: number | null;
  notes: string;
}

/** A follow-up email recorded in the outbox. */
export interface SentEmail {
  to: string;
  subject: string;
  body: string;
  followUpId: string | null;
}

/**
 * Prepares a retrospective session to run by voice. Nothing is written to Azure DevOps.
 * @param teamId - Team id.
 * @returns The session id.
 */
export function prepareRetrospective(teamId: string): Promise<string> {
  return request<string>('POST', '/api/retrospective/prepare', { teamId });
}

/**
 * Drafts a start-stop-continue retrospective.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The retrospective document.
 */
export function getRetrospective(teamId: string, signal?: AbortSignal): Promise<ReportDocument> {
  return request<ReportDocument>(
    'GET',
    `/api/retrospective?teamId=${encodeURIComponent(teamId)}`,
    undefined,
    signal,
  );
}

/**
 * Lists topics parked in recent standups.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The topics.
 */
export function getParkedTopics(teamId: string, signal?: AbortSignal): Promise<string[]> {
  return request<string[]>(
    'GET',
    `/api/stories/topics?teamId=${encodeURIComponent(teamId)}`,
    undefined,
    signal,
  );
}

/**
 * Drafts a user story from a topic.
 * @param teamId - Team id.
 * @param topic - The topic or parked decision.
 * @returns The draft.
 */
export function draftStory(teamId: string, topic: string): Promise<StoryDraft> {
  return request<StoryDraft>('POST', '/api/stories/draft', { teamId, topic });
}

/**
 * Compares realtime models on estimated cost and measured latency.
 * @param teamId - Team id.
 * @param signal - Optional abort signal.
 * @returns The rows.
 */
export function getModelComparison(teamId: string, signal?: AbortSignal): Promise<ModelRow[]> {
  return request<ModelRow[]>(
    'GET',
    `/api/models?teamId=${encodeURIComponent(teamId)}`,
    undefined,
    signal,
  );
}

/**
 * Lists follow-up emails sent so far.
 * @param signal - Optional abort signal.
 * @returns The emails.
 */
export function getEmails(signal?: AbortSignal): Promise<SentEmail[]> {
  return request<SentEmail[]>('GET', '/api/emails', undefined, signal);
}

/** Where a planned item comes from. */
export type PlanningSource = 'CarryOver' | 'Backlog';

/** An item the plan proposes for the next sprint. */
export interface PlannedItem {
  workItemId: number;
  type: string;
  title: string;
  points: number;
  source: PlanningSource;
  ownerMemberId: string | null;
  ownerName: string | null;
  /** True when the item had no owner and approving the plan assigns the suggested one. */
  ownerSuggested: boolean;
}

/** An item the plan leaves out, with why. */
export interface ExcludedItem {
  workItemId: number;
  title: string;
  points: number | null;
  reasons: string[];
}

/** A member's capacity for the next sprint and what is planned on them. */
export interface MemberCapacity {
  memberId: string;
  displayName: string;
  availabilityPct: number;
  capacityLow: number;
  capacityHigh: number;
  plannedPoints: number;
  overloaded: boolean;
}

/** The computed proposal; every figure comes from code. */
export interface PlanningProposal {
  velocity: { low: number; high: number };
  members: MemberCapacity[];
  selected: PlannedItem[];
  excluded: ExcludedItem[];
  totalPoints: number;
  commitment: 'Within' | 'Over' | 'Under';
  warnings: string[];
}

/** A prepared sprint plan. */
export interface PlanningPack {
  id: string;
  teamId: string;
  createdAt: string;
  iterationPath: string;
  nextIterationPath: string | null;
  velocityIllustrative: boolean;
  proposal: PlanningProposal;
  draftGoal: string;
  goalFromModel: boolean;
  status: 'Prepared' | 'Approved' | 'Rejected';
  decidedBy: string | null;
  decidedAt: string | null;
  approvedItemIds: number[];
  followUpId: string | null;
}

/**
 * Prepares the sprint planning pack. Nothing is written to Azure DevOps.
 * @param teamId - The team.
 * @returns The pack id.
 */
export function preparePlanning(teamId: string): Promise<string> {
  return request<string>('POST', '/api/planning/prepare', { teamId });
}

/**
 * Reads the team's latest planning pack.
 * @param teamId - The team.
 * @param signal - Abort signal.
 * @returns The pack, or `null` when none was prepared yet.
 */
export async function getPlanningPack(
  teamId: string,
  signal?: AbortSignal,
): Promise<PlanningPack | null> {
  try {
    return await request<PlanningPack>(
      'GET',
      `/api/planning/pack?teamId=${encodeURIComponent(teamId)}`,
      undefined,
      signal,
    );
  } catch (e: unknown) {
    if (signal?.aborted) throw e;
    return null;
  }
}

/**
 * Approves a sprint plan. The kept items become an approved Scrum Master follow-up that writes to Azure DevOps (F7).
 * @param packId - The pack.
 * @param selectedItemIds - The proposed items to keep.
 * @returns The follow-up id.
 */
export function approvePlan(packId: string, selectedItemIds: number[]): Promise<string> {
  return request<string>('POST', `/api/planning/packs/${encodeURIComponent(packId)}/approve`, {
    selectedItemIds,
  });
}

/**
 * Rejects a sprint plan as the signed-in person. Nothing is written.
 * @param packId - The pack.
 */
export async function rejectPlan(packId: string): Promise<void> {
  await request<unknown>('POST', `/api/planning/packs/${encodeURIComponent(packId)}/reject`, {});
}

/**
 * Sets how much of a normal sprint a member is available for.
 * @param teamId - The team.
 * @param memberId - The member.
 * @param percent - Availability, 0 to 100.
 */
export async function updateAvailability(
  teamId: string,
  memberId: string,
  percent: number,
): Promise<void> {
  await request<unknown>(
    'PUT',
    `/api/teams/${encodeURIComponent(teamId)}/members/${encodeURIComponent(memberId)}/availability`,
    { percent },
  );
}

/**
 * Reads one follow-up, for example to show whether an approved plan was written.
 * @param id - The follow-up.
 * @param signal - Abort signal.
 * @returns The follow-up.
 */
export function getFollowUp(id: string, signal?: AbortSignal): Promise<FollowUp> {
  return request<FollowUp>('GET', `/api/followups/${encodeURIComponent(id)}`, undefined, signal);
}

/** One meeting in a sprint calendar. */
export interface CalendarMeeting {
  key: string;
  kind: 'Planning' | 'Standup' | 'Review' | 'Retrospective';
  title: string;
  startsAt: string;
  endsAt: string;
  eventId: string | null;
  joinUrl: string | null;
}

/** The planned sprint calendar. */
export interface SprintCalendarPlan {
  iterationPath: string;
  timeZone: string;
  meetings: CalendarMeeting[];
}

/** A stored sprint calendar proposal. */
export interface SprintCalendarProposal {
  id: string;
  iterationPath: string;
  status: 'Proposed' | 'Approving' | 'Approved' | 'Rejected';
  meetings: CalendarMeeting[];
  decidedBy: string | null;
  applyError: string | null;
}

/**
 * Plans the next sprint's calendar from its dates and the team's settings. Nothing is stored.
 * @param teamId - The team.
 * @param signal - Optional abort signal.
 * @returns The plan.
 */
export function getSprintCalendar(
  teamId: string,
  signal?: AbortSignal,
): Promise<SprintCalendarPlan> {
  return request<SprintCalendarPlan>(
    'GET',
    `/api/calendar?teamId=${encodeURIComponent(teamId)}`,
    undefined,
    signal,
  );
}

/**
 * Lists the team's sprint calendar proposals, newest first.
 * @param teamId - The team.
 * @param signal - Optional abort signal.
 * @returns The proposals.
 */
export function listSprintCalendars(
  teamId: string,
  signal?: AbortSignal,
): Promise<SprintCalendarProposal[]> {
  return request<SprintCalendarProposal[]>(
    'GET',
    `/api/calendar/proposals?teamId=${encodeURIComponent(teamId)}`,
    undefined,
    signal,
  );
}

/**
 * Proposes the sprint calendar: stores it and sends the card to the Scrum Master. Nothing is created in any calendar.
 * @param teamId - The team.
 * @returns The proposal id.
 */
export function proposeSprintCalendar(teamId: string): Promise<string> {
  return request<string>('POST', '/api/calendar/propose', { teamId });
}

/**
 * Approves or rejects a proposal. Approving creates the Teams meetings in the agent's calendar.
 * @param id - The proposal id.
 * @param approve - True to approve, false to reject.
 */
export async function decideSprintCalendar(id: string, approve: boolean): Promise<void> {
  await request<unknown>(
    'POST',
    `/api/calendar/proposals/${encodeURIComponent(id)}/${approve ? 'approve' : 'reject'}`,
    {},
  );
}
