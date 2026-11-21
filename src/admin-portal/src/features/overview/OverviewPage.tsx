import { ArrowRight, CheckCircle2, Circle } from 'lucide-react';
import type { ReactNode } from 'react';
import { TEAM_ID } from '../../api/config';
import { getDemoStatus, listFollowUps, listSessions } from '../../api/client';
import { useApiQuery } from '../../hooks/useApiQuery';
import type { PageId } from '../../hooks/useRoute';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Stat } from '../../ui/Feedback';
import { PageHeader } from '../../ui/PageHeader';

interface Step {
  title: string;
  body: string;
  done: boolean;
  to: PageId;
  action: string;
  extra?: ReactNode;
}

/**
 * Overview: the four steps of a standup in order, each showing where you are and one button to go next.
 * @param props - Navigation callback.
 * @returns The page.
 */
export function OverviewPage({ go }: { go: (page: PageId) => void }) {
  const sessions = useApiQuery((s) => listSessions(TEAM_ID, s), TEAM_ID);
  const followUps = useApiQuery((s) => listFollowUps(TEAM_ID, s), TEAM_ID);
  const status = useApiQuery((s) => getDemoStatus(TEAM_ID, s), TEAM_ID);

  const latest = sessions.data?.[0];
  const pending =
    followUps.data?.filter((f) => f.status === 'Sent' || f.status === 'Pending').length ?? 0;
  const applied = followUps.data?.filter((f) => f.status === 'Applied').length ?? 0;
  const ended = latest?.status === 'Ended';

  const steps: Step[] = [
    {
      title: 'Prepare',
      body: 'Build the pre-brief from Azure DevOps: burndown, flags and one grounded question per person.',
      done: !!latest,
      to: 'sessions',
      action: 'Open sessions',
    },
    {
      title: 'Run the standup',
      body: 'Start it in the Simulated Room or join a Teams meeting, then watch and steer from the live console.',
      done: ended || latest?.status === 'Live',
      to: 'live',
      action: 'Open live console',
    },
    {
      title: 'Approve follow-ups',
      body: 'Nothing is written to Azure DevOps until a named person approves. Review each proposed change.',
      done: pending === 0 && (followUps.data?.length ?? 0) > 0,
      to: 'followups',
      action: pending > 0 ? `Review ${pending} pending` : 'Open follow-ups',
      extra: pending > 0 ? <Badge tone="warn">{pending} waiting</Badge> : undefined,
    },
    {
      title: 'Review and plan',
      body: 'Sprint and monthly reports, team KPIs, and planning prep with suggested owners.',
      done: false,
      to: 'reports',
      action: 'Open reports',
    },
  ];

  return (
    <>
      <PageHeader
        title="Good to see you"
        description="Run the daily standup, approve what the agent proposes, then learn from the numbers."
        actions={
          <Button
            variant="primary"
            icon={<ArrowRight className="size-4" />}
            onClick={() => go('sessions')}
          >
            Prepare a standup
          </Button>
        }
      />
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Stat
          label="Sessions"
          value={status.data?.sessions ?? '–'}
          detail={latest ? `Latest: ${latest.status}` : 'None yet'}
        />
        <Stat label="Awaiting approval" value={pending} detail="Follow-ups not yet decided" />
        <Stat
          label="Written to Azure DevOps"
          value={applied}
          detail="Approved follow-ups applied"
        />
        <Stat label="Cards sent" value={status.data?.cards ?? '–'} detail="Teams and outbox" />
      </div>
      <h2 className="mb-3 mt-10 text-sm font-semibold uppercase tracking-wide text-muted">
        Your standup, step by step
      </h2>
      <div className="grid gap-4 md:grid-cols-2">
        {steps.map((s, i) => (
          <Card key={s.title}>
            <div className="flex items-start gap-4">
              {s.done ? (
                <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-ok" aria-label="Done" />
              ) : (
                <Circle className="mt-0.5 size-5 shrink-0 text-muted" aria-label="To do" />
              )}
              <div className="grid flex-1 gap-2">
                <div className="flex items-center gap-2">
                  <h3 className="font-semibold">
                    {i + 1}. {s.title}
                  </h3>
                  {s.extra}
                </div>
                <p className="text-sm text-muted">{s.body}</p>
                <div>
                  <Button size="sm" onClick={() => go(s.to)}>
                    {s.action}
                  </Button>
                </div>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </>
  );
}
