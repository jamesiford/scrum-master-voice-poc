import { useState } from 'react';
import { useTeamId } from '../../team/TeamContext';
import { draftStory, getParkedTopics, type StoryDraft } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert } from '../../ui/Feedback';
import { Field, Textarea } from '../../ui/Form';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';

/**
 * Story Writer page: drafts a user story and acceptance criteria from a topic parked in standup. Draft only; nothing is
 * written to Azure DevOps from here.
 * @returns The page.
 */
export function StoriesPage() {
  const teamId = useTeamId();
  const [topic, setTopic] = useState('');
  const [result, setResult] = useState<StoryDraft>();
  const topics = useApiQuery((signal) => getParkedTopics(teamId, signal), teamId);
  const draft = useApiAction(() => draftStory(teamId, topic));

  const onDraft = async () => {
    const r = await draft.run();
    if (r) setResult(r);
  };

  return (
    <>
      <PageHeader
        title="Story Writer"
        description="Turn a parked decision into a user story with acceptance criteria. This is a draft for a person to edit."
      />
      <div className="grid gap-6 lg:grid-cols-2">
        <Card title="Topic">
          <div className="grid gap-4">
            <QueryStatus loading={topics.loading} error={topics.error ?? draft.error} />
            {topics.data && topics.data.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {topics.data.map((t) => (
                  <button key={t} onClick={() => setTopic(t)} className="text-left">
                    <Badge tone="brand">{t}</Badge>
                  </button>
                ))}
              </div>
            )}
            <Field label="What should the story cover?">
              <Textarea
                rows={4}
                value={topic}
                maxLength={500}
                onChange={(e) => setTopic(e.target.value)}
              />
            </Field>
            <Button variant="primary" disabled={draft.busy || !topic.trim()} onClick={onDraft}>
              {draft.busy ? 'Drafting...' : 'Draft story'}
            </Button>
          </div>
        </Card>
        <Card title="Draft">
          {!result && <p className="text-sm text-muted">Nothing drafted yet.</p>}
          {result && (
            <div className="grid gap-3 text-sm">
              {!result.fromModel && (
                <Alert tone="warn">
                  The language model was unavailable, so a template is shown.
                </Alert>
              )}
              <p className="text-base font-semibold">{result.title}</p>
              <p>{result.story}</p>
              <ul className="list-disc pl-5">
                {result.acceptanceCriteria.map((c) => (
                  <li key={c}>{c}</li>
                ))}
              </ul>
            </div>
          )}
        </Card>
      </div>
    </>
  );
}
