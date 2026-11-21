import type {
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from 'react';
import { cx } from './cx';

const control =
  'w-full rounded-lg border border-line bg-surface px-3 text-sm text-ink placeholder:text-muted/70 ' +
  'transition focus:border-brand focus:outline-none focus:ring-2 focus:ring-brand/25 disabled:opacity-60';

/**
 * A labelled form control with optional hint text.
 * @param props - Label, hint and the control.
 * @returns The field.
 */
export function Field({
  label,
  hint,
  children,
  className,
}: {
  label: string;
  hint?: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <label className={cx('grid gap-1.5', className)}>
      <span className="text-sm font-medium">{label}</span>
      {children}
      {hint && <span className="text-xs text-muted">{hint}</span>}
    </label>
  );
}

/**
 * A single-line input.
 * @param props - Input props.
 * @returns The input.
 */
export function Input({ className, ...rest }: InputHTMLAttributes<HTMLInputElement>) {
  return <input {...rest} className={cx(control, 'h-10', className)} />;
}

/**
 * A multi-line input.
 * @param props - Textarea props.
 * @returns The textarea.
 */
export function Textarea({ className, ...rest }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea {...rest} className={cx(control, 'py-2', className)} />;
}

/**
 * A native select, styled to match.
 * @param props - Select props.
 * @returns The select.
 */
export function Select({ className, ...rest }: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select {...rest} className={cx(control, 'h-10', className)} />;
}

/**
 * A checkbox with a label.
 * @param props - Checked state, change handler and label.
 * @returns The checkbox.
 */
export function Check({
  checked,
  onChange,
  label,
}: {
  checked: boolean;
  onChange: (checked: boolean) => void;
  label: string;
}) {
  return (
    <label className="inline-flex cursor-pointer items-center gap-2 text-sm">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="size-4 accent-[var(--c-brand)]"
      />
      {label}
    </label>
  );
}
