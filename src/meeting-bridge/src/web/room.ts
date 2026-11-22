import {
  AgentPlayer,
  MicrophoneStreamer,
  SAMPLE_RATE,
  base64ToBytes,
  playCastLine,
  streamAsMicrophone,
} from './audio.js';
import type {
  CastMemberInfo,
  ClientMessage,
  RoomMember,
  ServerMessage,
  SpokenLine,
  UiState,
} from './protocol.js';

/**
 * The Simulated Room (SPEC 7.3): roster, captions, timebox, parking lot, push-to-talk and the demo cast player.
 * It speaks the bridge protocol (SPEC 8) to the voice gateway; all conversation decisions are made there.
 */

const params = new URLSearchParams(location.search);
const sessionId = location.pathname.split('/').filter(Boolean).pop() ?? '';
const token = params.get('token') ?? '';
const apiBase = params.get('api') ?? '';

const $ = <T extends HTMLElement>(id: string): T => {
  const el = document.getElementById(id);
  if (!el) throw new Error(`Missing element #${id}`);
  return el as T;
};

const colors = ['#2f80ed', '#9b51e0', '#27ae60', '#e2793a', '#d6409f', '#00a3a3'];

let socket: WebSocket;
let members: RoomMember[] = [];
let cast: CastMemberInfo[] = [];
let standupSeconds = 900;
let kind = 'DailyScrum';
let started = false;
let castBusy = false;
let deferredTurn: string | undefined;
let lastAgentCaption = '';
let previousFloor = '';
let ui: UiState | undefined;
let muted = false;
let context: AudioContext;
let player: AgentPlayer;
let mic: MicrophoneStreamer | undefined;

function send(message: ClientMessage): void {
  if (socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify(message));
}

function setStatus(text: string, kind: 'ok' | 'bad' | '' = ''): void {
  const el = $('status');
  el.textContent = text;
  el.className = kind;
}

function banner(kind: 'error' | 'info' | '', text = ''): void {
  const el = $('banner');
  el.className = kind;
  el.textContent = text;
}

function connect(): void {
  if (!token || !apiBase) {
    banner(
      'error',
      'This room needs a session link. Start the session from the portal (Sessions > Start in Simulated Room).',
    );
    setStatus('No session', 'bad');
    return;
  }
  context = new AudioContext({ sampleRate: SAMPLE_RATE });
  player = new AgentPlayer(
    context,
    () => send({ type: 'agentAudioState', state: 'idle' }),
    () => undefined,
  );
  const url = `${apiBase.replace(/^http/, 'ws')}/ws/bridge/${encodeURIComponent(sessionId)}?mode=simulated&token=${encodeURIComponent(token)}`;
  socket = new WebSocket(url);
  socket.binaryType = 'arraybuffer';
  socket.onopen = () => setStatus('Connected', 'ok');
  socket.onclose = () => {
    setStatus('Disconnected', 'bad');
    if (!started || ui?.floor !== 'Closed')
      banner('error', 'Disconnected from the API. Reload the room or start a new session.');
  };
  socket.onerror = () => banner('error', 'Could not reach the API. Is it running?');
  socket.onmessage = (e: MessageEvent<string | ArrayBuffer>) => {
    if (typeof e.data === 'string') void onMessage(JSON.parse(e.data) as ServerMessage);
    else player.enqueue(new Uint8Array(e.data));
  };
}

async function onMessage(message: ServerMessage): Promise<void> {
  switch (message.type) {
    case 'hello':
      members = message.members;
      kind = message.kind ?? 'DailyScrum';
      standupSeconds = message.standupMinutes * 60;
      await loadCast();
      renderRoster();
      fillPresenter();
      $<HTMLButtonElement>('start').disabled = false;
      break;
    case 'caption':
      addCaption(message.role, message.memberId, message.text);
      if (message.role === 'agent') lastAgentCaption = message.text;
      break;
    case 'ui':
      onUi(message.state);
      break;
    case 'control':
      if (message.action === 'flush') player.flush();
      if (message.action === 'leave')
        banner('info', 'Standup finished. The recap and events are in the portal.');
      break;
    case 'error':
      banner('error', message.message);
      break;
    case 'info':
      banner('info', message.message);
      break;
    default:
      break;
  }
}

async function loadCast(): Promise<void> {
  const response = await fetch(`/cast/script?kind=${encodeURIComponent(kind)}`);
  cast = response.ok ? ((await response.json()) as { members: CastMemberInfo[] }).members : [];
  if (!response.ok)
    banner(
      'error',
      'The demo cast could not be loaded. You can still run the room, but cast members will stay silent.',
    );
}

