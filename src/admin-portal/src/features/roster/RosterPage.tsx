import { RefreshCw } from 'lucide-react';
import { TEAM_ID } from '../../api/config';
import { getRoster, syncRoster } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { EmptyState } from '../../ui/Feedback';
import { PageHeader } from '../../ui/PageHeader';
import { Table } from '../../ui/Table';
import { QueryStatus } from '../QueryStatus';

/**
 * Roster page (SPEC 13). DEMO: members come from the seed file; PROD: the Azure DevOps team.
 * @returns The page.
 */
export function RosterPage() {
  const roster = useApiQuery((signal) => getRoster(TEAM_ID, signal), TEAM_ID);
  const sync = useApiAction(() => syncRoster(TEAM_ID));

  const onSync = async () => {
    await sync.run();
    roster.reload();
  };

  return (
    <>
      <PageHeader
        title="Roster"
        description="The people the agent calls on, in speaking order. Local edits survive a sync."
        actions={
          <Button
            variant="primary"
            icon={<RefreshCw className="size-4" />}
            disabled={sync.busy}
            onClick={() => void onSync()}
          >
            Sync roster
          </Button>
        }
      />
      <div className="grid gap-6">
        <QueryStatus loading={roster.loading || sync.busy} error={sync.error ?? roster.error} />
        {roster.data?.length === 0 && (
          <EmptyState title="No members yet" hint="Sync the roster to load the team." />
        )}
        {roster.data && roster.data.length > 0 && (
          <Table label="Roster" headings={['Order', 'Name', 'Pronunciation', 'Key', 'Today']}>
            {roster.data.map((m) => (
              <tr key={m.id}>
                <td className="px-4 py-3 text-muted">{m.speakingOrder}</td>
                <td className="px-4 py-3 font-medium">{m.displayName}</td>
                <td className="px-4 py-3 text-muted">{m.pronunciation}</td>
                <td className="px-4 py-3 text-muted">{m.key}</td>
                <td className="px-4 py-3">
                  <Badge tone={m.active ? 'ok' : 'warn'} dot>
                    {m.active ? 'Present' : 'Absent'}
                  </Badge>
                </td>
              </tr>
            ))}
          </Table>
        )}
      </div>
    </>
  );
}
