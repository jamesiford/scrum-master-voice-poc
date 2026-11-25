import { CalendarPlus, Check as CheckIcon, Download, X } from 'lucide-react';
import { TEAM_ID } from '../../api/config';
import {
  decideSprintCalendar,
  getSprintCalendar,
  listSprintCalendars,
  proposeSprintCalendar,
} from '../../api/client';
import { SignedInAs } from '../../auth/SignedInAs';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Badge, type Tone } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert } from '../../ui/Feedback';
import { QueryStatus } from '../QueryStatus';

const statusTone: Record<string, Tone> = {
  Proposed: 'brand',
  Approving: 'warn',
  Approved: 'ok',
  Rejected: 'neutral',
};

function when(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/**
 * Sprint calendar (M9d): the next sprint's planning, daily standups and retrospective, proposed from the iteration dates. Approving creates
 * the Teams meetings in the agent's calendar; without Graph configured, download the .ics file instead.
 * @returns The card.
 */
export function SprintCalendarCard() {
  const plan = useApiQuery((signal) => getSprintCalendar(TEAM_ID, signal), TEAM_ID);
  const proposals = useApiQuery((signal) => listSprintCalendars(TEAM_ID, signal), TEAM_ID);
  const propose = useApiAction(() => proposeSprintCalendar(TEAM_ID));
  const decide = useApiAction((id: string, approve: boolean) => decideSprintCalendar(id, approve));
  const latest = proposals.data?.[0];
  const open = latest && (latest.status === 'Proposed' || latest.status === 'Approving');
  const name = plan.data?.iterationPath.split('\\').pop();
  const standups = plan.data?.meetings.filter((m) => m.kind === 'Standup').length ?? 0;

  const onPropose = async () => {
    await propose.run();
    proposals.reload();
  };
  const onDecide = async (approve: boolean) => {
    if (latest) await decide.run(latest.id, approve);
    proposals.reload();
  };

  return (
    <Card
      title="Proposed sprint calendar"
      description="Planning on day one, daily standups and the retrospective on the last day, from the iteration dates. Nothing is created until a named person approves."
      actions={
        <Button
          variant="primary"
          icon={<CalendarPlus className="size-4" />}
          disabled={propose.busy || !plan.data}
          onClick={() => void onPropose()}
        >
          Propose calendar
        </Button>
      }
    >
      <QueryStatus
        loading={plan.loading || proposals.loading || propose.busy || decide.busy}
        error={plan.error ?? proposals.error ?? propose.error ?? decide.error}
      />
      {plan.data && (
        <div className="mt-3 grid gap-3">
          <p className="text-sm">
            {name}: {standups} daily standups,{' '}
            {plan.data.meetings.filter((m) => m.kind !== 'Standup').length} other meetings.
          </p>
          <ul className="grid gap-1 text-sm">
            {plan.data.meetings
              .filter((m) => m.kind !== 'Standup')
              .map((m) => (
                <li key={m.key}>
                  <b>{m.kind}</b> {when(m.startsAt)}
                </li>
              ))}
          </ul>
          <a
            className="inline-flex items-center gap-1 text-sm text-brand underline"
            href={`/api/calendar/${encodeURIComponent(name ?? '')}.ics?teamId=${encodeURIComponent(TEAM_ID)}`}
          >
            <Download className="size-4" /> Download .ics (fallback)
          </a>
        </div>
      )}
      {latest && (
        <div className="mt-4 grid gap-3 border-t border-line pt-4">
          <div className="flex items-center gap-2 text-sm">
            <Badge tone={statusTone[latest.status] ?? 'neutral'} dot>
              {latest.status}
            </Badge>
            {latest.decidedBy && <span>by {latest.decidedBy}</span>}
            <span>
              {latest.meetings.filter((m) => m.eventId).length} of {latest.meetings.length} meetings
              created
            </span>
          </div>
          {latest.applyError && <Alert tone="warn">{latest.applyError}</Alert>}
          {open && (
            <div className="flex flex-wrap items-end gap-3">
              <SignedInAs />
              <Button
                variant="primary"
                icon={<CheckIcon className="size-4" />}
                disabled={decide.busy}
                onClick={() => void onDecide(true)}
              >
                Approve
              </Button>
              <Button
                icon={<X className="size-4" />}
                disabled={decide.busy || latest.status !== 'Proposed'}
                onClick={() => void onDecide(false)}
              >
                Reject
              </Button>
            </div>
          )}
        </div>
      )}
    </Card>
  );
}
