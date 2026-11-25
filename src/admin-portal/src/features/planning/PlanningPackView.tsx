import { Check as CheckIcon, X } from 'lucide-react';
import { useState } from 'react';
import {
  approvePlan,
  getFollowUp,
  rejectPlan,
  type PlanningPack,
  type Team,
} from '../../api/client';
import { SignedInAs } from '../../auth/SignedInAs';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert, Progress } from '../../ui/Feedback';
import { Check } from '../../ui/Form';
import { QueryStatus } from '../QueryStatus';

interface PlanningPackViewProps {
  pack: PlanningPack;
  team: Team | undefined;
  onChanged: () => void;
}

/**
 * A prepared sprint plan: draft goal, capacity bars, the proposed selection with a checkbox per item, what was left out and why, and
 * Approve plan or Reject. Approving is what permits the Azure DevOps write (F7), so it needs a name.
 * @param props - The pack, the team (for links) and a refresh callback.
 * @returns The view.
 */
export function PlanningPackView({ pack, team, onChanged }: PlanningPackViewProps) {
  const p = pack.proposal;
  const [kept, setKept] = useState<Set<number>>(new Set(p.selected.map((s) => s.workItemId)));
  const approve = useApiAction((ids: number[]) => approvePlan(pack.id, ids));
  const reject = useApiAction(() => rejectPlan(pack.id));
  const decided = pack.status !== 'Prepared';
  const canDecide = !decided;
  const keptPoints = p.selected
    .filter((s) => kept.has(s.workItemId))
    .reduce((n, s) => n + s.points, 0);
  const maxCapacity = Math.max(
    1,
    ...p.members.map((m) => Math.max(m.capacityHigh, m.plannedPoints)),
  );

  const toggle = (id: number, on: boolean) =>
    setKept((prev) => {
      const next = new Set(prev);
      if (on) next.add(id);
      else next.delete(id);
      return next;
    });
  const decide = async (run: () => Promise<unknown>) => {
    await run();
    onChanged();
  };

  return (
    <div className="grid gap-6">
      <Card
        title={`Plan for ${pack.nextIterationPath ?? 'the next sprint'}`}
        description={`Starts from ${pack.iterationPath}. Velocity ${p.velocity.low} to ${p.velocity.high} points${pack.velocityIllustrative ? ' (illustrative history)' : ''}.`}
        actions={<StatusBadge pack={pack} />}
      >
        <div className="grid gap-3">
          <p className="text-sm">
            <span className="font-medium">Draft sprint goal: </span>
            {pack.draftGoal}{' '}
            <Badge tone={pack.goalFromModel ? 'brand' : 'neutral'}>
              {pack.goalFromModel ? 'phrased by the model' : 'built in code'}
            </Badge>
          </p>
          <p className="text-sm">
            <span className="font-medium">Planned: </span>
            {p.totalPoints} points
            <Badge tone={p.commitment === 'Within' ? 'ok' : 'warn'} dot>
              {p.commitment === 'Within'
                ? 'within the velocity range'
                : p.commitment === 'Over'
                  ? 'over-committed'
                  : 'under-committed'}
            </Badge>
          </p>
          {p.warnings.map((w) => (
            <Alert key={w} tone="warn">
              {w}
            </Alert>
          ))}
          {!pack.nextIterationPath && (
            <Alert tone="bad">
              Azure DevOps has no future iteration for this team, so approving is blocked. Add one
              and prepare the pack again.
            </Alert>
          )}
        </div>
      </Card>

      <Card
        title="Capacity"
        description="Each member's equal share of the velocity range, times their availability. The marker shows the low end."
      >
        <div className="grid gap-4">
          {p.members.map((m) => (
            <div key={m.memberId} className="grid gap-1.5">
              <div className="flex justify-between text-sm">
                <span className="font-medium">
                  {m.displayName}{' '}
                  <span className="text-muted">({m.availabilityPct}% available)</span>
                </span>
                <span className={m.overloaded ? 'text-warn' : 'text-muted'}>
                  {m.plannedPoints} planned of {m.capacityLow} to {m.capacityHigh} pts
                </span>
              </div>
              <Progress
                value={m.plannedPoints}
                max={maxCapacity}
                tone={m.overloaded ? 'warn' : 'brand'}
              />
            </div>
          ))}
        </div>
      </Card>

      <Card
        title={`Proposed selection (${kept.size} of ${p.selected.length} kept, ${keptPoints} pts)`}
        description="Carry-over first, then ready backlog items in backlog order. Untick an item to leave it out of the approval."
      >
        <ul className="divide-y divide-line text-sm">
          {p.selected.map((s) => (
            <li
              key={s.workItemId}
              className="flex flex-wrap items-center justify-between gap-3 py-2"
            >
              <Check
                checked={kept.has(s.workItemId)}
                onChange={(on) => toggle(s.workItemId, on)}
                label={`${s.workItemId} ${s.title}`}
              />
              <span className="flex flex-wrap items-center gap-2 text-muted">
                <Badge tone={s.source === 'CarryOver' ? 'warn' : 'neutral'}>
                  {s.source === 'CarryOver' ? 'carry-over' : 'backlog'}
                </Badge>
                {s.points} pts ·{' '}
                {s.ownerName
                  ? s.ownerSuggested
                    ? `${s.ownerName} (suggested)`
                    : s.ownerName
                  : 'no owner'}
              </span>
            </li>
          ))}
        </ul>
      </Card>

      <Card
        title={`Not planned (${p.excluded.length})`}
        description="Left out of the selection, with the reason."
      >
        <ul className="grid gap-2 text-sm">
          {p.excluded.map((x) => (
            <li key={x.workItemId} className="flex flex-wrap items-center gap-2">
              <span className="font-medium">{x.workItemId}</span> {x.title}
              {x.points !== null && <span className="text-muted">{x.points} pts</span>}
              {x.reasons.map((r) => (
                <Badge key={r} tone="warn">
                  {r}
                </Badge>
              ))}
            </li>
          ))}
        </ul>
      </Card>

      {!decided && (
        <Card>
          <div className="flex flex-wrap items-end justify-between gap-4">
            <SignedInAs />
            <div className="flex flex-wrap gap-2">
              <Button
                variant="primary"
                icon={<CheckIcon className="size-4" />}
                disabled={!canDecide || kept.size === 0 || !pack.nextIterationPath || approve.busy}
                onClick={() => void decide(() => approve.run([...kept]))}
              >
                Approve plan
              </Button>
              <Button
                icon={<X className="size-4" />}
                disabled={!canDecide || reject.busy}
                onClick={() => void decide(() => reject.run())}
              >
                Reject
              </Button>
            </div>
          </div>
          <QueryStatus loading={false} error={approve.error ?? reject.error} />
        </Card>
      )}

      {decided && <Outcome pack={pack} team={team} />}
    </div>
  );
}

