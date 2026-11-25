import { Check, X, RotateCw, RefreshCw } from 'lucide-react';
import { useState } from 'react';
import { TEAM_ID } from '../../api/config';
import {
  applyFollowUp,
  getRoster,
  approveFollowUp,
  listCards,
  listFollowUps,
  rejectFollowUp,
  type FollowUp,
  type FollowUpStatus,
} from '../../api/client';
import { SignedInAs } from '../../auth/SignedInAs';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { Badge, type Tone } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert, EmptyState } from '../../ui/Feedback';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';

const statusTone: Record<FollowUpStatus, Tone> = {
  Pending: 'neutral',
  Sent: 'brand',
  Approved: 'warn',
  Rejected: 'bad',
  Applied: 'ok',
};

/**
 * Follow-ups page: approve or reject what the agent proposes. Approving is what permits the Azure DevOps write (F7).
 * @returns The page.
 */
export function FollowUpsPage() {
  const [showCards, setShowCards] = useState(false);
  const followUps = useApiQuery((signal) => listFollowUps(TEAM_ID, signal), TEAM_ID);
  const roster = useApiQuery((signal) => getRoster(TEAM_ID, signal), TEAM_ID);
  const nameOf = (id: string | null) =>
    id ? (roster.data?.find((m) => m.id === id)?.displayName ?? id) : 'Team';
  const cards = useApiQuery((signal) => listCards(signal), showCards ? 'cards' : null);
  const approve = useApiAction((id: string) => approveFollowUp(id));
  const reject = useApiAction((id: string) => rejectFollowUp(id));
  const retry = useApiAction((id: string) => applyFollowUp(id));

  const decide = async (run: () => Promise<unknown>) => {
    await run();
    followUps.reload();
  };
  const list = followUps.data ?? [];
  const needsDecision = list.filter(
    (f) => f.kind === 'Followup' && (f.status === 'Pending' || f.status === 'Sent'),
  ).length;

  return (
    <>
      <PageHeader
        title="Follow-ups"
        description="After each standup the agent proposes changes to Azure DevOps. Nothing is written until a named person approves."
        actions={
          <Button
            variant="ghost"
            icon={<RefreshCw className="size-4" />}
            aria-label="Refresh"
            onClick={followUps.reload}
          />
        }
      />
      <div className="grid gap-6">
        <Card>
          <div className="flex flex-wrap items-end justify-between gap-4">
            <SignedInAs />
            {needsDecision > 0 && (
              <Badge tone="warn" dot>
                {needsDecision} waiting for a decision
              </Badge>
            )}
          </div>
        </Card>
        <QueryStatus
          loading={followUps.loading}
          error={followUps.error ?? approve.error ?? reject.error ?? retry.error}
        />
        {list.length === 0 && !followUps.loading && (
          <EmptyState title="No follow-ups yet" hint="They appear here when a standup ends." />
        )}
        <div className="grid gap-4">
          {list.map((f) => (
            <FollowUpCard
              key={f.id}
              followUp={f}
              who={nameOf(f.memberId)}
              onApprove={() => void decide(() => approve.run(f.id))}
              onReject={() => void decide(() => reject.run(f.id))}
              onRetry={() => void decide(() => retry.run(f.id))}
            />
          ))}
        </div>
        <div>
          <Button variant="secondary" onClick={() => setShowCards((v) => !v)}>
            {showCards ? 'Hide card outbox' : 'Show card outbox'}
          </Button>
        </div>
        {showCards && (
          <div className="grid gap-3">
            <QueryStatus loading={cards.loading} error={cards.error} />
            {cards.data?.map((c) => (
              <Card
                key={c.id}
                title={`${c.message.kind}: ${c.message.title}`}
                description="Adaptive Card JSON, as sent to Teams"
              >
                <pre className="max-h-56 overflow-auto rounded-lg bg-subtle p-3 text-xs">
                  {c.adaptiveCardJson}
                </pre>
              </Card>
            ))}
          </div>
        )}
      </div>
    </>
  );
}

interface FollowUpCardProps {
  followUp: FollowUp;
  who: string;
  onApprove: () => void;
  onReject: () => void;
  onRetry: () => void;
}

/** Card heading per follow-up kind. */
const kindTitle: Record<FollowUp['kind'], string> = {
  Followup: 'Follow-up',
  Absent: 'Missed standup',
  Escalation: 'Escalation',
  Unblock: 'Unblock request',
  PlanningPack: 'Sprint planning pack',
  RetroActions: 'Retrospective actions',
  ReviewPack: 'Sprint review pack',
};

function FollowUpCard({ followUp: f, who, onApprove, onReject, onRetry }: FollowUpCardProps) {
  const undecided = f.status === 'Pending' || f.status === 'Sent';
  return (
    <Card
      title={`${kindTitle[f.kind]} · ${who}`}
      actions={
        <Badge tone={statusTone[f.status]} dot>
          {f.status}
        </Badge>
      }
    >
      <div className="grid gap-4">
        <p className="whitespace-pre-wrap text-sm">{f.message}</p>
        {f.changes.length > 0 && (
          <ul className="grid gap-2">
            {f.changes.map((c) => (
              <li
                key={`${c.workItemId}-${c.value}`}
                className="rounded-lg bg-subtle px-3 py-2 text-sm"
              >
                <span className="font-medium">
                  {c.op === 'CreateWorkItem'
                    ? 'New work item'
                    : `Work item ${c.workItemId} · ${c.op}`}
                </span>
                <span className="text-muted"> {c.value}</span>
              </li>
            ))}
          </ul>
        )}
        {f.approvedBy && <p className="text-xs text-muted">Approved by {f.approvedBy}</p>}
        {f.rejectedBy && <p className="text-xs text-muted">Rejected by {f.rejectedBy}</p>}
        <p className="text-xs text-muted">Correlation: zsa-{f.correlationId.slice(0, 12)}</p>
        {f.applyError && <Alert tone="bad">Azure DevOps write failed: {f.applyError}</Alert>}
        <div className="flex flex-wrap gap-2">
          {undecided && f.changes.length > 0 && (
            <>
              <Button variant="primary" icon={<Check className="size-4" />} onClick={onApprove}>
                Approve and write
              </Button>
              <Button icon={<X className="size-4" />} onClick={onReject}>
                Reject
              </Button>
            </>
          )}
          {f.status === 'Approved' && (
            <Button icon={<RotateCw className="size-4" />} onClick={onRetry}>
              Retry write
            </Button>
          )}
        </div>
      </div>
    </Card>
  );
}
