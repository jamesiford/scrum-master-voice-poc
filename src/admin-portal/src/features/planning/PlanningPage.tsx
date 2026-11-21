import { TEAM_ID } from '../../api/config';
import { getPlanning } from '../../api/client';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Badge } from '../../ui/Badge';
import { Card } from '../../ui/Card';
import { Alert, EmptyState, Progress } from '../../ui/Feedback';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';

/**
 * Planning page: load per member, what would carry over, items that fail the Definition of Ready, and suggested owners for
 * unassigned items. Suggestions are advice only; assigning needs an approved follow-up.
 * @returns The page.
 */
export function PlanningPage() {
  const prep = useApiQuery((signal) => getPlanning(TEAM_ID, signal), TEAM_ID);
  const p = prep.data;
  const maxPoints = Math.max(1, ...(p?.loads.map((l) => l.openPoints) ?? [1]));
  return (
    <>
      <PageHeader
        title="Planning"
        description="Prepare the next sprint: who has capacity, what is at risk of carrying over, and which items are not ready."
      />
      <div className="grid gap-6">
        <QueryStatus loading={prep.loading} error={prep.error} />
        {p && (
          <>
            <Card title="Load per person" description="Open story points and task hours">
              <div className="grid gap-4">
                {p.loads.map((l) => (
                  <div key={l.memberId} className="grid gap-1.5">
                    <div className="flex justify-between text-sm">
                      <span className="font-medium">{l.displayName}</span>
                      <span className="text-muted">
                        {l.openItems} items · {l.openPoints} pts · {l.remainingHours} h
                      </span>
                    </div>
                    <Progress value={l.openPoints} max={maxPoints} />
                  </div>
                ))}
              </div>
            </Card>
            <div className="grid gap-6 lg:grid-cols-2">
              <Card title={`Would carry over (${p.carryOver.length})`}>
                <ul className="divide-y divide-line text-sm">
                  {p.carryOver.map((c) => (
                    <li key={c.workItemId} className="flex items-center justify-between gap-3 py-2">
                      <span>
                        <span className="font-medium">{c.workItemId}</span> {c.title}
                      </span>
                      <span className="shrink-0 text-muted">{c.ownerName ?? 'unowned'}</span>
                    </li>
                  ))}
                </ul>
              </Card>
              <div className="grid content-start gap-6">
                <Card
                  title={`Not ready (${p.notReady.length})`}
                  description="Fails the Definition of Ready"
                >
                  {p.notReady.length === 0 && (
                    <p className="text-sm text-muted">Everything open is ready.</p>
                  )}
                  <ul className="grid gap-2 text-sm">
                    {p.notReady.map((n) => (
                      <li key={n.workItemId} className="flex flex-wrap items-center gap-2">
                        <span className="font-medium">{n.workItemId}</span> {n.title}
                        {n.reasons.map((r) => (
                          <Badge key={r} tone="warn">
                            {r}
                          </Badge>
                        ))}
                      </li>
                    ))}
                  </ul>
                </Card>
                <Card
                  title="Suggested owners"
                  description="Advice only. Assigning needs an approved follow-up."
                >
                  {p.suggestions.length === 0 ? (
                    <EmptyState title="Nothing is unassigned" />
                  ) : (
                    <ul className="grid gap-3 text-sm">
                      {p.suggestions.map((s) => (
                        <li key={s.workItemId}>
                          <Alert tone="info">
                            <span className="font-medium">
                              {s.workItemId} {s.title}
                            </span>{' '}
                            to {s.displayName}. {s.reason}
                          </Alert>
                        </li>
                      ))}
                    </ul>
                  )}
                </Card>
              </div>
            </div>
          </>
        )}
      </div>
    </>
  );
}
