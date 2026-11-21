import { TEAM_ID } from '../../api/config';
import { getEmails, getModelComparison } from '../../api/client';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Badge } from '../../ui/Badge';
import { Card } from '../../ui/Card';
import { Alert } from '../../ui/Feedback';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';

/**
 * Models page: estimated cost per standup and measured latency for the realtime models, plus the follow-up email outbox.
 * Prices are list-price estimates, not a bill.
 * @returns The page.
 */
export function ModelsPage() {
  const models = useApiQuery((signal) => getModelComparison(TEAM_ID, signal), TEAM_ID);
  const emails = useApiQuery((signal) => getEmails(signal), 'emails');
  return (
    <>
      <PageHeader
        title="Models and email"
        description="Compare realtime models on cost and latency, and see the follow-up emails sent."
      />
      <div className="grid gap-6">
        <QueryStatus
          loading={models.loading || emails.loading}
          error={models.error ?? emails.error}
        />
        <Alert tone="info">
          Cost is estimated from list prices and a standard standup length. Latency is measured only
          for the model in use.
        </Alert>
        {models.data && (
          <Card title="Realtime models">
            <table className="w-full text-left text-sm">
              <thead className="text-muted">
                <tr>
                  <th className="py-2">Model</th>
                  <th>Cost per standup</th>
                  <th>p50 / p95</th>
                  <th>Notes</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-line">
                {models.data.map((m) => (
                  <tr key={m.name}>
                    <td className="py-2 font-medium">
                      {m.name} {m.isCurrent && <Badge tone="ok">In use</Badge>}
                    </td>
                    <td>${m.costPerStandupUsd.toFixed(3)}</td>
                    <td>
                      {m.measuredP50Ms === null
                        ? 'n/a'
                        : `${m.measuredP50Ms} / ${m.measuredP95Ms} ms`}
                    </td>
                    <td className="text-muted">{m.notes}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>
        )}
        <Card title="Follow-up emails" description="Shown when Features:EmailFollowUps is on.">
          {emails.data?.length === 0 && <p className="text-sm text-muted">No emails sent.</p>}
          <ul className="divide-y divide-line text-sm">
            {emails.data?.map((e, i) => (
              <li key={i} className="py-2">
                <span className="font-medium">{e.subject}</span> to {e.to}
              </li>
            ))}
          </ul>
        </Card>
      </div>
    </>
  );
}
