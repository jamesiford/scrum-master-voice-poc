import {
  Features,
  LocalAudioStream,
  LocalVideoStream,
  CallClient,
  type Call,
  type RemoteParticipant,
} from '@azure/communication-calling';
import { AzureCommunicationTokenCredential } from '@azure/communication-common';
import { AgentPlayer, SAMPLE_RATE } from './audio.js';
import { BoardRenderer } from './board.js';
import type { ClientMessage, RoomMember, ServerMessage } from './protocol.js';

/**
 * The Teams call page (SPEC 7.3, M4). Runs in headless Chromium: joins the meeting as an ACS external user, feeds the
 * meeting's mixed audio to the voice gateway as PCM16 24 kHz frames, and plays the agent's audio into the call through a
 * virtual microphone. It makes no conversation decisions; the gateway owns the floor (SPEC 11.3).
 */

const params = new URLSearchParams(location.search);
const sessionId = location.pathname.split('/').filter(Boolean).pop() ?? '';
const token = params.get('token') ?? '';
const apiBase = params.get('api') ?? '';
const joinUrl = params.get('join') ?? '';
const displayName = params.get('name') ?? 'Zava Scrum Agent';
const beginDelayMs = Number(params.get('beginDelayMs') ?? 8000);

let socket: WebSocket;
let call: Call | undefined;
let members: RoomMember[] = [];
let begun = false;
let player: AgentPlayer;
let context: AudioContext;
let localAudio: LocalAudioStream;
let sharing = false;
const board = new BoardRenderer();
const wired = new Set<string>();

function status(text: string): void {
  const el = document.getElementById('status');
  if (el) el.textContent = text;
  (window as unknown as { __teamsState: string }).__teamsState = text;
}

function send(message: ClientMessage): void {
  if (socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify(message));
}

function report(state: string, message: string): void {
  status(`${state}: ${message}`);
  send({ type: 'callState', state, message });
}

/**
 * Matches a Teams display name to a roster member by first name, so "Priya Sharma" matches "Priya S.".
 * @param name - The Teams display name.
 * @returns The member, if any.
 */
function matchMember(name: string | undefined): RoomMember | undefined {
  // Any word of the Teams name may match the start of the roster name, or the reverse, so "System Administrator" matches "Admin".
  const words = (name ?? '')
    .toLowerCase()
    .split(/\s+/)
    .filter((w) => w.length > 1);
  return members.find((m) => {
    const roster = m.displayName.toLowerCase();
    const rosterFirst = roster.split(/\s+/)[0] ?? roster;
    return words.some((w) => roster.startsWith(w) || w.startsWith(rosterFirst));
  });
}

async function fetchAcsToken(): Promise<string> {
  const response = await fetch('/acs/token', { method: 'POST' });
  if (!response.ok) throw new Error(`ACS token request failed (${response.status})`);
  return ((await response.json()) as { token: string }).token;
}

/**
 * Explains why the call ended in words a presenter can act on.
 * @param code - ACS end code.
 * @param subCode - ACS end sub code.
 * @returns The message shown in the portal.
 */
function describeEnd(code: number, subCode: number): string {
  if (subCode === 5037)
    return 'The meeting link is invalid or the meeting has ended. Check the join URL.';
  if (subCode === 5854 || subCode === 10037)
    return 'The organiser denied entry from the lobby, or the lobby timed out.';
  if (subCode === 5300) return 'The agent was removed from the meeting.';
  if (code === 0 && subCode === 0) return 'Left the meeting.';
  return `Left the meeting (${code}/${subCode}).`;
}

function watchParticipant(p: RemoteParticipant): void {
  const key = String((p.identifier as { rawId?: string }).rawId ?? p.displayName);
  if (wired.has(key)) return;
  wired.add(key);
  p.on('isSpeakingChanged', () => {
    const member = matchMember(p.displayName);
    if (member) send({ type: 'speaking', memberId: member.memberId, active: p.isSpeaking });
  });
}

