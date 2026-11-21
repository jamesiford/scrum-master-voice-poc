/** Messages between the Simulated Room and the voice gateway (SPEC 8). Mirrors `Mcm.Scrum.Api/Voice/VoiceGateway.cs`. */

/** A roster member sent in `hello`. */
export interface RoomMember {
  memberId: string;
  displayName: string;
}

/** State pushed by the gateway about once a second and after every action. */
export interface UiState {
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
  team: { name: string; total: number; covered: number }[];
}

/** Gateway to room. */
export type ServerMessage =
  | ({ type: 'board' } & BoardState)
  | {
      type: 'hello';
      sessionId: string;
      mode: string;
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
