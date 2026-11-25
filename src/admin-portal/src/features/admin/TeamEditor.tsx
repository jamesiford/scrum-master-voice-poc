import {
  Archive,
  CalendarClock,
  Pause,
  Play,
  PlugZap,
  RefreshCw,
  Save,
  Trash2,
} from 'lucide-react';
import { useState } from 'react';
import {
  archiveTeam,
  getPortalConfig,
  syncRoster,
  testTeamConnection,
  updateTeamRegistry,
  type Ceremonies,
  type ConnectionReport,
  type Team,
  type TeamRegistryInput,
  type TeamsBinding,
} from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useTeams } from '../../team/TeamContext';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert } from '../../ui/Feedback';
import { Check, Field, Input, Select } from '../../ui/Form';
import { ConnectionReportView } from './ConnectionReportView';
import { MembershipsCard } from './MembershipsCard';

function toInput(team: Team): TeamRegistryInput {
  return {
    name: team.name,
    timeZone: team.timeZone,
    standupMinutes: team.standupMinutes,
    ado: { ...team.ado },
    sprintGoal: team.sprintGoal,
    ceremonies: { ...team.ceremonyFlags },
    status: team.status,
    teamsBindings: team.bindings.map((b) => ({ ...b })),
    entraGroupId: team.entraGroupId,
  };
}

const ceremonyLabels: [keyof Ceremonies, string][] = [
  ['dailyScrum', 'Daily scrum'],
  ['planning', 'Sprint planning'],
  ['retrospective', 'Retrospective'],
  ['review', 'Sprint review'],
];

/**
 * Admin editor for one team: connection, ceremonies, Teams bindings, pause and archive, plus the connection test, roster sync and members.
 * @param props - The team and callbacks.
 * @returns The editor.
 */
