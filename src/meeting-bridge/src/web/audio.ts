/** Audio helpers for the Simulated Room. Everything is PCM16, 24 kHz, mono (SPEC 8). */

export const SAMPLE_RATE = 24000;
export const FRAME_BYTES = 960;

/**
 * Decodes base64 PCM16.
 * @param base64 - Encoded audio.
 * @returns The raw bytes.
 */
export function base64ToBytes(base64: string): Uint8Array {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return bytes;
}

/**
 * Length of PCM16 24 kHz mono audio.
 * @param bytes - The audio.
 * @returns Milliseconds.
 */
export function durationMs(bytes: Uint8Array): number {
  return Math.round((bytes.byteLength / 2 / SAMPLE_RATE) * 1000);
}

/** Plays agent audio from the gateway and reports when its queue has drained, so the gateway knows the turn is over. */
export class AgentPlayer {
  private nextTime = 0;
  private sources = new Set<AudioBufferSourceNode>();
  private idleTimer: ReturnType<typeof setTimeout> | undefined;

  /**
   * @param context - The shared audio context (24 kHz).
   * @param onIdle - Called once playback has been silent long enough that the agent has finished.
   * @param onActive - Called when playback starts.
   */
  constructor(
    private readonly context: AudioContext,
    private readonly onIdle: () => void,
    private readonly onActive: () => void,
  ) {}

  /** Gets a value indicating whether agent audio is queued or playing. */
  get playing(): boolean {
    return this.sources.size > 0;
  }

  /**
   * Queues a chunk of agent audio.
   * @param pcm - PCM16 bytes.
   */
  enqueue(pcm: Uint8Array): void {
    if (this.sources.size === 0) this.onActive();
    clearTimeout(this.idleTimer);
    const samples = new Int16Array(
      pcm.buffer.slice(pcm.byteOffset, pcm.byteOffset + pcm.byteLength),
    );
    const buffer = this.context.createBuffer(1, samples.length, SAMPLE_RATE);
    const channel = buffer.getChannelData(0);
    for (let i = 0; i < samples.length; i++) channel[i] = (samples[i] ?? 0) / 0x8000;
    const source = this.context.createBufferSource();
    source.buffer = buffer;
    source.connect(this.context.destination);
    this.nextTime = Math.max(this.nextTime, this.context.currentTime + 0.05);
    source.start(this.nextTime);
    this.nextTime += buffer.duration;
    this.sources.add(source);
    source.onended = () => {
      this.sources.delete(source);
      if (this.sources.size === 0) this.scheduleIdle();
    };
  }

  /** Drops queued audio at once (barge-in or a cancelled response). */
  flush(): void {
    for (const source of this.sources) {
      source.onended = null;
      source.stop();
    }
    this.sources.clear();
    this.nextTime = 0;
    this.scheduleIdle();
  }

  private scheduleIdle(): void {
    clearTimeout(this.idleTimer);
    // A short grace period avoids reporting idle in the gap between two chunks of one response.
    this.idleTimer = setTimeout(() => {
      if (this.sources.size === 0) this.onIdle();
    }, 450);
  }
}

/**
 * Plays a cast line and resolves when it has finished.
 * @param context - The shared audio context.
 * @param pcm - PCM16 bytes.
 * @returns The length of the line in milliseconds.
 */
export function playCastLine(context: AudioContext, pcm: Uint8Array): Promise<number> {
  const samples = new Int16Array(pcm.buffer.slice(pcm.byteOffset, pcm.byteOffset + pcm.byteLength));
  const buffer = context.createBuffer(1, samples.length, SAMPLE_RATE);
  const channel = buffer.getChannelData(0);
  for (let i = 0; i < samples.length; i++) channel[i] = (samples[i] ?? 0) / 0x8000;
  const source = context.createBufferSource();
  source.buffer = buffer;
  source.connect(context.destination);
  return new Promise((resolve) => {
    source.onended = () => resolve(durationMs(pcm));
    source.start();
  });
}

/**
 * Sends PCM as 20 ms frames at real-time pace, followed by silence so the service ends the turn.
 * Used by autopilot to act as the presenter's microphone.
 * @param pcm - PCM16 bytes.
 * @param send - Sends one binary frame.
 * @param tailMs - Trailing silence.
 */
export async function streamAsMicrophone(
  pcm: Uint8Array,
  send: (frame: Uint8Array) => void,
  tailMs = 1600,
): Promise<void> {
  const frames = Math.ceil(pcm.byteLength / FRAME_BYTES) + Math.ceil(tailMs / 20);
  for (let i = 0; i < frames; i++) {
    const frame = new Uint8Array(FRAME_BYTES);
    frame.set(pcm.subarray(i * FRAME_BYTES, (i + 1) * FRAME_BYTES));
    send(frame);
    await new Promise((r) => setTimeout(r, 20));
  }
}

/** Captures the microphone as 20 ms PCM16 frames through the AudioWorklet. */
export class MicrophoneStreamer {
  private stream: MediaStream | undefined;

  /**
   * @param context - The shared audio context (24 kHz).
   * @param onFrame - Receives each 20 ms frame.
   */
  constructor(
    private readonly context: AudioContext,
    private readonly onFrame: (frame: ArrayBuffer) => void,
  ) {}

  /** Asks for the microphone and starts streaming frames. */
  async start(): Promise<void> {
    await this.context.audioWorklet.addModule('/static/mic-worklet.js');
    this.stream = await navigator.mediaDevices.getUserMedia({
      audio: { echoCancellation: true, noiseSuppression: true, channelCount: 1 },
    });
    const source = this.context.createMediaStreamSource(this.stream);
    const node = new AudioWorkletNode(this.context, 'mic-capture');
    node.port.onmessage = (e: MessageEvent<ArrayBuffer>) => this.onFrame(e.data);
    source.connect(node);
  }

  /** Releases the microphone. */
  stop(): void {
    this.stream?.getTracks().forEach((t) => t.stop());
  }
}
