import { RefreshCw, Sparkles } from 'lucide-react';
import { TEAM_ID } from '../../api/config';
import {
  getPlanning,
  getPlanningPack,
  getRoster,
  getTeam,
  preparePlanning,
  updateAvailability,
} from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert, EmptyState, Progress } from '../../ui/Feedback';
import { Input } from '../../ui/Form';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';
import { PlanningPackView } from './PlanningPackView';

/**
 * Planning page. The pack section prepares the next sprint (capacity, selection, draft goal) and lets a named person approve it; below it,
 * the advice view shows load per member, what would carry over, items that fail the Definition of Ready, and suggested owners.
 * @returns The page.
 */
export function PlanningPage() {
  const prep = useApiQuery((signal) => getPlanning(TEAM_ID, signal), TEAM_ID);
  const pack = useApiQuery((signal) => getPlanningPack(TEAM_ID, signal), 'pack');
  const roster = useApiQuery((signal) => getRoster(TEAM_ID, signal), TEAM_ID);
  const team = useApiQuery((signal) => getTeam(TEAM_ID, signal), TEAM_ID);
  const prepare = useApiAction(() => preparePlanning(TEAM_ID));
  const setAvailability = useApiAction((memberId: string, percent: number) =>
    updateAvailability(TEAM_ID, memberId, percent),
  );
  const p = prep.data;
  const maxPoints = Math.max(1, ...(p?.loads.map((l) => l.openPoints) ?? [1]));

  const onPrepare = async () => {
    await prepare.run();
    pack.reload();
  };

  return (
    <>
      <PageHeader
        title="Planning"
        description="Prepare the next sprint: capacity from velocity, carry-over first, then ready backlog items. Nothing is written until a named person approves."
        actions={
          <>
            <Button
              variant="primary"
              icon={<Sparkles className="size-4" />}
              disabled={prepare.busy}
              onClick={() => void onPrepare()}
            >
              Prepare pack
            </Button>
            <Button
              variant="ghost"
              icon={<RefreshCw className="size-4" />}
              aria-label="Refresh"
              onClick={() => {
                pack.reload();
                prep.reload();
              }}
            />
          </>
        }
      />
      <div className="grid gap-6">
        <QueryStatus
          loading={prepare.busy || pack.loading}
          error={prepare.error ?? pack.error ?? setAvailability.error}
        />
        <Card
          title="Availability"
          description="Share of a normal sprint each person is available for (holiday, part-time). Capacity is scaled by it the next time you prepare the pack."
        >
          <div className="flex flex-wrap gap-6">
            {roster.data?.map((m) => (
              <label key={m.id} className="grid gap-1 text-sm">
                <span className="font-medium">{m.displayName}</span>
                <span className="flex items-center gap-2">
                  <Input
                    type="number"
                    min={0}
                    max={100}
                    defaultValue={m.availabilityPct}
                    className="w-24"
                    aria-label={`${m.displayName} availability percent`}
                    onBlur={(e) => {
                      const value = Number(e.target.value);
                      if (Number.isFinite(value) && value !== m.availabilityPct) {
                        void setAvailability.run(m.id, value).then(roster.reload);
                      }
                    }}
                  />
                  %
                </span>
              </label>
            ))}
          </div>
        </Card>
        {pack.data ? (
          <PlanningPackView
            key={`${pack.data.id}-${pack.data.status}`}
            pack={pack.data}
            team={team.data}
            onChanged={pack.reload}
          />
        ) : (
          !pack.loading && (
            <EmptyState
              title="No planning pack yet"
              hint="Press Prepare pack to build the next sprint from the backlog."
            />
          )
        )}

        <h2 className="mt-4 text-sm font-semibold uppercase tracking-wide text-muted">
          Advice for the current sprint
        </h2>
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
                  description="Advice only. Assigning needs an approved plan or follow-up."
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
