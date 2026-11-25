import { Play, Plus, Video } from 'lucide-react';
import { useState } from 'react';
import { useTeamId } from '../../team/TeamContext';
import {
  getRetrospective,
  prepareRetrospective,
  startSession,
  startTeamsSession,
} from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert } from '../../ui/Feedback';
import { Field, Input } from '../../ui/Form';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';

/**
 * Retrospective page: run the retrospective by voice (prepare it, then start it in the Simulated Room or a Teams meeting), and read
 * the start-stop-continue draft built in code from the sprint and its standups, with a short model summary.
 * @returns The page.
 */
export function RetrospectivePage() {
  const teamId = useTeamId();
  const retro = useApiQuery((signal) => getRetrospective(teamId, signal), teamId);
  const [sessionId, setSessionId] = useState<string>();
  const [joinUrl, setJoinUrl] = useState('');
  const [started, setStarted] = useState<'room' | 'teams'>();
  const prepare = useApiAction(() => prepareRetrospective(teamId));
  const startRoom = useApiAction((id: string) => startSession(id));
  const startTeams = useApiAction((id: string, url: string) => startTeamsSession(id, url));
  const doc = retro.data;

  const onPrepare = async () => {
    setStarted(undefined);
    const id = await prepare.run();
    if (id) setSessionId(id);
  };

  const onStartRoom = async () => {
    const result = sessionId ? await startRoom.run(sessionId) : undefined;
    if (result) {
      setStarted('room');
      window.open(result.roomUrl, '_blank');
    }
  };

  const onStartTeams = async () => {
    if (!sessionId) return;
    await startTeams.run(sessionId, joinUrl);
    setStarted('teams');
  };

  return (
    <>
      <PageHeader
        title="Retrospective"
        description="Run it by voice, or read the start, stop and continue draft drawn from what the sprint and the standups recorded."
        actions={
          <Button
            variant="primary"
            icon={<Plus className="size-4" />}
            disabled={prepare.busy}
            onClick={() => void onPrepare()}
          >
            Prepare retro
          </Button>
        }
      />
      <div className="grid gap-6">
        <QueryStatus
          loading={retro.loading || prepare.busy || startRoom.busy || startTeams.busy}
          error={retro.error ?? prepare.error ?? startRoom.error ?? startTeams.error}
        />
        {sessionId && (
          <Card
            title="Run it by voice"
            description="The agent asks each person for what went well, what to do better and what is missing, reads the themes back, and asks for up to three actions. Approving the follow-up on the Follow-ups page creates the tasks in Azure DevOps."
          >
            <div className="flex flex-wrap items-end gap-3">
              <Button
                variant="primary"
                icon={<Play className="size-4" />}
                disabled={startRoom.busy || started !== undefined}
                onClick={() => void onStartRoom()}
              >
                Start in Simulated Room
              </Button>
              <Field label="Teams meeting link" className="min-w-72 flex-1">
                <Input
                  value={joinUrl}
                  onChange={(e) => setJoinUrl(e.target.value)}
                  placeholder="https://teams.microsoft.com/l/meetup-join/..."
                />
              </Field>
              <Button
                icon={<Video className="size-4" />}
                disabled={!joinUrl || startTeams.busy || started !== undefined}
                onClick={() => void onStartTeams()}
              >
                Start in Teams
              </Button>
            </div>
            {started && (
              <div className="mt-4">
                <Alert tone="info">
                  {started === 'room'
                    ? 'The Simulated Room opened in a new tab. Watch and steer it on the Live console.'
                    : 'The agent is joining the meeting and waits in the lobby until admitted. Watch it on the Live console.'}
                </Alert>
              </div>
            )}
          </Card>
        )}
        {doc && (
          <Card title={doc.title}>
            {!doc.narrativeFromModel && (
              <Alert tone="warn">
                The language model was unavailable, so a plain summary is shown.
              </Alert>
            )}
            <pre className="mt-3 whitespace-pre-wrap font-sans text-sm leading-relaxed">
              {doc.markdown}
            </pre>
          </Card>
        )}
      </div>
    </>
  );
}
