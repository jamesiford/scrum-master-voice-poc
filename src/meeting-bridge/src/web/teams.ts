import {
  Features,
  LocalAudioStream,
  CallClient,
  type Call,
  type RemoteParticipant,
} from '@azure/communication-calling';
import { AzureCommunicationTokenCredential } from '@azure/communication-common';
import { AgentPlayer, SAMPLE_RATE } from './audio.js';
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
const displayName = params.get('name') ?? 'MCM Scrum Agent';
const beginDelayMs = Number(params.get('beginDelayMs') ?? 8000);

let socket: WebSocket;
let call: Call | undefined;
let members: RoomMember[] = [];
let begun = false;
let player: AgentPlayer;
let context: AudioContext;
// Outgoing audio uses its own 48 kHz context: WebRTC sends 48 kHz, and a 24 kHz track can be dropped silently.
let outContext: AudioContext;
let meter: AnalyserNode;
let localAudio: LocalAudioStream;
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
 * Matches a Teams display name to a roster member by first name, so "Daniel Park" matches "Daniel Park".
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
  const connect = async (stream: { getMediaStream(): Promise<MediaStream> }): Promise<void> => {
    const media = await stream.getMediaStream();
    const source = context.createMediaStreamSource(media);
    source.connect(worklet);
  };
  for (const s of c.remoteAudioStreams) await connect(s);
  c.on('remoteAudioStreamsUpdated', (e) => {
    for (const s of e.added) void connect(s);
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
 * Makes sure the agent's virtual microphone is attached to the call and unmuted, then plays a short tone so a presenter can
 * hear straight away that audio works. Headless Chromium can leave the audio context suspended and ACS can join muted.
 * @param c - The call.
 * @param virtualMic - The destination the agent's audio is routed into.
 */
async function ensureOutgoingAudio(
  c: Call,
  virtualMic: MediaStreamAudioDestinationNode,
): Promise<void> {
  try {
    if (context.state !== 'running') await context.resume();
    if (outContext.state !== 'running') await outContext.resume();
    // Re-attach after the call connects; attaching only at join time is not always honoured by the SDK.
    if (c.localAudioStreams.length > 0) await c.stopAudio();
    await c.startAudio(localAudio);
    if (c.isMuted) await c.unmute();
    const track = virtualMic.stream.getAudioTracks()[0];
    send({
      type: 'callState',
      state: 'Audio',
      message: `Outgoing audio ready (out ${outContext.state}  Hz, track ${track?.readyState ?? 'none'}, muted ${c.isMuted}).`,
    });
    const osc = outContext.createOscillator();
    const gain = outContext.createGain();
    gain.gain.value = 0.15;
    osc.frequency.value = 660;
    osc.connect(gain).connect(virtualMic);
    osc.start();
    osc.stop(outContext.currentTime + 0.4);
  } catch (e) {
    send({
      type: 'callState',
      state: 'Audio',
      message: `Outgoing audio problem: ${e instanceof Error ? e.message : 'unknown'}`,
    });
  }
}

/**
 * Reports the loudest outgoing sample about once a second while the agent speaks, so the portal shows whether audio leaves
 * this page. A peak above zero here with silence in the meeting means the problem is after the page (the SDK or Teams).
 */
function watchLevel(): void {
  const buffer = new Float32Array(meter.fftSize);
  let peak = 0;
  let last = 0;
  setInterval(() => {
    meter.getFloatTimeDomainData(buffer);
    for (const s of buffer) peak = Math.max(peak, Math.abs(s));
    if (peak > 0.01 && Date.now() - last > 4000) {
      send({
        type: 'callState',
        state: 'Audio',
        message: `Agent audio leaving the page (peak ${peak.toFixed(2)}).`,
      });
      last = Date.now();
    }
    peak = 0;
  }, 250);
}

async function join(): Promise<void> {
  report('Joining', 'Requesting an ACS identity.');
  const credential = new AzureCommunicationTokenCredential(await fetchAcsToken());
  const client = new CallClient();
  const agent = await client.createCallAgent(credential, { displayName });

  outContext = new AudioContext({ sampleRate: 48000 });
  const virtualMic = outContext.createMediaStreamDestination();
  const outGain = outContext.createGain();
  meter = outContext.createAnalyser();
  outGain.connect(virtualMic);
  outGain.connect(meter);
  watchLevel();
  player = new AgentPlayer(
    outContext,
    () => send({ type: 'agentAudioState', state: 'idle' }),
    () => undefined,
    outGain,
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
          'Waiting in the Teams lobby. Admit "MCM Scrum Agent" from the meeting.',
        );
      case 'Connected':
        void ensureOutgoingAudio(c, virtualMic);
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
