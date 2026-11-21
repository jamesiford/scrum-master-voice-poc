import {
  Badge,
  Button,
  Card,
  CardHeader,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Text,
} from '@fluentui/react-components';
import { useState } from 'react';
import { TEAM_ID } from '../../api/config';
import {
  applyFollowUp,
  approveFollowUp,
  listCards,
  listFollowUps,
  rejectFollowUp,
  type FollowUp,
  type FollowUpStatus,
} from '../../api/client';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { QueryStatus } from '../QueryStatus';

const STATUS_COLOR: Record<FollowUpStatus, 'brand' | 'success' | 'danger' | 'informative'> = {
  Pending: 'informative',
  Sent: 'brand',
  Approved: 'brand',
  Rejected: 'danger',
  Applied: 'success',
};

/**
 * Follow-ups page: approve or reject what the agent proposes. Approving is what permits the Azure DevOps write (F7).
 * @returns The page.
 */
export function FollowUpsPage() {
  const [approver, setApprover] = useState('');
  const [showCards, setShowCards] = useState(false);
  const followUps = useApiQuery((signal) => listFollowUps(TEAM_ID, signal), TEAM_ID);
  const cards = useApiQuery((signal) => listCards(signal), showCards ? 'cards' : null);
  const approve = useApiAction((id: string, name: string) => approveFollowUp(id, name));
  const reject = useApiAction((id: string, name: string) => rejectFollowUp(id, name));
  const retry = useApiAction((id: string) => applyFollowUp(id));

  const decide = async (run: () => Promise<unknown>) => {
    await run();
    followUps.reload();
  };

  return (
    <section style={{ display: 'grid', gap: 12 }}>
      <div style={{ display: 'flex', gap: 12, alignItems: 'end', flexWrap: 'wrap' }}>
        <Field label="Approving as">
          <Input
            value={approver}
            onChange={(_, d) => setApprover(d.value)}
            placeholder="Your name"
          />
        </Field>
        <Button onClick={followUps.reload}>Refresh</Button>
        <Button onClick={() => setShowCards((v) => !v)}>
          {showCards ? 'Hide card outbox' : 'Show card outbox'}
        </Button>
      </div>
      <QueryStatus
        loading={followUps.loading}
        error={followUps.error ?? approve.error ?? reject.error ?? retry.error}
      />
      {followUps.data?.length === 0 && (
        <Text>No follow-ups yet. They appear when a standup ends.</Text>
      )}
      {followUps.data?.map((f) => (
        <FollowUpCard
          key={f.id}
          followUp={f}
          canDecide={approver.trim().length > 0}
          onApprove={() => void decide(() => approve.run(f.id, approver))}
          onReject={() => void decide(() => reject.run(f.id, approver))}
          onRetry={() => void decide(() => retry.run(f.id))}
        />
      ))}
      {showCards && (
        <div style={{ display: 'grid', gap: 8 }}>
          <QueryStatus loading={cards.loading} error={cards.error} />
          {cards.data?.map((c) => (
            <Card key={c.id}>
              <CardHeader
                header={
                  <Text weight="semibold">
                    {c.message.kind}: {c.message.title}
                  </Text>
                }
              />
              <pre style={{ margin: 0, overflow: 'auto', maxHeight: 220 }}>
                {c.adaptiveCardJson}
              </pre>
            </Card>
          ))}
        </div>
      )}
    </section>
  );
}

interface FollowUpCardProps {
  followUp: FollowUp;
  canDecide: boolean;
  onApprove: () => void;
  onReject: () => void;
  onRetry: () => void;
}

/**
 * One follow-up with its proposed changes and the actions its status allows.
 * @param props - The follow-up and action callbacks.
 * @returns The card.
 */
function FollowUpCard({ followUp: f, canDecide, onApprove, onReject, onRetry }: FollowUpCardProps) {
  const undecided = f.status === 'Pending' || f.status === 'Sent';
  return (
    <Card>
      <CardHeader
        header={
          <Text weight="semibold">
            {f.kind} {f.memberId ? `for ${f.memberId}` : ''}
          </Text>
        }
        action={<Badge color={STATUS_COLOR[f.status]}>{f.status}</Badge>}
      />
      <Text style={{ whiteSpace: 'pre-wrap' }}>{f.message}</Text>
      {f.changes.map((c) => (
        <Text key={`${c.workItemId}-${c.value}`} size={200}>
          Work item {c.workItemId} ({c.op}): {c.value}
        </Text>
      ))}
      {f.approvedBy && <Text size={200}>Approved by {f.approvedBy}</Text>}
      {f.rejectedBy && <Text size={200}>Rejected by {f.rejectedBy}</Text>}
      {f.applyError && (
        <MessageBar intent="error">
          <MessageBarBody>Azure DevOps write failed: {f.applyError}</MessageBarBody>
        </MessageBar>
      )}
      <div style={{ display: 'flex', gap: 8 }}>
        {undecided && f.kind === 'Followup' && (
          <>
            <Button appearance="primary" disabled={!canDecide} onClick={onApprove}>
              Approve
            </Button>
            <Button disabled={!canDecide} onClick={onReject}>
              Reject
            </Button>
          </>
        )}
        {f.status === 'Approved' && <Button onClick={onRetry}>Retry write</Button>}
      </div>
    </Card>
  );
}
