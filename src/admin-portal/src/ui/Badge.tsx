import type { ReactNode } from 'react';
import { cx } from './cx';

/** Meaning of a badge colour. */
export type Tone = 'neutral' | 'brand' | 'ok' | 'warn' | 'bad';

const tones: Record<Tone, string> = {
  neutral: 'bg-subtle text-muted',
  brand: 'bg-brand-soft text-brand',
  ok: 'bg-ok-soft text-ok',
  warn: 'bg-warn-soft text-warn',
  bad: 'bg-bad-soft text-bad',
};

/**
 * A compact status label.
 * @param props - Tone, optional leading dot, and content.
 * @returns The badge.
 */
export function Badge({
  tone = 'neutral',
  dot,
  children,
}: {
  tone?: Tone;
  dot?: boolean;
  children: ReactNode;
}) {
  return (
    <span
      className={cx(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium',
        tones[tone],
      )}
    >
      {dot && <span className="size-1.5 rounded-full bg-current" aria-hidden />}
      {children}
    </span>
  );
}
