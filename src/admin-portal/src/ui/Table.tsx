import type { ReactNode } from 'react';

/**
 * A responsive data table.
 * @param props - Column headings and rows.
 * @returns The table.
 */
export function Table({
  headings,
  children,
  label,
}: {
  headings: string[];
  children: ReactNode;
  label: string;
}) {
  return (
    <div className="overflow-x-auto rounded-xl border border-line bg-surface shadow-card">
      <table className="w-full text-sm" aria-label={label}>
        <thead>
          <tr className="border-b border-line bg-subtle/60 text-left text-xs uppercase tracking-wide text-muted">
            {headings.map((h) => (
              <th key={h} className="px-4 py-3 font-medium">
                {h}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-line">{children}</tbody>
      </table>
    </div>
  );
}
