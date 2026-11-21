import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { cx } from './cx';

/** Visual weight of a button. */
export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';

const variants: Record<ButtonVariant, string> = {
  primary: 'bg-brand text-brand-ink hover:opacity-90 shadow-card',
  secondary: 'bg-surface text-ink border border-line hover:bg-subtle',
  ghost: 'text-muted hover:bg-subtle hover:text-ink',
  danger: 'bg-bad-soft text-bad hover:opacity-80',
};

/** Props for {@link Button}. */
export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: 'sm' | 'md';
  icon?: ReactNode;
}

/**
 * A button with a clear visual hierarchy: one primary action per area, secondary for the rest.
 * @param props - Button props.
 * @returns The button.
 */
export function Button({
  variant = 'secondary',
  size = 'md',
  icon,
  className,
  children,
  ...rest
}: ButtonProps) {
  return (
    <button
      type="button"
      {...rest}
      className={cx(
        'inline-flex items-center justify-center gap-2 rounded-lg font-medium transition',
        'disabled:cursor-not-allowed disabled:opacity-50',
        size === 'sm' ? 'h-8 px-3 text-sm' : 'h-10 px-4 text-sm',
        variants[variant],
        className,
      )}
    >
      {icon}
      {children}
    </button>
  );
}
