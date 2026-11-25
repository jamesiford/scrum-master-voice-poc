import { RefreshCw } from 'lucide-react';
import { getFleet } from '../../api/client';
import { useApiQuery } from '../../hooks/useApiQuery';
import { useTeams } from '../../team/TeamContext';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { EmptyState, Stat } from '../../ui/Feedback';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';
import { connectionTone } from './TeamsAdminPage';

/**
 * Admin fleet dashboard across all teams: how many are active, what needs attention (broken connections, open follow-ups,
 * teams that have never run) and a card for each team that opens it.
 * @returns The page.
 */
export function FleetPage() {
  const fleet = useApiQuery((signal) => getFleet(signal), 'fleet');
  const { select } = useTeams();
  const rows = fleet.data ?? [];
  const active = rows.filter((r) => r.status === 'Active');
  const broken = rows.filter((r) => connectionTone(r) === 'bad');
  const open = rows.reduce((sum, r) => sum + r.openFollowUps, 0);

  return (
    <>
      <PageHeader
        title="Fleet"
        description="All teams at a glance. A team with a broken connection is shown here with the step that failed."
        actions={
          <Button icon={<RefreshCw className="size-4" />} onClick={fleet.reload}>
            Refresh
          </Button>
        }
      />
      <div className="grid gap-6">
        <QueryStatus loading={fleet.loading} error={fleet.error} />
        {fleet.data && (
          <div className="grid gap-4 sm:grid-cols-3">
            <Stat label="Active teams" value={`${active.length} of ${rows.length}`} />
            <Stat label="Connections needing attention" value={String(broken.length)} />
            <Stat label="Open follow-ups" value={String(open)} />
          </div>
        )}
        {fleet.data?.length === 0 && (
          <EmptyState title="No teams yet" hint="Onboard the first team from the Teams page." />
        )}
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
          {rows.map((r) => (
            <Card key={r.teamId} title={r.name} description={r.project}>
              <div className="grid gap-3 text-sm">
                <div className="flex flex-wrap gap-2">
                  <Badge tone={r.status === 'Active' ? 'ok' : 'warn'} dot>
                    {r.status}
                  </Badge>
                  <Badge tone={connectionTone(r)}>{r.connection}</Badge>
                </div>
                <p className="text-muted">Next standup: {r.nextStandup ?? 'not scheduled'}</p>
                <p className="text-muted">
                  Last run: {r.lastRun ? new Date(r.lastRun).toLocaleString() : 'never'}
                </p>
                <p>{r.openFollowUps} open follow-ups</p>
                <Button
                  disabled={r.status === 'Archived'}
                  onClick={() => {
                    select(r.teamId);
                    location.hash = '/overview';
                  }}
                >
                  Open team
                </Button>
              </div>
            </Card>
          ))}
        </div>
      </div>
    </>
  );
}
