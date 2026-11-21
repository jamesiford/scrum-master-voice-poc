import { useEffect, useState } from 'react';
import type { LiveState } from '../api/client';

/**
 * Subscribes to a standup's live state over server-sent events.
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
    if (!sessionId) return;
    const source = new EventSource(`/api/sessions/${encodeURIComponent(sessionId)}/live`);
    source.onopen = () => setConnected(true);
    source.onmessage = (e: MessageEvent<string>) => setState(JSON.parse(e.data) as LiveState);
    source.onerror = () => {
      setConnected(false);
      source.close();
    };
    return () => source.close();
  }, [sessionId]);

  return { state, connected };
}
