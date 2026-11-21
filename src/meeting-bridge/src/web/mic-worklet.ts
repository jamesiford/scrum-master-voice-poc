/** AudioWorklet that turns microphone input into PCM16 frames of 20 ms at 24 kHz (SPEC 8). Do not resample elsewhere. */

declare abstract class AudioWorkletProcessor {
  readonly port: MessagePort;
  abstract process(inputs: Float32Array[][], outputs: Float32Array[][]): boolean;
}
declare function registerProcessor(name: string, processor: new () => AudioWorkletProcessor): void;

const FRAME_SAMPLES = 480;

class MicCapture extends AudioWorkletProcessor {
  private frame = new Int16Array(FRAME_SAMPLES);
  private filled = 0;

  override process(inputs: Float32Array[][]): boolean {
    const channel = inputs[0]?.[0];
    if (!channel) return true;
    for (const sample of channel) {
      this.frame[this.filled++] = Math.max(-1, Math.min(1, sample)) * 0x7fff;
      if (this.filled === FRAME_SAMPLES) {
        const out = this.frame.slice();
        this.port.postMessage(out.buffer, [out.buffer]);
        this.filled = 0;
      }
    }
    return true;
  }
}

registerProcessor('mic-capture', MicCapture);
