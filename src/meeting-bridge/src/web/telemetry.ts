import type { ClientMessage } from './protocol.js';

/** Metadata-only browser hop sampling; audio still goes directly over the existing WebSocket. */
export class BridgeTelemetry {
  private lastAudioSample = 0;
  private headers: Record<string, string> = {};

  /** Binds HTTP hops to the authenticated gateway's team/session trace, not a page query parameter. */
  bind(teamId: string | undefined, traceParent: string | undefined): void {
    this.headers = teamId && traceParent ? { traceparent: traceParent, 'x-zava-team': teamId } : {};
  }

  /** Returns propagation metadata for bridge HTTP calls; no content or credentials. */
  requestHeaders(): Record<string, string> {
    return this.headers;
  }

  /** Forwards a PCM frame unchanged and reports at most one measured send hop each second. */
  forward(
    socket: WebSocket,
    frame: ArrayBuffer | Uint8Array,
    send: (message: ClientMessage) => void,
  ): void {
    const started = performance.now();
    socket.send(frame);
    if (started - this.lastAudioSample >= 1000) {
      this.lastAudioSample = started;
      send({
        type: 'telemetry',
        hop: 'bridge.audio',
        durationMs: Math.ceil(performance.now() - started),
      });
    }
  }
}
