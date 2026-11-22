import { Play, Plus, RefreshCw } from 'lucide-react';
import { useState } from 'react';
import { TEAM_ID } from '../../api/config';
import { getSession, listSessions, prepareStandup, startSession } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Badge, type Tone } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { EmptyState } from '../../ui/Feedback';
import { Field, Input } from '../../ui/Form';
import { PageHeader } from '../../ui/PageHeader';
import { Table } from '../../ui/Table';
import { QueryStatus } from '../QueryStatus';
import { PreBriefView } from './PreBriefView';
import { SessionTimeline } from './SessionTimeline';

const statusTone: Record<string, Tone> = { Prepared: 'brand', Live: 'ok', Ended: 'neutral' };
const ceremonyLabel: Record<string, string> = {
  DailyScrum: 'Daily scrum',
  Retrospective: 'Retrospective',
};

/**
 * Sessions page: prepare a standup, start it in the Simulated Room, and read any session's timeline and pre-brief (SPEC 13).
 * @returns The page.
 */
export function SessionsPage() {
  const [selected, setSelected] = useState<string | null>(null);
  const [minutes, setMinutes] = useState(15);
  const sessions = useApiQuery((signal) => listSessions(TEAM_ID, signal), TEAM_ID);
  const detail = useApiQuery((signal) => getSession(selected ?? '', signal), selected);
  const prepare = useApiAction(() => prepareStandup(TEAM_ID));
  const start = useApiAction((id: string, m: number | undefined) => startSession(id, m));

  const onPrepare = async () => {
    const id = await prepare.run();
    if (id) {
      sessions.reload();
      setSelected(id);
    }
  };

  const onStart = async (id: string, kind: string) => {
    // A retrospective takes its own timebox (Retro:Minutes), not the standup field.
    const result = await start.run(id, kind === 'Retrospective' ? undefined : minutes);
    if (result) {
      window.open(result.roomUrl, '_blank');
      sessions.reload();
      detail.reload();
    }
  };

  const refresh = () => {
    sessions.reload();
    detail.reload();
  };

  return (
    <>
      <PageHeader
        title="Sessions"
        description="Each session is one standup: a pre-brief built from Azure DevOps, the run itself, and everything that was recorded."
        actions={
          <>
            <Field label="Timebox (min)" className="w-28">
              <Input
                type="number"
                min={1}
                value={String(minutes)}
                onChange={(e) => setMinutes(Math.max(1, Number(e.target.value) || 15))}
                aria-label="Timebox in minutes"
              />
            </Field>
            <Button
              variant="ghost"
              icon={<RefreshCw className="size-4" />}
              onClick={refresh}
              aria-label="Refresh"
            />
            <Button
              variant="primary"
              icon={<Plus className="size-4" />}
              disabled={prepare.busy}
              onClick={() => void onPrepare()}
            >
              Prepare now
            </Button>
          </>
        }
      />
      <div className="grid gap-6">
        <QueryStatus
          loading={prepare.busy || start.busy || sessions.loading}
          error={prepare.error ?? start.error ?? sessions.error}
        />
        {sessions.data?.length === 0 && (
          <EmptyState
            title="No sessions yet"
            hint="Prepare a standup to build the pre-brief from the current sprint."
            action={
              <Button variant="primary" onClick={() => void onPrepare()}>
                Prepare now
              </Button>
            }
          />
        )}
        {sessions.data && sessions.data.length > 0 && (
          <Table
            label="Sessions"
            headings={['Prepared', 'Ceremony', 'Status', 'Flagged items', '']}
          >
            {sessions.data.map((s) => (
              <tr
                key={s.id}
                aria-selected={s.id === selected}
                className={s.id === selected ? 'bg-brand-soft/40' : 'hover:bg-subtle/50'}
              >
                <td className="px-4 py-3">{new Date(s.createdAt).toLocaleString()}</td>
                <td className="px-4 py-3">{ceremonyLabel[s.kind] ?? s.kind}</td>
                <td className="px-4 py-3">
                  <Badge tone={statusTone[s.status] ?? 'neutral'} dot>
                    {s.status}
                  </Badge>
                </td>
                <td className="px-4 py-3">{s.flaggedItems}</td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-2">
                    <Button
                      size="sm"
                      variant={s.id === selected ? 'primary' : 'secondary'}
                      onClick={() => setSelected(s.id)}
                    >
                      View
                    </Button>
                    {s.status === 'Prepared' && (
                      <Button
                        size="sm"
                        icon={<Play className="size-3.5" />}
                        disabled={start.busy}
                        onClick={() => void onStart(s.id, s.kind)}
                      >
                        Simulated Room
                      </Button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </Table>
        )}
        {selected && <QueryStatus loading={detail.loading} error={detail.error} />}
        {selected && detail.data && <SessionTimeline session={detail.data} />}
        {selected && detail.data?.preBrief && <PreBriefView brief={detail.data.preBrief} />}
      </div>
    </>
  );
}
