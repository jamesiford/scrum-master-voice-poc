import { useEffect, useState } from 'react';
import { getHealth, type Health } from '../api/client';

/** Result of polling API health. */
export type HealthState =
  | { kind: 'loading' }
  | { kind: 'healthy'; health: Health }
  | { kind: 'unhealthy'; message: string };

/**
 * Polls <c>/api/health</c> so the shell can show "API healthy" or a plain failure message.
 * @param intervalMs - Poll interval in milliseconds.
 * @returns The latest health state.
 */
export function useHealth(intervalMs = 5000): HealthState {
  const [state, setState] = useState<HealthState>({ kind: 'loading' });

  useEffect(() => {
    const controller = new AbortController();
    const poll = (): void => {
      getHealth(controller.signal)
        .then((health) => setState({ kind: 'healthy', health }))
        .catch((error: unknown) => {
          if (controller.signal.aborted) return;
          const message = error instanceof Error ? error.message : 'API unreachable';
          setState({ kind: 'unhealthy', message });
        });
    };
    poll();
    const timer = setInterval(poll, intervalMs);
    return () => {
      controller.abort();
      clearInterval(timer);
    };
  }, [intervalMs]);

  return state;
}
