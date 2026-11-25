import { Check, X } from 'lucide-react';
import type { ConnectionReport } from '../../api/client';

/**
 * Shows each step of a connection test and which one failed, so a broken team connection is a clear state and not a vague error.
 * @param props - The report.
 * @returns The list of steps.
 */
export function ConnectionReportView({ report }: { report: ConnectionReport }) {
  return (
    <ul className="grid gap-1.5 text-sm" aria-label="Connection test">
      {report.steps.map((s) => (
        <li key={s.name} className="flex items-start gap-2">
          {s.ok ? (
            <Check className="mt-0.5 size-4 shrink-0 text-ok" aria-label="passed" />
          ) : (
            <X className="mt-0.5 size-4 shrink-0 text-bad" aria-label="failed" />
          )}
          <span>
            <span className="font-medium">{s.name}</span>
            <span className="text-muted">: {s.detail}</span>
          </span>
        </li>
      ))}
    </ul>
  );
}
