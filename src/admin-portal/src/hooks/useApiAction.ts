import { useCallback, useState } from 'react';

/** State of an API write. */
export interface ActionState<A extends unknown[], R> {
  run: (...args: A) => Promise<R | undefined>;
  busy: boolean;
  error: string | undefined;
}

/**
 * Wraps an API write so pages get busy and error state without try/catch in components.
 * @param action - The write.
 * @returns A runner plus busy and error state. The runner resolves `undefined` on failure.
 */
export function useApiAction<A extends unknown[], R>(
  action: (...args: A) => Promise<R>,
): ActionState<A, R> {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();

  const run = useCallback(
    async (...args: A) => {
      setBusy(true);
      setError(undefined);
      try {
        return await action(...args);
      } catch (e: unknown) {
        setError(e instanceof Error ? e.message : 'Request failed');
        return undefined;
      } finally {
        setBusy(false);
      }
    },
    [action],
  );

  return { run, busy, error };
}
