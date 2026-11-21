/**
 * The one typed API client (SPEC 7.4). All HTTP goes through here so components never call fetch.
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
}

/** One thing that happened in a standup. */
export interface SessionEvent {
  id: string;
  at: string;
  type: string;
  memberId: string | null;
  workItemIds: number[];
  text: string;
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

async function request<T>(
  method: string,
  path: string,
  body?: unknown,
  signal?: AbortSignal,
): Promise<T> {
  const response = await fetch(path, {
    method,
    signal,
    headers: body === undefined ? undefined : { 'content-type': 'application/json' },
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
 * @param standupMinutes - Timebox for this run.
 * @returns The room URL to open.
 */
export function startSession(sessionId: string, standupMinutes: number): Promise<StartResult> {
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
  op: 'Comment' | 'State' | 'Tag';
  value: string;
  rationale: string;
}

/** A follow-up with its proposed changes. */
export interface FollowUp {
  id: string;
  teamId: string;
  sessionId: string;
  memberId: string | null;
  kind: 'Followup' | 'Absent' | 'Escalation';
  message: string;
  changes: ProposedChange[];
  status: FollowUpStatus;
  standupDate: string;
  createdAt: string;
  approvedBy: string | null;
  rejectedBy: string | null;
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
 * Approves a follow-up; the API then writes its changes to Azure DevOps.
 * @param id - Follow-up id.
 * @param approver - Name recorded as the approver.
 */
export async function approveFollowUp(id: string, approver: string): Promise<void> {
  await request<unknown>('POST', `/api/followups/${encodeURIComponent(id)}/approve`, { approver });
}

/**
 * Rejects a follow-up; nothing is written to Azure DevOps.
 * @param id - Follow-up id.
 * @param approver - Name recorded as the person who rejected.
 */
export async function rejectFollowUp(id: string, approver: string): Promise<void> {
  await request<unknown>('POST', `/api/followups/${encodeURIComponent(id)}/reject`, { approver });
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
 * @param standupMinutes - Timebox.
 */
export async function startTeamsSession(
  sessionId: string,
  joinUrl: string,
  standupMinutes: number,
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
