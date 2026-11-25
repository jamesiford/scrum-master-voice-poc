import { useState } from 'react';
import {
  createTeam,
  getPortalConfig,
  listAdoProjects,
  listAdoTeams,
  syncRoster,
  testConnection,
  updateSchedule,
  updateTeamRegistry,
  type ConnectionReport,
  type TeamRegistryInput,
} from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useTeams } from '../../team/TeamContext';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert } from '../../ui/Feedback';
import { Field, Input, Select } from '../../ui/Form';
import { PageHeader } from '../../ui/PageHeader';
import { cx } from '../../ui/cx';
import { ConnectionReportView } from './ConnectionReportView';

const steps = ['Team', 'Azure DevOps', 'Test', 'Roster', 'Schedule', 'Teams', 'Enable'] as const;
const weekdays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

/**
 * Onboarding wizard for a new team: name, Azure DevOps organisation with project and team pickers read from the organisation, a connection
 * test, roster, schedule, Teams binding and enable. The team is created as a draft and only runs once enabled.
 * @returns The wizard.
 */
export function OnboardingWizard() {
  const { reload, select } = useTeams();
  const [step, setStep] = useState(0);
  const [id, setId] = useState('');
  const [form, setForm] = useState<TeamRegistryInput>({
    name: '',
    timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    standupMinutes: 15,
    ado: { org: '', project: '', team: '', areaPath: '' },
    sprintGoal: '',
  });
  const [projects, setProjects] = useState<string[]>([]);
  const [adoTeams, setAdoTeams] = useState<{ name: string; defaultAreaPath: string | null }[]>([]);
  const [report, setReport] = useState<ConnectionReport>();
  const [time, setTime] = useState('09:30');
  const [conversation, setConversation] = useState('');
  const [done, setDone] = useState(false);

  const loadProjects = useApiAction(async () => {
    const found = await listAdoProjects(form.ado.org);
    setProjects(found.map((p) => p.name));
    return true;
  });
  const loadTeams = useApiAction(async (project: string) => {
    const found = await listAdoTeams(form.ado.org, project);
    setAdoTeams(found);
    return true;
  });
  const test = useApiAction(() => testConnection(form.ado));
  const create = useApiAction(async () => {
    await createTeam(id, form);
    await syncRoster(id);
    return true;
  });
  const schedule = useApiAction(async () => {
    await updateSchedule(id, {
      enabled: true,
      days: weekdays,
      startTime: time,
      mode: 'simulated',
      joinUrl: null,
    });
    return true;
  });
  const bind = useApiAction(async () => {
    if (conversation.trim()) {
      const config = await getPortalConfig();
      const next = {
        ...form,
        teamsBindings: [
          {
            tenantId: config.tenantId ?? '',
            teamId: null,
            channelId: conversation.trim(),
            chatId: null,
            purpose: 'Recap' as const,
          },
        ],
      };
      await updateTeamRegistry(id, next);
      setForm(next);
    }
    return true;
  });
  const enable = useApiAction(async () => {
    await updateTeamRegistry(id, { ...form, status: 'Active' });
    return true;
  });

  const next = () => setStep((s) => s + 1);
  const setAdo = (key: keyof TeamRegistryInput['ado'], value: string) =>
    setForm({ ...form, ado: { ...form.ado, [key]: value } });
  const error =
    loadProjects.error ??
    loadTeams.error ??
    test.error ??
    create.error ??
    schedule.error ??
    bind.error ??
    enable.error;
  const idOk = /^[a-z][a-z0-9-]{1,39}$/.test(id);

  const finish = async () => {
    if (await enable.run()) {
      reload();
      select(id);
      setDone(true);
    }
  };

  return (
    <>
      <PageHeader
        title="Onboard a team"
        description="Seven short steps. The team is a draft until the last one, so nothing runs by accident."
      />
      <ol className="mb-6 flex flex-wrap gap-2" aria-label="Steps">
        {steps.map((s, i) => (
          <li
            key={s}
            aria-current={i === step ? 'step' : undefined}
            className={cx(
              'rounded-full px-3 py-1 text-xs font-medium',
              i === step
                ? 'bg-brand text-brand-ink'
                : i < step
                  ? 'bg-brand-soft text-brand'
                  : 'bg-subtle text-muted',
            )}
          >
            {i + 1}. {s}
          </li>
        ))}
      </ol>
      <div className="grid max-w-2xl gap-5">
        {error && <Alert tone="bad">{error}</Alert>}
        {done && (
          <Alert tone="ok">
            {form.name} is active. Open Teams to bind more channels or add members.
          </Alert>
        )}

        {step === 0 && (
          <Card title="Name the team">
            <div className="grid gap-4">
              <Field label="Name">
                <Input
                  value={form.name}
                  onChange={(e) => setForm({ ...form, name: e.target.value })}
                />
              </Field>
              <Field
                label="Team id"
                hint="Lower case letters, digits and hyphens, for example team-payments."
              >
                <Input value={id} onChange={(e) => setId(e.target.value)} />
              </Field>
              <Field
                label="Time zone"
                hint="IANA name. The standup runs at its start time in this zone."
              >
                <Input
                  value={form.timeZone}
                  onChange={(e) => setForm({ ...form, timeZone: e.target.value })}
                />
              </Field>
              <Field label="Sprint goal">
                <Input
                  value={form.sprintGoal}
                  onChange={(e) => setForm({ ...form, sprintGoal: e.target.value })}
                />
              </Field>
              <Button variant="primary" disabled={!idOk || !form.name.trim()} onClick={next}>
                Next
              </Button>
            </div>
          </Card>
        )}

        {step === 1 && (
          <Card
            title="Azure DevOps"
            description="The organisation must be in this tenant, and the API's managed identity must be a user in it."
          >
            <div className="grid gap-4">
              <Field label="Organisation URL" hint="https://dev.azure.com/name">
                <Input value={form.ado.org} onChange={(e) => setAdo('org', e.target.value)} />
              </Field>
              <Button
                disabled={!form.ado.org || loadProjects.busy}
                onClick={() => void loadProjects.run()}
              >
                Load projects
              </Button>
              {projects.length > 0 && (
                <Field label="Project">
                  <Select
                    value={form.ado.project}
                    onChange={(e) => {
                      setAdo('project', e.target.value);
                      void loadTeams.run(e.target.value);
                    }}
                  >
                    <option value="">Choose a project</option>
                    {projects.map((p) => (
                      <option key={p}>{p}</option>
                    ))}
                  </Select>
                </Field>
              )}
              {adoTeams.length > 0 && (
                <Field label="Team">
                  <Select
                    value={form.ado.team}
                    onChange={(e) => {
                      const picked = adoTeams.find((t) => t.name === e.target.value);
                      setForm({
                        ...form,
                        ado: {
                          ...form.ado,
                          team: e.target.value,
                          areaPath: picked?.defaultAreaPath ?? form.ado.areaPath,
                        },
                      });
                    }}
                  >
                    <option value="">Choose a team</option>
                    {adoTeams.map((t) => (
                      <option key={t.name}>{t.name}</option>
                    ))}
                  </Select>
                </Field>
              )}
              <Field label="Area path" hint="Writes outside this area path are refused.">
                <Input
                  value={form.ado.areaPath}
                  onChange={(e) => setAdo('areaPath', e.target.value)}
                />
              </Field>
              <Button
                variant="primary"
                disabled={!form.ado.project || !form.ado.team || !form.ado.areaPath}
                onClick={next}
              >
                Next
              </Button>
            </div>
          </Card>
        )}

        {step === 2 && (
          <Card
            title="Test the connection"
            description="Reaches the organisation, the project and team, the current sprint and the roster."
          >
            <div className="grid gap-4">
              <Button disabled={test.busy} onClick={() => void test.run().then(setReport)}>
                Run test
              </Button>
              {report && <ConnectionReportView report={report} />}
              <Button variant="primary" disabled={!report?.ok} onClick={next}>
                Next
              </Button>
            </div>
          </Card>
        )}

        {step === 3 && (
          <Card
            title="Create the team and load its roster"
            description="Reads the team's members from Azure DevOps."
          >
            <Button
              variant="primary"
              disabled={create.busy}
              onClick={() => void create.run().then((ok) => ok && next())}
            >
              Create team and sync roster
            </Button>
          </Card>
        )}

        {step === 4 && (
          <Card title="Schedule" description="Weekdays, in the team's time zone.">
            <div className="grid gap-4">
              <Field label="Standup start time">
                <Input type="time" value={time} onChange={(e) => setTime(e.target.value)} />
              </Field>
              <Button
                variant="primary"
                disabled={schedule.busy}
                onClick={() => void schedule.run().then((ok) => ok && next())}
              >
                Save schedule
              </Button>
            </div>
          </Card>
        )}

        {step === 5 && (
          <Card
            title="Teams channel"
            description="Optional. Bind the channel where the recap and follow-ups are posted. You can do this later from Teams."
          >
            <div className="grid gap-4">
              <Field label="Channel conversation id">
                <Input value={conversation} onChange={(e) => setConversation(e.target.value)} />
              </Field>
              <Button
                variant="primary"
                disabled={bind.busy}
                onClick={() => void bind.run().then((ok) => ok && next())}
              >
                {conversation.trim() ? 'Bind and continue' : 'Skip'}
              </Button>
            </div>
          </Card>
        )}

        {step === 6 && (
          <Card
            title="Enable"
            description="The team starts running its ceremonies from its schedule."
          >
            <Button variant="primary" disabled={enable.busy || done} onClick={() => void finish()}>
              Enable {form.name}
            </Button>
          </Card>
        )}
      </div>
    </>
  );
}
