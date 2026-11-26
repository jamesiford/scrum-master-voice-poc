import {
  Hand,
  Mic,
  MicOff,
  Play,
  SkipForward,
  Square,
  Undo2,
  Video,
  RefreshCw,
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTeamId } from '../../team/TeamContext';
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
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert, EmptyState, Progress } from '../../ui/Feedback';
import { Field, Input, Select } from '../../ui/Form';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';
import { SessionTimeline } from '../sessions/SessionTimeline';

/**
 * Live console: watch the floor, timebox, parking lot and bridge status of a running standup, and take over or end it.
 * Also starts a standup in a Teams meeting and resets the demo.
 * @returns The page.
 */
export function LivePage() {
  const teamId = useTeamId();
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [joinUrl, setJoinUrl] = useState('');
  const sessions = useApiQuery((signal) => listSessions(teamId, signal), teamId);
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
  const live = state && !state.ended ? state : undefined;
  const bridgeEvents = detail.data?.events.filter((e) => e.type === 'Bridge') ?? [];
  const bridge = bridgeEvents[bridgeEvents.length - 1]?.text;
  const bridgeTone =
    bridge?.startsWith('Failed') || bridge?.startsWith('Ended')
      ? 'bad'
      : bridge?.startsWith('Lobby')
        ? 'warn'
        : 'info';

  return (
    <>
      <PageHeader
        title="Live console"
        description="Watch a running standup and steer it. Start one in a Teams meeting by pasting the join link."
        actions={
          <>
            <Button
              variant="ghost"
              icon={<RefreshCw className="size-4" />}
              aria-label="Refresh"
              onClick={refresh}
            />
            <Button
              variant="danger"
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
          </>
        }
      />
      <div className="grid gap-6">
        <QueryStatus
          loading={sessions.loading}
          error={sessions.error ?? control.error ?? join.error ?? reset.error}
        />
        <Card>
          <Field label="Session" className="max-w-md">
            <Select value={sessionId ?? ''} onChange={(e) => setSessionId(e.target.value || null)}>
              <option value="">Choose a session</option>
              {(sessions.data ?? []).map((s) => (
                <option key={s.id} value={s.id}>
                  {s.status} · {new Date(s.createdAt).toLocaleString()}
                </option>
              ))}
            </Select>
          </Field>
        </Card>
        {!sessionId && (
          <EmptyState
            title="Pick a session to watch"
            hint="Prepare one on the Sessions page first. A live standup shows its floor, timer and controls here."
          />
        )}
        {sessionId && (
          <>
            <Card
              title="Join a Teams meeting"
              description="The agent joins as “Zava Scrum Agent” and waits in the lobby until admitted."
            >
              <div className="flex flex-wrap items-end gap-3">
                <Field label="Teams meeting link" className="min-w-72 flex-1">
                  <Input
                    value={joinUrl}
                    onChange={(e) => setJoinUrl(e.target.value)}
                    placeholder="https://teams.microsoft.com/l/meetup-join/..."
                  />
                </Field>
                <Button
                  variant="primary"
                  icon={<Video className="size-4" />}
                  disabled={!joinUrl || join.busy || detail.data?.status !== 'Prepared'}
                  onClick={() => void join.run().then(refresh)}
                >
                  Start in Teams
                </Button>
              </div>
              {bridge && (
                <div className="mt-4">
                  <Alert tone={bridgeTone}>Bridge: {bridge}</Alert>
                </div>
              )}
              {live?.recording === false && (
                <div className="mt-4 grid gap-3">
                  <Alert tone={live.callState === 'Established' ? 'info' : 'warn'}>
                    Teams-native media: {live.callState ?? 'Joining'}. Audio, transcripts and
                    meeting-derived follow-ups are not saved.
                  </Alert>
                  <div className="flex flex-wrap gap-2" aria-label="Meeting participants">
                    {live.participants?.map((participant) => (
                      <Badge
                        key={participant.participantId}
                        tone={participant.guest ? 'warn' : 'ok'}
                      >
                        {participant.displayName}
                        {participant.guest ? ' (guest, no work or approval access)' : ''}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
            </Card>
            <Card
              title="Floor"
              actions={
                <Badge tone={live ? 'ok' : 'neutral'} dot>
                  {live ? 'Live' : connected ? 'Waiting' : 'Not running'}
                </Badge>
              }
            >
              {!live && (
                <p className="text-sm text-muted">Nothing is running for this session right now.</p>
              )}
              {live && (
                <div className="grid gap-5">
                  <div className="grid gap-3 sm:grid-cols-3">
                    <div>
                      <p className="text-xs text-muted">Floor</p>
                      <p className="text-lg font-semibold">{live.floor}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted">Speaker</p>
                      <p className="text-lg font-semibold">{live.currentSpeaker ?? '–'}</p>
                    </div>
                    <div>
                      <p className="text-xs text-muted">Latency p50 / p95</p>
                      <p className="text-lg font-semibold">
                        {live.recording === false ? 'Not retained' : `${live.p50} / ${live.p95} ms`}
                      </p>
                    </div>
                  </div>
                  <div className="grid gap-1.5">
                    <Progress
                      value={Math.max(0, live.remainingSec)}
                      max={(detail.data?.standupMinutes ?? 15) * 60}
                    />
                    <p className="text-sm text-muted">
                      {Math.floor(live.remainingSec / 60)}:
                      {String(live.remainingSec % 60).padStart(2, '0')} left{' '}
                      {live.agentSpeaking && '· agent speaking'}
                    </p>
                  </div>
                  {live.parkingLot.length > 0 && (
                    <Alert tone="warn">Parking lot: {live.parkingLot.join('; ')}</Alert>
                  )}
                  <div className="flex flex-wrap gap-2">
                    <Button
                      icon={<Play className="size-4" />}
                      disabled={live.running}
                      onClick={() => send('begin')}
                    >
                      Begin
                    </Button>
                    <Button icon={<SkipForward className="size-4" />} onClick={() => send('skip')}>
                      Skip speaker
                    </Button>
                    <Button icon={<MicOff className="size-4" />} onClick={() => send('mute')}>
                      Mute agent
                    </Button>
                    <Button icon={<Mic className="size-4" />} onClick={() => send('unmute')}>
                      Unmute
                    </Button>
                    <Button icon={<Hand className="size-4" />} onClick={() => send('takeover')}>
                      Take over
                    </Button>
                    <Button icon={<Undo2 className="size-4" />} onClick={() => send('release')}>
                      Hand back
                    </Button>
                    <Button
                      variant="danger"
                      icon={<Square className="size-4" />}
                      onClick={() => send('end')}
                    >
                      End standup
                    </Button>
                  </div>
                </div>
              )}
            </Card>
            {detail.data && <SessionTimeline session={detail.data} />}
          </>
        )}
      </div>
    </>
  );
}
