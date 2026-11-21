import { Send } from 'lucide-react';
import { useState } from 'react';
import { TEAM_ID } from '../../api/config';
import { askAgent, getKpis, getReport } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Alert, Progress, Stat } from '../../ui/Feedback';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Input } from '../../ui/Form';
import { PageHeader } from '../../ui/PageHeader';
import { cx } from '../../ui/cx';
import { QueryStatus } from '../QueryStatus';

const pct = (n: number) => `${Math.round(n * 100)}%`;

/**
 * Reports page: team KPIs, the sprint trend, a written sprint or monthly narrative, and a box to ask the Reporting agent a question.
 * Numbers come from code; the model only phrases the narrative. Fixture-backed data is labelled "illustrative" (SPEC 9.5).
 * @returns The page.
 */
export function ReportsPage() {
  const [kind, setKind] = useState<'sprint' | 'monthly'>('sprint');
  const [question, setQuestion] = useState('');
  const [answer, setAnswer] = useState('');
  const kpis = useApiQuery((signal) => getKpis(TEAM_ID, 6, signal), TEAM_ID);
  const report = useApiQuery((signal) => getReport(kind, TEAM_ID, 6, signal), kind);
  const ask = useApiAction(() => askAgent(TEAM_ID, question));
  const k = kpis.data?.summary;

  const onAsk = async () => {
    const text = await ask.run();
    if (text) setAnswer(text);
  };

  return (
    <>
      <PageHeader
        title="Reports"
        description="How the team is delivering, written up for a delivery manager. Every figure is calculated in code."
        actions={kpis.data?.illustrative ? <Badge tone="warn">Illustrative data</Badge> : undefined}
      />
      <div className="grid gap-6">
        <QueryStatus
          loading={kpis.loading || report.loading}
          error={kpis.error ?? report.error ?? ask.error}
        />
        {k && (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Stat
              label="Say/do ratio"
              value={pct(k.sayDoRatio)}
              detail={`Last ${k.sprints} sprints`}
            />
            <Stat
              label="Velocity"
              value={`${Math.round(k.velocity * 10) / 10}`}
              detail="Points per sprint"
            />
            <Stat label="Carry-over" value={k.carryOverItems} detail="Items across the window" />
            <Stat
              label="Follow-ups closed in 24h"
              value={`${Math.round(k.avgFollowUpClosurePct)}%`}
              detail={`${k.agentRunStandups} agent-run standups`}
            />
          </div>
        )}
        <div className="grid gap-6 lg:grid-cols-5">
          <Card title="Sprint trend" className="lg:col-span-2">
            <div className="grid gap-4">
              {kpis.data?.sprints.map((s) => (
                <div key={s.name} className="grid gap-1.5">
                  <div className="flex justify-between text-sm">
                    <span className="font-medium">{s.name}</span>
                    <span className="text-muted">
                      {s.completedPoints} / {s.committedPoints} pts · {s.carryOverItems} carried
                    </span>
                  </div>
                  <Progress
                    value={s.completedPoints}
                    max={Math.max(s.committedPoints, 1)}
                    tone={s.completedPoints / Math.max(s.committedPoints, 1) < 0.85 ? 'warn' : 'ok'}
                  />
                </div>
              ))}
            </div>
          </Card>
          <Card
            title="Written report"
            className="lg:col-span-3"
            actions={
              <div
                className="inline-flex rounded-lg border border-line p-0.5 text-sm"
                role="tablist"
              >
                {(['sprint', 'monthly'] as const).map((id) => (
                  <button
                    key={id}
                    role="tab"
                    aria-selected={kind === id}
                    onClick={() => setKind(id)}
                    className={cx(
                      'rounded-md px-3 py-1 font-medium capitalize transition',
                      kind === id ? 'bg-brand-soft text-brand' : 'text-muted hover:text-ink',
                    )}
                  >
                    {id}
                  </button>
                ))}
              </div>
            }
          >
            {report.data && (
              <div className="grid gap-3">
                {!report.data.narrativeFromModel && (
                  <Alert tone="warn">
                    The language model was unavailable, so a plain summary is shown.
                  </Alert>
                )}
                <p className="text-sm leading-relaxed">{report.data.narrative}</p>
              </div>
            )}
          </Card>
        </div>
        <Card title="Ask the agent" description="The same answers are available in Teams chat.">
          <form
            className="flex gap-2"
            onSubmit={(e) => {
              e.preventDefault();
              if (question.trim()) void onAsk();
            }}
          >
            <Input
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              placeholder="For example: how is standup attendance?"
              aria-label="Question"
            />
            <Button
              variant="primary"
              icon={<Send className="size-4" />}
              disabled={!question.trim() || ask.busy}
              type="submit"
            >
              Ask
            </Button>
          </form>
          {answer && (
            <p className="mt-4 whitespace-pre-wrap rounded-lg bg-subtle p-4 text-sm">{answer}</p>
          )}
        </Card>
      </div>
    </>
  );
}