function nameOf(memberId: string | null): string {
  return members.find((m) => m.memberId === memberId)?.displayName ?? 'Agent';
}

function presenter(): string {
  return $<HTMLSelectElement>('me').value;
}

function isAbsent(memberId: string): boolean {
  return (
    cast.find((c) => c.memberId === memberId)?.absent === true ||
    ui?.absent.includes(memberId) === true
  );
}

function fillPresenter(): void {
  const select = $<HTMLSelectElement>('me');
  select.replaceChildren(...members.map((m) => new Option(m.displayName, m.memberId)));
  select.value =
    members.find((m) => m.memberId === 'm-admin')?.memberId ?? members[0]?.memberId ?? '';
  select.onchange = () => {
    send({ type: 'localSpeaker', memberId: presenter() });
    renderRoster();
  };
  $<HTMLInputElement>('autopilot').checked = params.get('autopilot') === '1';
}

function renderRoster(): void {
  const root = $('roster');
  root.replaceChildren(
    ...members.map((m, i) => {
      const row = document.createElement('div');
      row.className =
        'member' +
        (ui?.currentSpeaker === m.memberId ? ' speaking' : '') +
        (isAbsent(m.memberId) ? ' absent' : '');
      const avatar = document.createElement('div');
      avatar.className = 'avatar';
      avatar.style.background = colors[i % colors.length] ?? '#2f80ed';
      avatar.textContent = m.displayName
        .split(' ')
        .map((p) => p[0])
        .join('')
        .slice(0, 2);
      const name = document.createElement('span');
      name.textContent = m.displayName;
      row.append(avatar, name);
      if (m.memberId === (members.length ? presenter() : '')) row.append(tag('you'));
      if (isAbsent(m.memberId)) row.append(tag('absent'));
      return row;
    }),
  );
}

function tag(text: string): HTMLElement {
  const el = document.createElement('span');
  el.className = 'tag';
  el.textContent = text;
  return el;
}

function addCaption(role: 'agent' | 'participant', memberId: string | null, text: string): void {
  const box = $('captions');
  const el = document.createElement('div');
  el.className = `caption ${role}`;
  const who = document.createElement('b');
  who.textContent = role === 'agent' ? 'Scrum Agent' : nameOf(memberId);
  el.append(who, document.createTextNode(text));
  box.append(el);
  box.scrollTop = box.scrollHeight;
}

function onUi(state: UiState): void {
  const entering = state.floor === 'Listening' && previousFloor !== 'Listening';
  previousFloor = state.floor;
  ui = state;
  $('floor').textContent =
    `${state.floor}${state.currentSpeaker ? ' - ' + nameOf(state.currentSpeaker) : ''}`;
  const total = Math.max(1, standupSeconds);
  const bar = $('timebar');
  bar.style.width = `${Math.max(0, Math.min(100, (state.remainingSec / total) * 100))}%`;
  bar.className = state.remainingSec <= total * 0.2 ? 'warn' : '';
  $('remaining').textContent =
    `${Math.floor(state.remainingSec / 60)}:${String(state.remainingSec % 60).padStart(2, '0')} left`;
  $('parking').replaceChildren(
    ...state.parkingLot.map((t) => Object.assign(document.createElement('li'), { textContent: t })),
  );
  $('latency').textContent = `p50 ${state.p50 || '--'} ms, p95 ${state.p95 || '--'} ms`;
  for (const id of ['takeover', 'skip', 'mute', 'end'])
    $<HTMLButtonElement>(id).disabled = !state.running || state.floor === 'Closed';
  renderRoster();
  if (state.floor === 'Closed') {
    setStatus('Standup finished', 'ok');
    mic?.stop();
  }
  if (entering && state.currentSpeaker) void runCastTurn(state.currentSpeaker);
}

async function requestLine(
  memberId: string,
): Promise<{ line: SpokenLine | null; sidebar: SpokenLine[] }> {
  const response = await fetch('/cast/line', {
    method: 'POST',
    headers: { 'content-type': 'application/json' },
    body: JSON.stringify({ sessionId, memberId, question: lastAgentCaption, kind }),
  });
  if (!response.ok) {
    banner(
      'error',
      `The demo cast failed (${response.status}). Speak your own lines or press Skip speaker.`,
    );
    return { line: null, sidebar: [] };
  }
  return (await response.json()) as { line: SpokenLine | null; sidebar: SpokenLine[] };
}

