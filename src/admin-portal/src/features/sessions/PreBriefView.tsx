import type { BriefItem, Flag, PreBrief } from '../../api/client';
import { Badge, type Tone } from '../../ui/Badge';
import { Card } from '../../ui/Card';
import { Progress } from '../../ui/Feedback';

const flagTone: Record<Flag, Tone> = {
  Blocked: 'bad',
  Overdue: 'bad',
  Stale: 'warn',
  NoRecentUpdate: 'neutral',
  Unassigned: 'brand',
};
const flagLabel: Record<Flag, string> = {
  Blocked: 'Blocked',
  Overdue: 'Overdue',
  Stale: 'Stale',
  NoRecentUpdate: 'No recent update',
  Unassigned: 'Unassigned',
};

function ItemLine({ item }: { item: BriefItem }) {
  return (
    <li className="flex flex-wrap items-center gap-x-3 gap-y-1 py-2 text-sm">
      <span className="font-medium">
        {item.type} {item.id.value}
      </span>
      <span className="text-muted">
        {item.title} · {item.state}, {item.daysInState}d
      </span>
      {item.flags.map((f) => (
        <Badge key={f} tone={flagTone[f]}>
          {flagLabel[f]}
        </Badge>
      ))}
    </li>
  );
}

/**
 * Renders a pre-brief: sprint position, team risks, then each member's items and suggested question.
 * @param props - The pre-brief to show.
 * @returns The pre-brief view.
 */
export function PreBriefView({ brief }: { brief: PreBrief }) {
  const { burndown } = brief;
  return (
    <Card title="Pre-brief" description={brief.iterationPath}>
      <div className="grid gap-6">
        <div className="grid gap-2">
          <p className="text-sm">
            <span className="font-medium">Goal:</span> {brief.sprintGoal || 'not set'}
          </p>
          <div className="flex items-center gap-3">
            <div className="flex-1">
              <Progress
                value={burndown.completedPoints}
                max={burndown.committedPoints}
                tone={burndown.atRisk ? 'warn' : 'ok'}
              />
            </div>
            <Badge tone={burndown.atRisk ? 'bad' : 'ok'} dot>
              {burndown.atRisk ? 'Burndown at risk' : 'On pace'}
            </Badge>
          </div>
          <p className="text-sm text-muted">
            {burndown.completedPoints} of {burndown.committedPoints} points done,{' '}
            {burndown.remainingPoints} left, {burndown.daysLeft} days to go.
          </p>
          <p className="text-sm">{brief.summary}</p>
          {brief.teamRisks.length > 0 && (
            <ul aria-label="Team risks" className="list-disc pl-5 text-sm text-muted">
              {brief.teamRisks.map((r) => (
                <li key={r}>{r}</li>
              ))}
            </ul>
          )}
        </div>
        <div className="grid gap-4 lg:grid-cols-2">
          {brief.members.map((m) => (
            <div key={m.memberId} className="rounded-lg border border-line p-4">
              <h3 className="font-semibold">{m.displayName}</h3>
              <p className="mt-1 text-sm italic text-muted">“{m.suggestedQuestion}”</p>
              {m.items.length === 0 ? (
                <p className="mt-3 text-sm text-muted">No open items.</p>
              ) : (
                <ul className="mt-2 divide-y divide-line">
                  {m.items.map((i) => (
                    <ItemLine key={i.id.value} item={i} />
                  ))}
                </ul>
              )}
            </div>
          ))}
          {brief.unassignedItems.length > 0 && (
            <div className="rounded-lg border border-dashed border-line p-4">
              <h3 className="font-semibold">Unassigned</h3>
              <ul className="mt-2 divide-y divide-line">
                {brief.unassignedItems.map((i) => (
                  <ItemLine key={i.id.value} item={i} />
                ))}
              </ul>
            </div>
          )}
        </div>
      </div>
    </Card>
  );
}
