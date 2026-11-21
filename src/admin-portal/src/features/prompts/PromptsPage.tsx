import { Button, Dropdown, Field, Option, Text, Textarea } from '@fluentui/react-components';
import { useEffect, useState } from 'react';
import { TEAM_ID } from '../../api/config';
import { getPrompt, listPrompts, savePrompt } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
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
    <section style={{ display: 'grid', gap: 12 }}>
      <QueryStatus
        loading={names.loading || prompt.loading}
        error={names.error ?? prompt.error ?? save.error}
      />
      <Field label="Prompt">
        <Dropdown
          placeholder="Choose a prompt"
          value={name ?? ''}
          onOptionSelect={(_, d) => setName(d.optionValue ?? null)}
        >
          {(names.data ?? []).map((n) => (
            <Option key={n} value={n}>
              {n}
            </Option>
          ))}
        </Dropdown>
      </Field>
      {name && (
        <>
          <Textarea
            value={text}
            rows={22}
            resize="vertical"
            onChange={(_, d) => setText(d.value)}
            style={{ fontFamily: 'Consolas, monospace' }}
          />
          <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
            <Button
              appearance="primary"
              disabled={save.busy || !text.trim()}
              onClick={() => void onSave()}
            >
              Save prompt
            </Button>
            {saved && !save.error && <Text>Saved. Applies on the next agent turn.</Text>}
          </div>
        </>
      )}
    </section>
  );
}