/** Called when the floor passes to a member: cast members answer; the presenter speaks live unless autopilot is on. */
async function runCastTurn(memberId: string): Promise<void> {
  const mine = memberId === presenter();
  if (mine && !$<HTMLInputElement>('autopilot').checked) return;
  if (castBusy) {
    // A side discussion may still be playing when the agent asks its question; answer as soon as it ends.
    deferredTurn = memberId;
    return;
  }
  castBusy = true;
  try {
    const { line, sidebar } = await requestLine(memberId);
    if (!line) return;
    if (mine) await sayAsMicrophone(line);
    else await sayAsCast(line);
    for (const side of sidebar) {
      // The agent's interjection ends the side discussion. The gap between lines (shorter than the silence timer) lets floor
      // control see a pause; the first line follows the member's line at once so their update is not reconciled mid-discussion.
      if (side !== sidebar[0]) await new Promise((r) => setTimeout(r, 650));
      if (player.playing || ui?.floor !== 'Listening') break;
      await sayAsCast(side);
    }
  } finally {
    castBusy = false;
    const next = deferredTurn;
    deferredTurn = undefined;
    if (next && ui?.floor === 'Listening' && ui.currentSpeaker === next) void runCastTurn(next);
  }
}

function surfaceSpeechError(line: SpokenLine): void {
  if (line.error)
    banner(
      'error',
      `Speech synthesis failed for ${nameOf(line.memberId)}: ${line.error}. Showing text only.`,
    );
}

async function sayAsCast(line: SpokenLine): Promise<void> {
  surfaceSpeechError(line);
  const pcm = line.audio ? base64ToBytes(line.audio) : undefined;
  // Tell the gateway someone is talking, as Teams does, so a side discussion holds back the silence timer.
  send({ type: 'speaking', memberId: line.memberId, active: true });
  const length = pcm
    ? await playCastLine(context, pcm)
    : Math.max(1500, line.text.split(/\s+/).length * 400);
  send({ type: 'speaking', memberId: line.memberId, active: false });
  send({ type: 'utterance', memberId: line.memberId, text: line.text, durationMs: length });
}

/** Autopilot presenter: the line is spoken aloud and also streamed as microphone audio, so the real speech-to-text path runs. */
async function sayAsMicrophone(line: SpokenLine): Promise<void> {
  surfaceSpeechError(line);
  if (!line.audio) return;
  const pcm = base64ToBytes(line.audio);
  void playCastLine(context, pcm);
  await streamAsMicrophone(pcm, (frame) => socket.send(frame));
}

function micGate(): boolean {
  return (
    $<HTMLInputElement>('micopen').checked && !castBusy && !player.playing && ui?.floor !== 'Closed'
  );
}

async function startStandup(): Promise<void> {
  await context.resume();
  started = true;
  banner('', '');
  $<HTMLButtonElement>('start').disabled = true;
  $<HTMLSelectElement>('me').disabled = true;
  send({ type: 'localSpeaker', memberId: presenter() });
  send({
    type: 'participants',
    items: members.map((m) => ({
      memberId: m.memberId,
      displayName: m.displayName,
      present: !isAbsent(m.memberId),
    })),
  });
  send({ type: 'command', action: 'begin', memberId: presenter() });
  if (!$<HTMLInputElement>('autopilot').checked) await startMic();
}

async function startMic(): Promise<void> {
  try {
    mic = new MicrophoneStreamer(context, (frame) => {
      if (micGate() && socket.readyState === WebSocket.OPEN) socket.send(frame);
    });
    await mic.start();
  } catch (error) {
    banner(
      'error',
      `Microphone unavailable (${error instanceof Error ? error.message : 'denied'}). Tick Autopilot to let the cast speak for you.`,
    );
  }
}

function wireControls(): void {
  $('start').onclick = () => void startStandup();
  $('takeover').onclick = () => {
    const button = $('takeover');
    const on = button.dataset['on'] !== '1';
    button.dataset['on'] = on ? '1' : '0';
    button.textContent = on ? 'Hand back to agent' : "I'll take it from here";
    send({ type: 'command', action: on ? 'takeover' : 'release' });
  };
  $('skip').onclick = () => send({ type: 'command', action: 'skip' });
  $('mute').onclick = () => {
    muted = !muted;
    $('mute').textContent = muted ? 'Unmute agent' : 'Mute agent';
    send({ type: 'command', action: muted ? 'mute' : 'unmute' });
  };
  $('end').onclick = () => send({ type: 'command', action: 'end' });
}

wireControls();
connect();
