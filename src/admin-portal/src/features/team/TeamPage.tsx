import { useState } from 'react';
import { useTeamId } from '../../team/TeamContext';
import { getTeam, updateTeam, type Team, type TeamSettingsInput } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Alert } from '../../ui/Feedback';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Field, Input, Textarea } from '../../ui/Form';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';
import { MembershipsCard } from '../admin/MembershipsCard';

function toInput(team: Team): TeamSettingsInput {
  return {
    name: team.name,
    timeZone: team.timeZone,
    standupMinutes: team.standupMinutes,
    project: team.ado.project,
    adoTeam: team.ado.team,
    areaPath: team.ado.areaPath,
    staleDays: team.behaviours.staleDays,
    escalateBlockerAfterDays: team.behaviours.escalateBlockerAfterDays,
    sprintGoal: team.sprintGoal,
  };
}

function TeamForm({ team, onSaved }: { team: Team; onSaved: () => void }) {
  const [form, setForm] = useState<TeamSettingsInput>(() => toInput(team));
  const [saved, setSaved] = useState(false);
  const save = useApiAction((input: TeamSettingsInput) => updateTeam(team.id, input));

  const text = (key: keyof TeamSettingsInput, label: string, readOnly = false) => (
    <Field label={label} hint={readOnly ? 'Set by an administrator on the Teams page.' : undefined}>
      <Input
        readOnly={readOnly}
        value={String(form[key])}
        onChange={(e) => setForm({ ...form, [key]: e.target.value })}
      />
    </Field>
  );
  const num = (key: keyof TeamSettingsInput, label: string) => (
    <Field label={label}>
      <Input
        type="number"
        value={String(form[key])}
        onChange={(e) => setForm({ ...form, [key]: Number(e.target.value) })}
      />
    </Field>
  );

  const onSave = async () => {
    setSaved(false);
    const result = await save.run(form);
    if (result === undefined && save.error === undefined) {
      setSaved(true);
      onSaved();
    }
  };

  return (
    <form className="grid max-w-3xl gap-6" onSubmit={(e) => e.preventDefault()}>
      <Card title="Azure DevOps" description="Where the agent reads the sprint from.">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Organisation" hint="Set with Ado__Org; not editable here.">
            <Input value={team.ado.org} readOnly />
          </Field>
          {text('project', 'Project', true)}
          {text('adoTeam', 'Team', true)}
          {text('areaPath', 'Area path', true)}
        </div>
      </Card>
      <Card title="Standup" description="Timing and the rules that raise flags.">
        <div className="grid gap-4 sm:grid-cols-2">
          {text('name', 'Team name')}
          {text('timeZone', 'Time zone')}
          {num('standupMinutes', 'Standup length (minutes)')}
          {num('staleDays', 'Stale after (days in state)')}
          {num('escalateBlockerAfterDays', 'Escalate blockers after (days)')}
        </div>
        <div className="mt-4">
          <Field
            label="Sprint goal"
            hint="Azure DevOps has no sprint goal field, so it is kept here."
          >
            <Textarea
              value={form.sprintGoal}
              rows={3}
              onChange={(e) => setForm({ ...form, sprintGoal: e.target.value })}
            />
          </Field>
        </div>
      </Card>
      {save.error && <Alert tone="bad">{save.error}</Alert>}
      {saved && !save.error && <Alert tone="ok">Saved.</Alert>}
      <div>
        <Button variant="primary" disabled={save.busy} onClick={() => void onSave()}>
          Save changes
        </Button>
      </div>
    </form>
  );
}

/**
 * Team settings page (SPEC 13).
 * @returns The page.
 */
export function TeamPage() {
  const teamId = useTeamId();
  const team = useApiQuery((signal) => getTeam(teamId, signal), teamId);
  return (
    <>
      <PageHeader
        title="Team"
        description="Azure DevOps location, standup timing and the thresholds that decide what gets flagged."
      />
      <QueryStatus loading={team.loading} error={team.error} />
      {team.data && (
        <div className="grid gap-6">
          <TeamForm key={JSON.stringify(team.data)} team={team.data} onSaved={team.reload} />
          <MembershipsCard teamId={teamId} hasGroup={Boolean(team.data.entraGroupId)} />
        </div>
      )}
    </>
  );
}
