import { useCallback, useEffect, useState } from 'react';

/** State of an API read. */
export interface QueryState<T> {
  data: T | undefined;
  error: string | undefined;
  loading: boolean;
  /** Re-runs the read. */
  reload: () => void;
}

/**
 * Runs an API read on mount and whenever `key` changes, with abort on unmount.
 * @param load - The read; receives an abort signal.
 * @param key - Re-run when this value changes (use `null` to skip).
 * @returns Data, error and loading state plus a reload function.
 */
export function useApiQuery<T>(
  load: (signal: AbortSignal) => Promise<T>,
  key: string | null,
): QueryState<T> {
  const [data, setData] = useState<T>();
  const [error, setError] = useState<string>();
  const [loading, setLoading] = useState(key !== null);
  const [tick, setTick] = useState(0);

  useEffect(() => {
    if (key === null) return;
    const controller = new AbortController();
    setLoading(true);
    load(controller.signal)
      .then((result) => {
        setData(result);
        setError(undefined);
      })
      .catch((e: unknown) => {
        if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Request failed');
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
    // `load` is intentionally excluded: callers pass inline closures and `key` identifies the request.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key, tick]);

  const reload = useCallback(() => setTick((t) => t + 1), []);
  return { data, error, loading, reload };
}
