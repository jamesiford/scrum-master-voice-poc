import {
  Badge,
  Button,
  Dropdown,
  Field,
  Input,
  Option,
  ProgressBar,
  Text,
} from '@fluentui/react-components';
import { useEffect, useState } from 'react';
import { TEAM_ID } from '../../api/config';
import {
  controlSession,
  getSession,
  listSessions,
  resetDemo,
  startTeamsSession,
} from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { useLiveState } from '../../hooks/useLiveState';
import { QueryStatus } from '../QueryStatus';
import { SessionTimeline } from '../sessions/SessionTimeline';

/**
 * Live console: watch the floor, timebox, parking lot and bridge status of a running standup, and take over or end it.
 * Also starts a standup in a Teams meeting and resets the demo.
 * @returns The page.
 */
export function LivePage() {
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [joinUrl, setJoinUrl] = useState('');
  const sessions = useApiQuery((signal) => listSessions(TEAM_ID, signal), TEAM_ID);
  const detail = useApiQuery((signal) => getSession(sessionId ?? '', signal), sessionId);
  const { state, connected } = useLiveState(sessionId);
  const control = useApiAction((action: string) => controlSession(sessionId ?? '', action));
  const join = useApiAction(() => startTeamsSession(sessionId ?? '', joinUrl, 15));
  const reset = useApiAction(() => resetDemo());

  useEffect(() => {
    if (!sessionId) return;
    const timer = setInterval(detail.reload, 4000);
    return () => clearInterval(timer);
  }, [sessionId, detail.reload]);

  const refresh = () => {
    sessions.reload();
    detail.reload();
  };
  const send = (action: string) => void control.run(action).then(refresh);
  const live = state && !state.ended;
  const bridgeEvents = detail.data?.events.filter((e) => e.type === 'Bridge') ?? [];

  return (
    <section style={{ display: 'grid', gap: 12 }}>
      <QueryStatus
        loading={sessions.loading}
        error={sessions.error ?? control.error ?? join.error ?? reset.error}
      />
      <div style={{ display: 'flex', gap: 12, alignItems: 'end', flexWrap: 'wrap' }}>
        <Field label="Session">
          <Dropdown
            placeholder="Choose a session"
            onOptionSelect={(_, d) => setSessionId(d.optionValue ?? null)}
          >
            {(sessions.data ?? []).map((s) => (
              <Option key={s.id} value={s.id} text={`${s.status} ${s.createdAt}`}>
                {s.status} {new Date(s.createdAt).toLocaleString()}
              </Option>
            ))}
          </Dropdown>
        </Field>
        <Button onClick={refresh}>Refresh</Button>
        <Button
          onClick={() => {
            if (
              window.confirm(
                'Delete all sessions, follow-ups and cards? Azure DevOps is not touched.',
              )
            )
              void reset.run().then(() => {
                setSessionId(null);
                sessions.reload();
              });
          }}
        >
          Reset demo
        </Button>
      </div>

      {sessionId && (
        <div style={{ display: 'flex', gap: 12, alignItems: 'end', flexWrap: 'wrap' }}>
          <Field label="Teams meeting link" style={{ minWidth: 420 }}>
            <Input value={joinUrl} onChange={(_, d) => setJoinUrl(d.value)} />
          </Field>
          <Button
            appearance="primary"
            disabled={!joinUrl || join.busy || detail.data?.status !== 'Prepared'}
            onClick={() => void join.run().then(refresh)}
          >
            Start in Teams
          </Button>
        </div>
      )}

      {bridgeEvents.length > 0 && (
        <Text weight="semibold">Bridge: {bridgeEvents[bridgeEvents.length - 1]?.text}</Text>
      )}

      {sessionId && (
        <div style={{ display: 'grid', gap: 8 }}>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <Badge color={live ? 'success' : 'informative'}>
              {live ? 'Live' : connected ? 'Waiting' : 'Not running'}
            </Badge>
            {live && <Text>Floor: {state.floor}</Text>}
            {live && state.currentSpeaker && <Text>Speaker: {state.currentSpeaker}</Text>}
            {live && state.agentSpeaking && <Badge>Agent speaking</Badge>}
          </div>
          {live && (
            <>
              <ProgressBar
                value={Math.max(0, state.remainingSec)}
                max={(detail.data?.standupMinutes ?? 15) * 60}
              />
              <Text>
                {Math.floor(state.remainingSec / 60)}:
                {String(state.remainingSec % 60).padStart(2, '0')} left. Latency p50 {state.p50} ms,
                p95 {state.p95} ms.
              </Text>
              {state.parkingLot.length > 0 && (
                <Text>Parking lot: {state.parkingLot.join('; ')}</Text>
              )}
              <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                <Button onClick={() => send('begin')} disabled={state.running}>
                  Begin
                </Button>
                <Button onClick={() => send('skip')}>Skip speaker</Button>
                <Button onClick={() => send('mute')}>Mute agent</Button>
                <Button onClick={() => send('unmute')}>Unmute</Button>
                <Button onClick={() => send('takeover')}>Take over</Button>
                <Button onClick={() => send('release')}>Hand back</Button>
                <Button appearance="primary" onClick={() => send('end')}>
                  End standup
                </Button>
              </div>
            </>
          )}
          {detail.data && <SessionTimeline session={detail.data} />}
        </div>
      )}
    </section>
  );
}
