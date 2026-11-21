import { useEffect, useState } from 'react';
import { TEAM_ID } from '../../api/config';
import { getSchedule, updateSchedule } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Alert } from '../../ui/Feedback';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Check, Field, Input, Select } from '../../ui/Form';
import { PageHeader } from '../../ui/PageHeader';
import { cx } from '../../ui/cx';
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
  const [saved, setSaved] = useState(false);
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

  const toggle = (day: string) =>
    setDays((d) => (d.includes(day) ? d.filter((x) => x !== day) : [...d, day]));
  const onSave = async () => {
    setSaved(false);
    await save.run();
    schedule.reload();
    setSaved(true);
  };

  return (
    <>
      <PageHeader
        title="Schedule"
        description="Run the standup on its own. Preparation starts ten minutes before, and Teams mode joins one minute before."
      />
      <div className="grid max-w-2xl gap-6">
        <QueryStatus loading={schedule.loading} error={schedule.error ?? save.error} />
        <Card>
          <div className="grid gap-6">
            <Check checked={enabled} onChange={setEnabled} label="Run this standup automatically" />
            <div className="grid gap-1.5">
              <span className="text-sm font-medium">Days</span>
              <div className="flex flex-wrap gap-2">
                {DAYS.map((d) => (
                  <button
                    key={d}
                    type="button"
                    aria-pressed={days.includes(d)}
                    onClick={() => toggle(d)}
                    className={cx(
                      'h-9 w-12 rounded-lg border text-sm font-medium transition',
                      days.includes(d)
                        ? 'border-brand bg-brand-soft text-brand'
                        : 'border-line text-muted hover:bg-subtle',
                    )}
                  >
                    {d.slice(0, 3)}
                  </button>
                ))}
              </div>
            </div>
            <Field label="Start time" hint="In the team's time zone.">
              <Input
                type="time"
                value={time}
                onChange={(e) => setTime(e.target.value)}
                className="max-w-40"
              />
            </Field>
            <Field label="Mode">
              <Select
                value={mode}
                onChange={(e) => setMode(e.target.value === 'acs' ? 'acs' : 'simulated')}
                className="max-w-sm"
              >
                <option value="simulated">Prepare only (Simulated Room)</option>
                <option value="acs">Join the Teams meeting</option>
              </Select>
            </Field>
            {mode === 'acs' && (
              <Field label="Teams meeting link">
                <Input value={joinUrl} onChange={(e) => setJoinUrl(e.target.value)} />
              </Field>
            )}
          </div>
        </Card>
        {saved && !save.error && <Alert tone="ok">Schedule saved.</Alert>}
        <div>
          <Button variant="primary" disabled={save.busy} onClick={() => void onSave()}>
            Save schedule
          </Button>
        </div>
      </div>
    </>
  );
}