export function TeamEditor({
  team,
  onChanged,
  onClose,
}: {
  team: Team;
  onChanged: () => void;
  onClose: () => void;
}) {
  const { select } = useTeams();
  const [form, setForm] = useState<TeamRegistryInput>(() => toInput(team));
  const [report, setReport] = useState<ConnectionReport>();
  const [note, setNote] = useState<string>();
  const [conversation, setConversation] = useState('');
  const [kind, setKind] = useState<'channel' | 'chat'>('channel');
  const [purpose, setPurpose] = useState<TeamsBinding['purpose']>('Recap');

  const save = useApiAction(async (input: TeamRegistryInput) => {
    await updateTeamRegistry(team.id, input);
    return true;
  });
  const test = useApiAction(() => testTeamConnection(team.id));
  const sync = useApiAction(async () => {
    await syncRoster(team.id);
    return true;
  });
  const archive = useApiAction(async () => {
    await archiveTeam(team.id);
    return true;
  });

  const persist = async (input: TeamRegistryInput, message: string) => {
    if (await save.run(input)) {
      setForm(input);
      setNote(message);
      onChanged();
    }
  };
  const addBinding = async () => {
    const config = await getPortalConfig();
    const binding: TeamsBinding = {
      tenantId: config.tenantId ?? '',
      teamId: null,
      channelId: kind === 'channel' ? conversation.trim() : null,
      chatId: kind === 'chat' ? conversation.trim() : null,
      purpose,
    };
    setForm({ ...form, teamsBindings: [...(form.teamsBindings ?? []), binding] });
    setConversation('');
  };
  const dropBinding = (index: number) =>
    setForm({ ...form, teamsBindings: (form.teamsBindings ?? []).filter((_, i) => i !== index) });
  const setAdo = (key: keyof TeamRegistryInput['ado'], value: string) =>
    setForm({ ...form, ado: { ...form.ado, [key]: value } });

  const paused = form.status === 'Paused';
  const busy = save.busy || test.busy || sync.busy || archive.busy;
  const error = save.error ?? test.error ?? sync.error ?? archive.error;

  return (
    <div className="grid gap-6">
      <Card
        title={`Edit ${team.name}`}
        description={`Team id ${team.id}. Status: ${team.status}.`}
        actions={
          <Button variant="ghost" onClick={onClose}>
            Close
          </Button>
        }
      >
        <form className="grid max-w-3xl gap-5" onSubmit={(e) => e.preventDefault()}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Name">
              <Input
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </Field>
            <Field label="Time zone" hint="IANA name, for example Europe/London.">
              <Input
                value={form.timeZone}
                onChange={(e) => setForm({ ...form, timeZone: e.target.value })}
              />
            </Field>
            <Field
              label="Azure DevOps organisation"
              hint="https://dev.azure.com/name, in this tenant."
            >
              <Input value={form.ado.org} onChange={(e) => setAdo('org', e.target.value)} />
            </Field>
            <Field label="Project">
              <Input value={form.ado.project} onChange={(e) => setAdo('project', e.target.value)} />
            </Field>
            <Field label="Azure DevOps team">
              <Input value={form.ado.team} onChange={(e) => setAdo('team', e.target.value)} />
            </Field>
            <Field label="Area path" hint="Writes outside this area path are refused.">
              <Input
                value={form.ado.areaPath}
                onChange={(e) => setAdo('areaPath', e.target.value)}
              />
            </Field>
            <Field label="Standup minutes">
              <Input
                type="number"
                value={form.standupMinutes}
                onChange={(e) => setForm({ ...form, standupMinutes: Number(e.target.value) })}
              />
            </Field>
            <Field
              label="Entra group (optional)"
              hint="Group whose members are synced into the team."
            >
              <Input
                value={form.entraGroupId ?? ''}
                onChange={(e) => setForm({ ...form, entraGroupId: e.target.value || null })}
              />
            </Field>
          </div>
          <Field label="Sprint goal">
            <Input
              value={form.sprintGoal}
              onChange={(e) => setForm({ ...form, sprintGoal: e.target.value })}
            />
          </Field>

          <fieldset className="grid gap-2">
            <legend className="mb-1 text-sm font-medium">Ceremonies this team runs</legend>
            <div className="flex flex-wrap gap-x-6 gap-y-2">
              {ceremonyLabels.map(([key, label]) => (
                <Check
                  key={key}
                  label={label}
                  checked={form.ceremonies?.[key] ?? false}
                  onChange={(checked) =>
                    setForm({
                      ...form,
                      ceremonies: { ...(form.ceremonies as Ceremonies), [key]: checked },
                    })
                  }
                />
              ))}
            </div>
          </fieldset>

          <fieldset className="grid gap-3">
            <legend className="mb-1 text-sm font-medium">Teams channels and chats</legend>
            {(form.teamsBindings ?? []).length === 0 && (
              <p className="text-sm text-muted">
                None bound. Cards stay in the outbox until a channel or chat is bound.
              </p>
            )}
            {(form.teamsBindings ?? []).map((b, i) => (
              <div
                key={`${b.channelId ?? b.chatId}-${b.purpose}`}
                className="flex items-center gap-3"
              >
                <Badge tone="brand">{b.purpose}</Badge>
                <span className="truncate font-mono text-xs">{b.channelId ?? b.chatId}</span>
                <Button
                  variant="ghost"
                  aria-label="Remove binding"
                  icon={<Trash2 className="size-4" />}
                  onClick={() => dropBinding(i)}
                />
              </div>
            ))}
            <div className="grid gap-3 sm:grid-cols-[1fr_8rem_8rem_auto] sm:items-end">
              <Field label="Conversation id">
                <Input value={conversation} onChange={(e) => setConversation(e.target.value)} />
              </Field>
              <Field label="Kind">
                <Select
                  value={kind}
                  onChange={(e) => setKind(e.target.value as 'channel' | 'chat')}
                >
                  <option value="channel">Channel</option>
                  <option value="chat">Chat</option>
                </Select>
              </Field>
              <Field label="Purpose">
                <Select
                  value={purpose}
                  onChange={(e) => setPurpose(e.target.value as TeamsBinding['purpose'])}
                >
                  <option>Recap</option>
                  <option>Standup</option>
                  <option>Planning</option>
                </Select>
              </Field>
              <Button disabled={!conversation.trim()} onClick={() => void addBinding()}>
                Add binding
              </Button>
            </div>
          </fieldset>

          <div className="flex flex-wrap gap-3">
            <Button
              variant="primary"
              icon={<Save className="size-4" />}
              disabled={busy}
              onClick={() => void persist(form, 'Saved.')}
            >
              Save
            </Button>
            <Button
              icon={<PlugZap className="size-4" />}
              disabled={busy}
              onClick={() => void test.run().then(setReport)}
            >
              Test connection
            </Button>
            <Button
              icon={<RefreshCw className="size-4" />}
              disabled={busy}
              onClick={() => void sync.run().then((ok) => ok && setNote('Roster synced.'))}
            >
              Sync roster
            </Button>
            <Button
              icon={<CalendarClock className="size-4" />}
              onClick={() => {
                select(team.id);
                location.hash = '/schedule';
              }}
            >
              Schedule
            </Button>
            <Button
              icon={paused ? <Play className="size-4" /> : <Pause className="size-4" />}
              disabled={busy || team.status === 'Draft'}
              onClick={() =>
                void persist(
                  { ...form, status: paused ? 'Active' : 'Paused' },
                  paused ? 'Resumed.' : 'Paused: no ceremony starts until it is resumed.',
                )
              }
            >
              {paused ? 'Resume' : 'Pause'}
            </Button>
            {team.status === 'Draft' && (
              <Button
                variant="primary"
                disabled={busy}
                onClick={() => void persist({ ...form, status: 'Active' }, 'Enabled.')}
              >
                Enable team
              </Button>
            )}
            <Button
              variant="danger"
              icon={<Archive className="size-4" />}
              disabled={busy}
              onClick={() => {
                if (
                  window.confirm(`Archive ${team.name}? It becomes read-only and is never deleted.`)
                ) {
                  void archive.run().then((ok) => {
                    if (ok) {
                      onChanged();
                      onClose();
                    }
                  });
                }
              }}
            >
              Archive
            </Button>
          </div>
          {error && <Alert tone="bad">{error}</Alert>}
          {note && !error && <Alert tone="ok">{note}</Alert>}
          {report && <ConnectionReportView report={report} />}
        </form>
      </Card>
      <MembershipsCard teamId={team.id} hasGroup={Boolean(form.entraGroupId)} />
    </div>
  );
}
