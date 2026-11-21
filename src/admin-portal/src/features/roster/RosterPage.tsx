import {
  Badge,
  Button,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  Text,
} from '@fluentui/react-components';
import { TEAM_ID } from '../../api/config';
import { getRoster, syncRoster } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
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
    <section style={{ display: 'grid', gap: 12 }}>
      <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
        <Button appearance="primary" disabled={sync.busy} onClick={() => void onSync()}>
          Sync roster
        </Button>
        <Text>Local edits (active, pronunciation, order) survive a sync.</Text>
      </div>
      <QueryStatus loading={roster.loading || sync.busy} error={sync.error ?? roster.error} />
      {roster.data?.length === 0 && <Text>No members yet. Sync the roster.</Text>}
      {roster.data && roster.data.length > 0 && (
        <Table aria-label="Roster">
          <TableHeader>
            <TableRow>
              <TableHeaderCell>Order</TableHeaderCell>
              <TableHeaderCell>Name</TableHeaderCell>
              <TableHeaderCell>Pronunciation</TableHeaderCell>
              <TableHeaderCell>Key</TableHeaderCell>
              <TableHeaderCell>Today</TableHeaderCell>
            </TableRow>
          </TableHeader>
          <TableBody>
            {roster.data.map((m) => (
              <TableRow key={m.id}>
                <TableCell>{m.speakingOrder}</TableCell>
                <TableCell>{m.displayName}</TableCell>
                <TableCell>{m.pronunciation}</TableCell>
                <TableCell>{m.key}</TableCell>
                <TableCell>
                  <Badge appearance="tint" color={m.active ? 'success' : 'warning'}>
                    {m.active ? 'Present' : 'Absent'}
                  </Badge>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </section>
  );
}