function StatusBadge({ pack }: { pack: PlanningPack }) {
  const tone = pack.status === 'Approved' ? 'ok' : pack.status === 'Rejected' ? 'bad' : 'brand';
  return (
    <Badge tone={tone} dot>
      {pack.status}
    </Badge>
  );
}

/** The result of a decided plan: who decided, whether the write succeeded, and links to the planned items in Azure DevOps. */
function Outcome({ pack, team }: { pack: PlanningPack; team: Team | undefined }) {
  const followUp = useApiQuery(
    (signal) =>
      pack.followUpId ? getFollowUp(pack.followUpId, signal) : Promise.reject(new Error('none')),
    pack.followUpId,
  );
  if (pack.status === 'Rejected') {
    return (
      <Alert tone="info">Rejected by {pack.decidedBy}. Nothing was written to Azure DevOps.</Alert>
    );
  }
  const f = followUp.data;
  const link = (id: number) =>
    team
      ? `${team.ado.org.replace(/\/$/, '')}/${encodeURIComponent(team.ado.project)}/_workitems/edit/${id}`
      : '#';
  return (
    <Card
      title={f?.status === 'Applied' ? 'Plan written to Azure DevOps' : 'Plan approved'}
      description={`Approved by ${pack.decidedBy}.`}
    >
      <div className="grid gap-3">
        <QueryStatus loading={followUp.loading} error={undefined} />
        {f?.applyError && <Alert tone="bad">Azure DevOps write failed: {f.applyError}</Alert>}
        {f?.status === 'Approved' && !f.applyError && (
          <Alert tone="info">Writing to Azure DevOps.</Alert>
        )}
        <ul className="grid gap-1 text-sm">
          {pack.approvedItemIds.map((id) => (
            <li key={id}>
              <a className="text-brand underline" href={link(id)} target="_blank" rel="noreferrer">
                Work item {id}
              </a>{' '}
              <span className="text-muted">
                {pack.proposal.selected.find((s) => s.workItemId === id)?.title}
              </span>
            </li>
          ))}
        </ul>
      </div>
    </Card>
  );
}
