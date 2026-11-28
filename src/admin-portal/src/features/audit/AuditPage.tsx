import { useState } from 'react';
import { getAuditChain } from '../../api/client';
import { useApiQuery } from '../../hooks/useApiQuery';
import { useTeamId } from '../../team/TeamContext';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert } from '../../ui/Feedback';
import { Field, Input } from '../../ui/Form';
import { PageHeader } from '../../ui/PageHeader';
import { QueryStatus } from '../QueryStatus';

/** Explains a stored meeting-to-revision chain without inventing missing evidence. */
export function AuditPage() {
  const teamId = useTeamId();
  const [input, setInput] = useState('');
  const [reference, setReference] = useState<string | null>(null);
  const query = useApiQuery(
    (signal) => getAuditChain(reference ?? '', signal),
    reference ? `${teamId}:${reference}` : null,
  );
  const chain =
    !query.loading && !query.error && query.data?.teamId === teamId ? query.data : undefined;

  return (
    <>
      <PageHeader
        title="Audit chain"
        description="Follow a correlation id from the meeting and turns through proposals, named decisions and Azure DevOps revisions."
      />
      <div className="grid gap-6">
        <Card>
          <form
            className="flex flex-wrap items-end gap-3"
            onSubmit={(event) => {
              event.preventDefault();
              if (reference === input.trim()) query.reload();
              else setReference(input.trim());
            }}
          >
            <Field label="Correlation id or zsa tag" className="min-w-64 flex-1">
              <Input
                value={input}
                onChange={(event) => setInput(event.target.value)}
                placeholder="32-character trace id or zsa-..."
              />
            </Field>
            <Button type="submit" disabled={!input.trim() || query.loading}>
              Read chain
            </Button>
          </form>
        </Card>
        <QueryStatus loading={query.loading} error={query.error} />
        {!query.loading && !query.error && query.data && !chain && (
          <Alert tone="warn">This chain belongs to another team. Select its team to view it.</Alert>
        )}
        {chain && (
          <>
            <Card
              title={chain.tag}
              actions={
                <Badge
                  tone={chain.records.length === 0 ? 'neutral' : chain.complete ? 'ok' : 'warn'}
                >
                  {chain.records.length === 0
                    ? 'No decisions yet'
                    : chain.complete
                      ? 'Decisions linked'
                      : 'Missing evidence'}
                </Badge>
              }
            >
              <p className="break-all font-mono text-sm">{chain.correlationId}</p>
              <p className="mt-2 text-sm text-muted">
                Team: {chain.teamId}. {chain.approvedCount} approved proposals.
              </p>
              {chain.missing.length > 0 && <Alert tone="warn">{chain.missing.join('; ')}</Alert>}
            </Card>
            <Card title="Meeting and session">
              {chain.meeting ? (
                <p className="break-all text-sm">
                  Meeting {chain.meeting.meetingId} · conversation {chain.meeting.conversationId}
                </p>
              ) : (
                <p className="text-sm text-muted">
                  No Teams lifecycle identity recorded
                  {chain.session?.mode === 'simulated' ? ' (Simulated Room)' : ''}.
                </p>
              )}
              {chain.joinUrl && <p className="mt-2 break-all text-sm">{chain.joinUrl}</p>}
              {chain.session ? (
                <p className="mt-2 break-all text-sm">
                  Session {chain.session.id} · {chain.session.kind} ·{' '}
                  {chain.session.mode ?? 'Not started'} ·{' '}
                  {new Date(chain.session.createdAt).toLocaleString()}
                </p>
              ) : (
                <p className="text-sm text-muted">
                  No session; this chain may originate from a planning pack.
                </p>
              )}
            </Card>
            <Card title="Turns">
              {chain.turns.length === 0 && <p className="text-sm text-muted">No recorded turns.</p>}
              <ol className="grid gap-2 text-sm">
                {chain.turns.map((turn, index) => (
                  <li key={index}>
                    {new Date(turn.at).toLocaleTimeString()} · {turn.type} ·{' '}
                    {turn.memberId ?? 'Facilitator'}
                    {turn.workItemIds.length > 0 ? ` · items ${turn.workItemIds.join(', ')}` : ''}
                  </li>
                ))}
              </ol>
            </Card>
            <Card title="Proposals">
              {chain.followUps.length === 0 && (
                <p className="text-sm text-muted">No proposals recorded.</p>
              )}
              <div className="grid gap-5">
                {chain.followUps.map((proposal) => (
                  <div key={proposal.id} className="border-b border-line pb-4 text-sm">
                    <p className="break-all font-semibold">
                      {proposal.kind} · {proposal.status} · {proposal.id}
                    </p>
                    <p>
                      {proposal.changes} proposed changes · {proposal.memberId ?? 'Team'}
                    </p>
                    <p className="break-all font-mono text-xs text-muted">
                      SHA-256: {proposal.proposalHash}
                    </p>
                  </div>
                ))}
              </div>
            </Card>
            <Card
              title="Decisions and Azure DevOps revisions"
              description="Immutable records remain visible even when a demo re-seed has cleared the session or proposal."
            >
              {chain.records.length === 0 && (
                <p className="text-sm text-muted">No decision or write records.</p>
              )}
              <div className="grid gap-4 text-sm">
                {chain.records.map((record) => (
                  <div key={record.id}>
                    <p>
                      {record.kind} by {record.actor.displayName} via {record.channel} ·{' '}
                      {new Date(record.occurredAt).toLocaleString()}
                    </p>
                    <p className="break-all text-xs text-muted">
                      Record: {record.id} · proposal: {record.followUpId}
                    </p>
                    <p className="break-all text-xs text-muted">
                      Entra: {record.actor.objectId} · tenant {record.actor.tenantId}
                    </p>
                    <p className="break-all font-mono text-xs text-muted">
                      SHA-256: {record.proposalHash}
                    </p>
                    {record.decisionRecordId && (
                      <p className="break-all text-xs text-muted">
                        Approval record: {record.decisionRecordId}
                      </p>
                    )}
                    {record.adoRevisions.map((revision) => (
                      <p
                        key={`${revision.workItemId}:${revision.revision}`}
                        className="font-semibold"
                      >
                        Azure DevOps item #{revision.workItemId} · revision {revision.revision}
                      </p>
                    ))}
                  </div>
                ))}
              </div>
            </Card>
            <Card
              title="Card outbox evidence"
              description="Stored send records, not proof that a person received or read a Teams message."
            >
              {chain.cards.length === 0 ? (
                <p className="text-sm text-muted">No cards recorded in the outbox.</p>
              ) : (
                chain.cards.map((card) => (
                  <p key={card.id} className="text-sm">
                    {card.kind} · {new Date(card.sentAt).toLocaleString()} ·{' '}
                    {card.followUpId ?? 'Session card'}
                  </p>
                ))
              )}
            </Card>
          </>
        )}
      </div>
    </>
  );
}
