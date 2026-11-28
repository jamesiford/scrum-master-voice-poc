import { useEffect, useState } from 'react';
import { streamLiveState, type LiveState } from '../api/client';

/**
 * Subscribes to a standup's live state over server-sent events. The stream is read with `fetch`, not `EventSource`, because
 * `EventSource` cannot send the bearer token the API requires.
 * @param sessionId - The standup, or `null` to stay disconnected.
 * @returns The latest state, and whether the stream is connected.
 */
export function useLiveState(sessionId: string | null): {
  state: LiveState | undefined;
  connected: boolean;
} {
  const [state, setState] = useState<LiveState>();
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    setState(undefined);
    setConnected(false);
    if (!sessionId) return;
    const controller = new AbortController();
    void streamLiveState(sessionId, setState, setConnected, controller.signal);
    return () => controller.abort();
  }, [sessionId]);

  return { state, connected };
}
