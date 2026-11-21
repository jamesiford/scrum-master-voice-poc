import {
  Badge,
  Button,
  Field,
  Input,
  ProgressBar,
  Tab,
  TabList,
  Text,
  Title3,
} from '@fluentui/react-components';
import { useState } from 'react';
import { TEAM_ID } from '../../api/config';
import { askAgent, getKpis, getReport } from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { QueryStatus } from '../QueryStatus';

/**
 * Reports page: KPI bars, the sprint summary and monthly report, and a box to ask the Reporting agent a question.
 * Fixture-backed data is labelled "illustrative" (SPEC 9.5).
 * @returns The page.
 */
export function ReportsPage() {
  const [kind, setKind] = useState<'sprint' | 'monthly'>('sprint');
  const [question, setQuestion] = useState('');
  const kpis = useApiQuery((signal) => getKpis(TEAM_ID, 6, signal), TEAM_ID);
  const report = useApiQuery((signal) => getReport(kind, TEAM_ID, 6, signal), kind);
  const ask = useApiAction(() => askAgent(TEAM_ID, question));
  const [answer, setAnswer] = useState('');

  const onAsk = async () => {
    const text = await ask.run();
    if (text) setAnswer(text);
  };

  return (
    <section style={{ display: 'grid', gap: 12 }}>
      <QueryStatus
        loading={kpis.loading || report.loading}
        error={kpis.error ?? report.error ?? ask.error}
      />
      <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
        <Title3>Sprint trend</Title3>
        {kpis.data?.illustrative && <Badge color="warning">Illustrative data</Badge>}
      </div>
      <div style={{ display: 'grid', gap: 6, maxWidth: 640 }}>
        {kpis.data?.sprints.map((s) => (
          <div key={s.name}>
            <Text size={200}>
              {s.name}: {s.completedPoints} of {s.committedPoints} points, {s.carryOverItems}{' '}
              carried over
            </Text>
            <ProgressBar value={s.completedPoints} max={Math.max(s.committedPoints, 1)} />
          </div>
        ))}
      </div>
      <TabList
        selectedValue={kind}
        onTabSelect={(_, d) => setKind(d.value as 'sprint' | 'monthly')}
      >
        <Tab value="sprint">Sprint summary</Tab>
        <Tab value="monthly">Monthly report</Tab>
      </TabList>
      {report.data && (
        <>
          {!report.data.narrativeFromModel && (
            <Text size={200}>The language model was unavailable, so a plain summary is shown.</Text>
          )}
          <pre style={{ whiteSpace: 'pre-wrap', margin: 0, fontFamily: 'inherit' }}>
            {report.data.markdown}
          </pre>
        </>
      )}
      <Title3>Ask the agent</Title3>
      <div style={{ display: 'flex', gap: 8, alignItems: 'end' }}>
        <Field label="Question" style={{ flex: 1 }}>
          <Input value={question} onChange={(_, d) => setQuestion(d.value)} />
        </Field>
        <Button
          appearance="primary"
          disabled={!question.trim() || ask.busy}
          onClick={() => void onAsk()}
        >
          Ask
        </Button>
      </div>
      {answer && <Text style={{ whiteSpace: 'pre-wrap' }}>{answer}</Text>}
    </section>
  );
}
