import { Trash2, UserPlus, Users } from 'lucide-react';
import { useState } from 'react';
import {
  listMemberships,
  removeMembership,
  setMembership,
  syncMemberships,
  type TeamRole,
} from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert } from '../../ui/Feedback';
import { Field, Input, Select } from '../../ui/Form';
import { Table } from '../../ui/Table';
import { QueryStatus } from '../QueryStatus';

const roles: TeamRole[] = ['Reader', 'Member', 'TeamLead'];

/**
 * Who is in a team and in what role. An Admin or the team's lead manages it; people are keyed by Entra object id, never by name.
 * @param props - The team.
 * @returns The card.
 */
export function MembershipsCard({ teamId, hasGroup }: { teamId: string; hasGroup: boolean }) {
  const members = useApiQuery((signal) => listMemberships(teamId, signal), teamId);
  const [objectId, setObjectId] = useState('');
  const [role, setRole] = useState<TeamRole>('Member');
  const [note, setNote] = useState<string>();
  const add = useApiAction(async () => {
    await setMembership(teamId, objectId.trim(), role);
    return true;
  });
  const change = useApiAction(async (id: string, next: TeamRole) => {
    await setMembership(teamId, id, next);
    return true;
  });
  const remove = useApiAction(async (id: string) => {
    await removeMembership(teamId, id);
    return true;
  });
  const sync = useApiAction(() => syncMemberships(teamId));

  const run = async (done: Promise<unknown>) => {
    await done;
    members.reload();
  };
  const onSync = async () => {
    const added = await sync.run();
    if (added !== undefined) setNote(`Added ${added} from the Entra group.`);
    members.reload();
  };

  return (
    <Card
      title="Members and roles"
      description="Team lead: approve, schedule and edit prompts. Member: answers for themselves. Reader: read only."
      actions={
        hasGroup && (
          <Button
            icon={<Users className="size-4" />}
            disabled={sync.busy}
            onClick={() => void onSync()}
          >
            Sync from Entra group
          </Button>
        )
      }
    >
      <div className="grid gap-4">
        <QueryStatus
          loading={members.loading}
          error={members.error ?? add.error ?? change.error ?? remove.error ?? sync.error}
        />
        {note && <Alert tone="ok">{note}</Alert>}
        {members.data && members.data.length > 0 && (
          <Table label="Memberships" headings={['Entra object id', 'Role', '']}>
            {members.data.map((m) => (
              <tr key={m.objectId}>
                <td className="px-4 py-3 font-mono text-xs">{m.objectId}</td>
                <td className="px-4 py-3">
                  <Select
                    aria-label={`Role of ${m.objectId}`}
                    value={m.role}
                    className="h-8 w-36"
                    onChange={(e) => void run(change.run(m.objectId, e.target.value as TeamRole))}
                  >
                    {roles.map((r) => (
                      <option key={r}>{r}</option>
                    ))}
                  </Select>
                </td>
                <td className="px-4 py-3 text-right">
                  <Button
                    variant="ghost"
                    aria-label={`Remove ${m.objectId}`}
                    icon={<Trash2 className="size-4" />}
                    onClick={() => void run(remove.run(m.objectId))}
                  />
                </td>
              </tr>
            ))}
          </Table>
        )}
        {members.data?.length === 0 && <Badge tone="warn">Nobody is in this team yet</Badge>}
        <form
          className="grid gap-3 sm:grid-cols-[1fr_10rem_auto] sm:items-end"
          onSubmit={(e) => {
            e.preventDefault();
            void run(add.run()).then(() => setObjectId(''));
          }}
        >
          <Field label="Entra object id" hint="From the person's Entra profile.">
            <Input value={objectId} onChange={(e) => setObjectId(e.target.value)} />
          </Field>
          <Field label="Role">
            <Select value={role} onChange={(e) => setRole(e.target.value as TeamRole)}>
              {roles.map((r) => (
                <option key={r}>{r}</option>
              ))}
            </Select>
          </Field>
          <Button
            type="submit"
            variant="primary"
            icon={<UserPlus className="size-4" />}
            disabled={add.busy || !objectId.trim()}
          >
            Add
          </Button>
        </form>
      </div>
    </Card>
  );
}