async function pipeRemoteAudio(c: Call, worklet: AudioWorkletNode): Promise<void> {
  const piped = new Set<string>();
  const connect = async (stream: { getMediaStream(): Promise<MediaStream> }): Promise<void> => {
    const media = await stream.getMediaStream();
    const track = media.getAudioTracks()[0];
    const key = track?.id ?? String(piped.size);
    if (piped.has(key)) return;
    piped.add(key);
    console.log(
      `[audio] remote stream ${key} tracks=${media.getAudioTracks().length} state=${track?.readyState} muted=${track?.muted} ctx=${context.state}`,
    );
    // Chromium hands Web Audio silence for a remote WebRTC stream unless the stream is also attached to a playing media element.
    const sink = new Audio();
    sink.srcObject = media;
    sink.volume = 0.01;
    document.body.append(sink);
    await sink.play().catch((e: unknown) => console.log(`[audio] sink play failed: ${String(e)}`));
    await context.resume();
    const source = context.createMediaStreamSource(media);
    source.connect(worklet);
    const analyser = context.createAnalyser();
    analyser.fftSize = 2048;
    source.connect(analyser);
    const buf = new Float32Array(analyser.fftSize);
    let quiet = 0;
    setInterval(() => {
      analyser.getFloatTimeDomainData(buf);
      let sum = 0;
      for (const v of buf) sum += v * v;
      const rms = Math.sqrt(sum / buf.length);
      // Log only on activity (and a periodic heartbeat) so the container log stays readable.
      if (rms > 0.002 || ++quiet % 15 === 0)
        console.log(
          `[audio] remote rms=${rms.toFixed(4)} ctx=${context.state} track=${track?.readyState}/${track?.muted}`,
        );
    }, 1000);
  };
  const attach = (): void => {
    for (const s of c.remoteAudioStreams) void connect(s);
  };
  attach();
  c.on('remoteAudioStreamsUpdated', (e) => {
    for (const s of e.added) void connect(s);
  });
  c.on('stateChanged', () => {
    if (c.state === 'Connected') attach();
  });
}

function beginWhenReady(c: Call): void {
  const timer = setInterval(() => {
    if (begun || c.state !== 'Connected') return;
    const present = c.remoteParticipants.filter((p) => matchMember(p.displayName));
    if (present.length === 0) return;
    begun = true;
    clearInterval(timer);
    setTimeout(() => {
      const names = c.remoteParticipants.map((p) => matchMember(p.displayName)?.memberId);
      send({
        type: 'participants',
        items: members.map((m) => ({
          memberId: m.memberId,
          displayName: m.displayName,
          present: names.includes(m.memberId),
        })),
      });
      send({
        type: 'command',
        action: 'begin',
        memberId: present[0] && matchMember(present[0].displayName)?.memberId,
      });
      report('Live', 'Standup started in the Teams meeting.');
    }, beginDelayMs);
  }, 1000);
}

/**
 * Shares the standup board as the agent's screen. Best effort: the standup still runs if sharing is not permitted.
 * @param c - The connected call.
 */
