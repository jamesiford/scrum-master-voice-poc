import { TEAM_ID } from '../../api/config';
import { getRetrospective } from '../../api/client';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Card } from '../../ui/Card';
import { Alert } from '../../ui/Feedback';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';

/**
 * Retrospective page: a start-stop-continue draft built in code from the sprint and its standups, with a short model summary.
 * @returns The page.
 */
export function RetrospectivePage() {
  const retro = useApiQuery((signal) => getRetrospective(TEAM_ID, signal), TEAM_ID);
  const doc = retro.data;
  return (
    <>
      <PageHeader
        title="Retrospective"
        description="Start, stop and continue, drawn from what the sprint and the standups recorded."
      />
      <div className="grid gap-6">
        <QueryStatus loading={retro.loading} error={retro.error} />
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
