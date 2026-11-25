import { Plus } from 'lucide-react';
import { useState } from 'react';
import { getFleet, type FleetRow, type TeamStatus } from '../../api/client';
import { useApiQuery } from '../../hooks/useApiQuery';
import { useTeams } from '../../team/TeamContext';
import { Badge, type Tone } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { EmptyState } from '../../ui/Feedback';
import { PageHeader } from '../../ui/PageHeader';
import { Table } from '../../ui/Table';
import { QueryStatus } from '../QueryStatus';
import { TeamEditor } from './TeamEditor';

const statusTone: Record<TeamStatus, Tone> = {
  Draft: 'neutral',
  Active: 'ok',
  Paused: 'warn',
  Archived: 'neutral',
};

/**
 * Whether a connection health string is healthy; anything else names the step that failed.
 * @param row - The fleet row.
 * @returns The tone to show it in.
 */
export function connectionTone(row: FleetRow): Tone {
  if (row.connection === 'ok') return 'ok';
  return row.connection === 'not checked' ? 'neutral' : 'bad';
}

/**
 * Admin Teams page: every team with its status, next standup, last run, open follow-ups and connection health, and the actions to
 * create, edit, test, sync, schedule, bind, pause and archive.
 * @returns The page.
 */
export function TeamsAdminPage() {
  const fleet = useApiQuery((signal) => getFleet(signal), 'fleet');
  const { teams, reload } = useTeams();
  const [editing, setEditing] = useState<string>();
  const team = teams.find((t) => t.id === editing);

  const changed = () => {
    reload();
    fleet.reload();
  };

  return (
    <>
      <PageHeader
        title="Teams"
        description="Every scrum team the agent serves. Teams are archived, never deleted."
        actions={
          <Button
            variant="primary"
            icon={<Plus className="size-4" />}
            onClick={() => (location.hash = '/onboarding')}
          >
            Onboard a team
          </Button>
        }
      />
      <div className="grid gap-6">
        <QueryStatus loading={fleet.loading} error={fleet.error} />
        {team && (
          <TeamEditor
            key={team.id}
            team={team}
            onChanged={changed}
            onClose={() => setEditing(undefined)}
          />
        )}
        {fleet.data?.length === 0 && (
          <EmptyState title="No teams yet" hint="Onboard the first team with the wizard." />
        )}
        {fleet.data && fleet.data.length > 0 && (
          <Table
            label="Teams"
            headings={[
              'Team',
              'Status',
              'Next standup',
              'Last run',
              'Open follow-ups',
              'Connection',
              '',
            ]}
          >
            {fleet.data.map((r) => (
              <tr key={r.teamId}>
                <td className="px-4 py-3">
                  <p className="font-medium">{r.name}</p>
                  <p className="text-xs text-muted">{r.project}</p>
                </td>
                <td className="px-4 py-3">
                  <Badge tone={statusTone[r.status]} dot>
                    {r.status}
                  </Badge>
                </td>
                <td className="px-4 py-3 text-muted">{r.nextStandup ?? 'Not scheduled'}</td>
                <td className="px-4 py-3 text-muted">
                  {r.lastRun ? new Date(r.lastRun).toLocaleString() : 'Never'}
                </td>
                <td className="px-4 py-3">{r.openFollowUps}</td>
                <td className="px-4 py-3">
                  <Badge tone={connectionTone(r)}>{r.connection}</Badge>
                </td>
                <td className="px-4 py-3 text-right">
                  <Button disabled={r.status === 'Archived'} onClick={() => setEditing(r.teamId)}>
                    Edit
                  </Button>
                </td>
              </tr>
            ))}
          </Table>
        )}
      </div>
    </>
  );
}
