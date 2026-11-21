import { useEffect, useState } from 'react';
import { TEAM_ID } from '../../api/config';
import { getPrompt, listPrompts, savePrompt } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Alert, EmptyState } from '../../ui/Feedback';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { PageHeader } from '../../ui/PageHeader';
import { Textarea } from '../../ui/Form';
import { cx } from '../../ui/cx';
import { QueryStatus } from '../QueryStatus';

/**
 * Prompts page: tune the agent's instructions during a demo. Saved prompts apply on the next turn, with no rebuild (rule 5).
 * @returns The page.
 */
export function PromptsPage() {
  const names = useApiQuery((signal) => listPrompts(TEAM_ID, signal), TEAM_ID);
  const [name, setName] = useState<string | null>(null);
  const prompt = useApiQuery((signal) => getPrompt(TEAM_ID, name ?? '', signal), name);
  const [text, setText] = useState('');
  const [saved, setSaved] = useState(false);
  const save = useApiAction(() => savePrompt(TEAM_ID, name ?? '', text));

  useEffect(() => {
    setText(prompt.data ?? '');
    setSaved(false);
  }, [prompt.data]);

  const onSave = async () => {
    await save.run();
    setSaved(true);
  };

  return (
    <>
      <PageHeader
        title="Prompts"
        description="The agent's instructions live in plain files. Edit one and it applies on the next agent turn, with no rebuild."
      />
      <QueryStatus
        loading={names.loading || prompt.loading}
        error={names.error ?? prompt.error ?? save.error}
      />
      <div className="mt-4 grid gap-6 lg:grid-cols-[16rem_1fr]">
        <nav
          aria-label="Prompts"
          className="grid content-start gap-1 rounded-xl border border-line bg-surface p-2 shadow-card"
        >
          {(names.data ?? []).map((n) => (
            <button
              key={n}
              onClick={() => setName(n)}
              aria-current={name === n}
              className={cx(
                'rounded-lg px-3 py-2 text-left text-sm font-medium transition',
                name === n
                  ? 'bg-brand-soft text-brand'
                  : 'text-muted hover:bg-subtle hover:text-ink',
              )}
            >
              {n}
            </button>
          ))}
        </nav>
        {!name && (
          <EmptyState title="Choose a prompt" hint="Pick one on the left to read and edit it." />
        )}
        {name && (
          <Card
            title={name}
            actions={
              <Button
                variant="primary"
                disabled={save.busy || !text.trim()}
                onClick={() => void onSave()}
              >
                Save prompt
              </Button>
            }
          >
            <div className="grid gap-3">
              <Textarea
                value={text}
                rows={22}
                onChange={(e) => setText(e.target.value)}
                className="font-mono text-xs leading-relaxed"
                aria-label="Prompt text"
              />
              {saved && !save.error && (
                <Alert tone="ok">Saved. Applies on the next agent turn.</Alert>
              )}
            </div>
          </Card>
        )}
      </div>
    </>
  );
}
