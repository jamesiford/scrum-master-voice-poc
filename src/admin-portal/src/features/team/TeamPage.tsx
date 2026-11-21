import {
  Button,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Textarea,
} from '@fluentui/react-components';
import { useState } from 'react';
import { TEAM_ID } from '../../api/config';
import { getTeam, updateTeam, type Team, type TeamSettingsInput } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { QueryStatus } from '../QueryStatus';

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

  const text = (key: keyof TeamSettingsInput, label: string, hint?: string) => (
    <Field label={label} hint={hint}>
      <Input value={String(form[key])} onChange={(_, d) => setForm({ ...form, [key]: d.value })} />
    </Field>
  );
  const num = (key: keyof TeamSettingsInput, label: string) => (
    <Field label={label}>
      <Input
        type="number"
        value={String(form[key])}
        onChange={(_, d) => setForm({ ...form, [key]: Number(d.value) })}
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
    <form style={{ display: 'grid', gap: 12, maxWidth: 520 }} onSubmit={(e) => e.preventDefault()}>
      <Field label="Azure DevOps organisation" hint="Set with Ado__Org; not editable here.">
        <Input value={team.ado.org} readOnly />
      </Field>
      {text('name', 'Team name')}
      {text('project', 'Azure DevOps project')}
      {text('adoTeam', 'Azure DevOps team')}
      {text('areaPath', 'Area path')}
      {text('timeZone', 'Time zone')}
      {num('standupMinutes', 'Standup length (minutes)')}
      {num('staleDays', 'Stale after (days in state)')}
      {num('escalateBlockerAfterDays', 'Escalate blockers after (days)')}
      <Field label="Sprint goal" hint="Azure DevOps has no sprint goal field, so it is kept here.">
        <Textarea
          value={form.sprintGoal}
          onChange={(_, d) => setForm({ ...form, sprintGoal: d.value })}
        />
      </Field>
      <div>
        <Button appearance="primary" disabled={save.busy} onClick={() => void onSave()}>
          Save
        </Button>
      </div>
      {save.error && (
        <MessageBar intent="error">
          <MessageBarBody>{save.error}</MessageBarBody>
        </MessageBar>
      )}
      {saved && !save.error && (
        <MessageBar intent="success">
          <MessageBarBody>Saved.</MessageBarBody>
        </MessageBar>
      )}
    </form>
  );
}

/**
 * Team settings page (SPEC 13). Human Scrum Master and Teams channel arrive with M3.
 * @returns The page.
 */
export function TeamPage() {
  const team = useApiQuery((signal) => getTeam(TEAM_ID, signal), TEAM_ID);
  return (
    <section style={{ display: 'grid', gap: 12 }}>
      <QueryStatus loading={team.loading} error={team.error} />
      {team.data && (
        <TeamForm key={JSON.stringify(team.data)} team={team.data} onSaved={team.reload} />
      )}
    </section>
  );
}
