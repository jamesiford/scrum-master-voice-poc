import { Button, Checkbox, Dropdown, Field, Input, Option, Text } from '@fluentui/react-components';
import { useEffect, useState } from 'react';
import { TEAM_ID } from '../../api/config';
import { getSchedule, updateSchedule } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { QueryStatus } from '../QueryStatus';

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

/**
 * Schedule page: when the standup runs and how it starts. The scheduler prepares ten minutes before and, in Teams mode, joins one minute before.
 * @returns The page.
 */
export function SchedulePage() {
  const schedule = useApiQuery((signal) => getSchedule(TEAM_ID, signal), TEAM_ID);
  const [enabled, setEnabled] = useState(false);
  const [days, setDays] = useState<string[]>([]);
  const [time, setTime] = useState('09:30');
  const [mode, setMode] = useState<'simulated' | 'acs'>('simulated');
  const [joinUrl, setJoinUrl] = useState('');
  const save = useApiAction(() =>
    updateSchedule(TEAM_ID, { enabled, days, startTime: time, mode, joinUrl: joinUrl || null }),
  );

  useEffect(() => {
    const s = schedule.data;
    if (!s) return;
    setEnabled(s.enabled);
    setDays(s.days);
    setTime(s.startTime.slice(0, 5));
    setMode(s.mode);
    setJoinUrl(s.joinUrl ?? '');
  }, [schedule.data]);

  const toggle = (day: string, on: boolean) =>
    setDays((d) => (on ? [...d, day] : d.filter((x) => x !== day)));

  return (
    <section style={{ display: 'grid', gap: 12, maxWidth: 640 }}>
      <QueryStatus loading={schedule.loading} error={schedule.error ?? save.error} />
      <Checkbox
        checked={enabled}
        onChange={(_, d) => setEnabled(d.checked === true)}
        label="Run this standup automatically"
      />
      <Field label="Days">
        <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
          {DAYS.map((d) => (
            <Checkbox
              key={d}
              checked={days.includes(d)}
              onChange={(_, x) => toggle(d, x.checked === true)}
              label={d.slice(0, 3)}
            />
          ))}
        </div>
      </Field>
      <Field label="Start time (team time zone)">
        <Input type="time" value={time} onChange={(_, d) => setTime(d.value)} />
      </Field>
      <Field label="Mode">
        <Dropdown
          value={mode === 'acs' ? 'Join the Teams meeting' : 'Prepare only (Simulated Room)'}
          selectedOptions={[mode]}
          onOptionSelect={(_, d) => setMode(d.optionValue === 'acs' ? 'acs' : 'simulated')}
        >
          <Option value="simulated">Prepare only (Simulated Room)</Option>
          <Option value="acs">Join the Teams meeting</Option>
        </Dropdown>
      </Field>
      {mode === 'acs' && (
        <Field label="Teams meeting link">
          <Input value={joinUrl} onChange={(_, d) => setJoinUrl(d.value)} />
        </Field>
      )}
      <Text size={200}>
        Prepare runs at T-10 minutes and posts the pre-brief card. Teams mode starts and joins at
        T-1 minute; the agent waits in the lobby until admitted.
      </Text>
      <div>
        <Button
          appearance="primary"
          disabled={save.busy}
          onClick={() => void save.run().then(schedule.reload)}
        >
          Save schedule
        </Button>
      </div>
    </section>
  );
}
