import { AlertCircle, CheckCircle2, Info, Loader2 } from 'lucide-react';
import type { ReactNode } from 'react';
import { cx } from './cx';

/**
 * An inline message. Failures are shown plainly rather than silently (demo reliability).
 * @param props - Tone and content.
 * @returns The alert.
 */
export function Alert({
  tone = 'info',
  children,
}: {
  tone?: 'info' | 'ok' | 'warn' | 'bad';
  children: ReactNode;
}) {
  const styles = {
    info: 'bg-brand-soft text-brand',
    ok: 'bg-ok-soft text-ok',
    warn: 'bg-warn-soft text-warn',
    bad: 'bg-bad-soft text-bad',
  };
  const Icon = tone === 'ok' ? CheckCircle2 : tone === 'info' ? Info : AlertCircle;
  return (
    <div
      role={tone === 'bad' ? 'alert' : 'status'}
      className={cx('flex items-start gap-2.5 rounded-lg px-4 py-3 text-sm', styles[tone])}
    >
      <Icon className="mt-0.5 size-4 shrink-0" aria-hidden />
      <div>{children}</div>
    </div>
  );
}

/**
 * A small loading indicator.
 * @param props - Optional label.
 * @returns The spinner.
 */
export function Spinner({ label = 'Loading' }: { label?: string }) {
  return (
    <span className="inline-flex items-center gap-2 text-sm text-muted" role="status">
      <Loader2 className="size-4 animate-spin" aria-hidden />
      {label}
    </span>
  );
}

/**
 * A friendly empty state that says what to do next.
 * @param props - Title, hint and optional action.
 * @returns The empty state.
 */
export function EmptyState({
  title,
  hint,
  action,
}: {
  title: string;
  hint?: string;
  action?: ReactNode;
}) {
  return (
    <div className="grid place-items-center gap-2 rounded-xl border border-dashed border-line px-6 py-12 text-center">
      <p className="font-medium">{title}</p>
      {hint && <p className="max-w-md text-sm text-muted">{hint}</p>}
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}

/**
 * One headline number.
 * @param props - Label, value and optional detail.
 * @returns The stat.
 */
export function Stat({
  label,
  value,
  detail,
}: {
  label: string;
  value: ReactNode;
  detail?: ReactNode;
}) {
  return (
    <div className="rounded-xl border border-line bg-surface p-5 shadow-card">
      <p className="text-sm text-muted">{label}</p>
      <p className="mt-1 text-2xl font-semibold tracking-tight">{value}</p>
      {detail && <p className="mt-1 text-xs text-muted">{detail}</p>}
    </div>
  );
}

/**
 * A progress bar.
 * @param props - Current value and maximum.
 * @returns The bar.
 */
export function Progress({
  value,
  max,
  tone = 'brand',
}: {
  value: number;
  max: number;
  tone?: 'brand' | 'ok' | 'warn';
}) {
  const pct = max <= 0 ? 0 : Math.min(100, Math.max(0, (value / max) * 100));
  const fill = { brand: 'bg-brand', ok: 'bg-ok', warn: 'bg-warn' }[tone];
  return (
    <div
      className="h-2 w-full overflow-hidden rounded-full bg-subtle"
      role="progressbar"
      aria-valuenow={Math.round(pct)}
      aria-valuemin={0}
      aria-valuemax={100}
    >
      <div
        className={cx('h-full rounded-full transition-all', fill)}
        style={{ width: `${pct}%` }}
      />
    </div>
  );
}