async function shareBoard(c: Call): Promise<void> {
  if (sharing) return;
  sharing = true;
  let last = '';
  const note = (message: string): void => {
    console.log(`[share] ${message}`);
    if (message !== last) send({ type: 'callState', state: 'Info', message });
    last = message;
  };
  // Supervisor: keep trying until Teams reports the share is on, and restart it if it drops. Sharing is best effort.
  const tick = async (): Promise<void> => {
    if (c.state !== 'Connected') return;
    if (c.isScreenSharingOn) {
      note('Sharing the standup board.');
      return;
    }
    try {
      note('Starting the board share.');
      await c.startScreenSharing(new LocalVideoStream(board.stream()));
      note(
        c.isScreenSharingOn
          ? 'Sharing the standup board.'
          : 'Share requested; waiting for Teams to confirm.',
      );
    } catch (e) {
      note(`Could not share the board: ${e instanceof Error ? e.message : 'unknown error'}`);
      await c.stopScreenSharing().catch(() => undefined);
    }
  };
  await new Promise((r) => setTimeout(r, 2000));
  void tick();
  setInterval(() => void tick(), 5000);
}
async function join(): Promise<void> {
  report('Joining', 'Requesting an ACS identity.');
  const credential = new AzureCommunicationTokenCredential(await fetchAcsToken());
  const client = new CallClient();
  const agent = await client.createCallAgent(credential, { displayName });

  const virtualMic = context.createMediaStreamDestination();
  player = new AgentPlayer(
    context,
    () => send({ type: 'agentAudioState', state: 'idle' }),
    () => undefined,
    virtualMic,
  );
  localAudio = new LocalAudioStream(virtualMic.stream);
  await context.audioWorklet.addModule('/static/mic-worklet.js');
  const worklet = new AudioWorkletNode(context, 'mic-capture');
  worklet.port.onmessage = (e: MessageEvent<ArrayBuffer>) => {
    if (socket.readyState === WebSocket.OPEN) socket.send(e.data);
  };
  const silent = context.createGain();
  silent.gain.value = 0;
  worklet.connect(silent).connect(context.destination);

  call = agent.join(
    { meetingLink: joinUrl },
    { audioOptions: { muted: false, localAudioStreams: [localAudio] } },
  );
  const c = call;
  c.on('stateChanged', () => {
    switch (c.state) {
      case 'Connecting':
        return report('Joining', 'Connecting to the meeting.');
      case 'InLobby':
        return report(
          'Lobby',
          'Waiting in the Teams lobby. Admit "Zava Scrum Agent" from the meeting.',
        );
      case 'Connected':
        void shareBoard(c);
        return report('Connected', 'In the meeting. Waiting for roster members to join.');
      case 'Disconnected':
        return report(
          'Ended',
          describeEnd(c.callEndReason?.code ?? 0, c.callEndReason?.subCode ?? 0),
        );
      default:
    }
  });
  c.on('remoteParticipantsUpdated', (e) => e.added.forEach(watchParticipant));
  c.remoteParticipants.forEach(watchParticipant);
  await pipeRemoteAudio(c, worklet);
  beginWhenReady(c);
  void c.feature(Features.Capabilities);
}

function onMessage(message: ServerMessage): void {
  switch (message.type) {
    case 'hello':
      members = message.members;
      join().catch((e: unknown) =>
        report('Failed', e instanceof Error ? e.message : 'Could not join the meeting.'),
      );
      break;
    case 'board':
      board.update(message);
      break;
    case 'control':
      if (message.action === 'flush') player.flush();
      if (message.action === 'leave') void call?.hangUp();
      if (message.action === 'mute') void call?.mute();
      if (message.action === 'unmute') void call?.unmute();
      break;
    case 'error':
      status(`Failed: ${message.message}`);
      break;
    default:
  }
}

function connect(): void {
  if (!token || !apiBase || !joinUrl) {
    status('Failed: missing session token, API address or meeting link.');
    return;
  }
  context = new AudioContext({ sampleRate: SAMPLE_RATE });
  const url = `${apiBase.replace(/^http/, 'ws')}/ws/bridge/${encodeURIComponent(sessionId)}?mode=teams&token=${encodeURIComponent(token)}`;
  socket = new WebSocket(url);
  socket.binaryType = 'arraybuffer';
  socket.onopen = () => status('Connected to the API');
  socket.onclose = () => {
    status('Disconnected from the API');
    void call?.hangUp();
  };
  socket.onmessage = (e: MessageEvent<string | ArrayBuffer>) => {
    if (typeof e.data === 'string') onMessage(JSON.parse(e.data) as ServerMessage);
    else player.enqueue(new Uint8Array(e.data));
  };
}

connect();
