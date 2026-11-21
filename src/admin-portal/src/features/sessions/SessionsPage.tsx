import {
  Badge,
  Button,
  Field,
  Input,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Text,
} from '@fluentui/react-components';
import { useState } from 'react';
import { TEAM_ID } from '../../api/config';
import { getSession, listSessions, prepareStandup, startSession } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { QueryStatus } from '../QueryStatus';
import { PreBriefView } from './PreBriefView';
import { SessionTimeline } from './SessionTimeline';

/**
 * Sessions page: list, "Prepare now", "Start in Simulated Room", and the selected session's pre-brief and timeline (SPEC 13).
 * @returns The page.
 */
export function SessionsPage() {
  const [selected, setSelected] = useState<string | null>(null);
  const [minutes, setMinutes] = useState(15);
  const sessions = useApiQuery((signal) => listSessions(TEAM_ID, signal), TEAM_ID);
  const detail = useApiQuery((signal) => getSession(selected ?? '', signal), selected);
  const prepare = useApiAction(() => prepareStandup(TEAM_ID));
  const start = useApiAction((id: string, m: number) => startSession(id, m));

  const onPrepare = async () => {
    const id = await prepare.run();
    if (id) {
      sessions.reload();
      setSelected(id);
    }
  };

  const onStart = async (id: string) => {
    const result = await start.run(id, minutes);
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
    <section style={{ display: 'grid', gap: 12 }}>
      <div style={{ display: 'flex', gap: 12, alignItems: 'end', flexWrap: 'wrap' }}>
        <Button appearance="primary" disabled={prepare.busy} onClick={() => void onPrepare()}>
          Prepare now
        </Button>
        <Field
          label="Timebox (minutes)"
          hint="Use a short timebox, for example 3, to see the time warning."
        >
          <Input
            type="number"
            value={String(minutes)}
            onChange={(_, d) => setMinutes(Math.max(1, Number(d.value) || 15))}
            style={{ width: 90 }}
          />
        </Field>
        <Button onClick={refresh}>Refresh</Button>
      </div>
      <QueryStatus
        loading={prepare.busy || start.busy}
        error={prepare.error ?? start.error ?? sessions.error}
      />
      {sessions.data?.length === 0 && <Text>No sessions yet. Select Prepare now.</Text>}
      {sessions.data && sessions.data.length > 0 && (
        <Table aria-label="Sessions">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Prepared</TableHeaderCell>
              <TableHeaderCell>Status</TableHeaderCell>
              <TableHeaderCell>Flagged items</TableHeaderCell>
              <TableHeaderCell />
            </TableRow>
          </TableHeader>
          <TableBody>
            {sessions.data.map((s) => (
              <TableRow key={s.id} aria-selected={s.id === selected}>
                <TableCell>{new Date(s.createdAt).toLocaleString()}</TableCell>
                <TableCell>
                  <Badge appearance="tint">{s.status}</Badge>
                </TableCell>
                <TableCell>{s.flaggedItems}</TableCell>
                <TableCell>
                  <Button
                    size="small"
                    appearance={s.id === selected ? 'primary' : 'secondary'}
                    onClick={() => setSelected(s.id)}
                  >
                    View
                  </Button>{' '}
                  {s.status === 'Prepared' && (
                    <Button size="small" disabled={start.busy} onClick={() => void onStart(s.id)}>
                      Start in Simulated Room
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
      {selected && <QueryStatus loading={detail.loading} error={detail.error} />}
      {selected && detail.data && <SessionTimeline session={detail.data} />}
      {selected && detail.data?.preBrief && <PreBriefView brief={detail.data.preBrief} />}
    </section>
  );
}
