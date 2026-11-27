/** Messages between the Simulated Room and the voice gateway (SPEC 8). Mirrors `Zava.Scrum.Api/Voice/VoiceGateway.cs`. */

/** A roster member sent in `hello`. */
export interface RoomMember {
  memberId: string;
  displayName: string;
  /** Verified roster id used only to label participant names, never to approve a change. */
  entraObjectId?: string | null;
}

/** State pushed by the gateway about once a second and after every action. */
export interface UiState {
  /** Owning team and session for film evidence emitted on this authenticated connection. */
  teamId?: string;
  sessionId?: string;
  /** Actual participant labels, independent of name-based ACS floor/presence matching. */
  participants?: {
    participantId: string;
    memberId: string | null;
    displayName: string;
    guest: boolean;
  }[];
  /** Locally measured browser send/playback intervals, not network delivery. */
  bridgeHops?: { hop: string; p50Ms: number; p95Ms: number; n: number }[];
  floor: string;
  currentSpeaker: string | null;
  agentSpeaking: boolean;
  remainingSec: number;
  parkingLot: string[];
  absent: string[];
  p50: number;
  p95: number;
  running: boolean;
}

/** What the shared screen shows: the current speaker's items and what has been covered. */
export interface BoardState {
  ended: boolean;
  sprint: string;
  goal: string;
  daysLeft: number;
  speaker: string | null;
  focus: number | null;
  items: {
    id: number;
    owner: string | null;
    title: string;
    type: string;
    state: string;
    flags: string[];
    covered: boolean;
  }[];
  team: { name: string; total: number; covered: number; status?: string | null }[];
}

/** What the shared screen shows in a retrospective: three columns that fill as members speak, then the actions. */
export interface RetroBoardState {
  kind: 'retro';
  ended: boolean;
  sprint: string;
  phase: 'Round' | 'Actions';
  speaker: string | null;
  columns: { key: string; label: string; notes: { text: string; member: string }[] }[];
  themes: { name: string; count: number }[];
  actions: { text: string; owner: string }[];
  team: { name: string; status?: string | null }[];
}

/** Either board the gateway can push. */
export type AnyBoardState = BoardState | RetroBoardState;

/** Gateway to room. */
export type ServerMessage =
  | ({ type: 'board' } & AnyBoardState)
  | {
      type: 'hello';
      sessionId: string;
      teamId?: string;
      traceParent?: string;
      mode: string;
      /** The ceremony: `DailyScrum` or `Retrospective`. Selects the cast script. */
      kind?: string;
      version: number;
      standupMinutes: number;
      members: RoomMember[];
    }
  | { type: 'caption'; role: 'agent' | 'participant'; memberId: string | null; text: string }
  | { type: 'ui'; state: UiState }
  | { type: 'agentAudioState'; state: 'speaking' | 'idle' }
  | { type: 'control'; action: 'flush' | 'leave' | 'mute' | 'unmute' }
  | { type: 'error'; code: string; message: string }
  | { type: 'info'; code: string; message: string };

/** Room to gateway. */
export type ClientMessage =
  | {
      type: 'participantNames';
      teamId: string;
      sessionId: string;
      items: { participantId: string; objectId: string | null; displayName: string }[];
    }
  | { type: 'telemetry'; hop: 'bridge.audio' | 'bridge.playback'; durationMs: number }
  | { type: 'participants'; items: { memberId: string; displayName: string; present: boolean }[] }
  | { type: 'localSpeaker'; memberId: string }
  | { type: 'speaking'; memberId: string; active: boolean }
  | { type: 'utterance'; memberId: string; text: string; durationMs: number }
  | { type: 'agentAudioState'; state: 'idle' | 'speaking' }
  | { type: 'callState'; state: string; message: string }
  | {
      type: 'command';
      action: 'begin' | 'takeover' | 'release' | 'mute' | 'unmute' | 'skip' | 'end';
      memberId?: string;
    };

/** A cast member as listed by the bridge. */
export interface CastMemberInfo {
  memberId: string;
  voice: string;
  absent: boolean;
}

/** A spoken cast line returned by `POST /cast/line`. */
export interface SpokenLine {
  memberId: string;
  text: string;
  audio: string | null;
  error?: string;
}
