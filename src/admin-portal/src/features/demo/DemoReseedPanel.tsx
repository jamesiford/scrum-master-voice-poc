import { useEffect, useState } from 'react';
import {
  abortDemoReseed,
  applyFollowUp,
  approveFollowUp,
  commitDemoReseed,
  getDemoReseed,
  getFollowUp,
  stageDemoReseed,
} from '../../api/client';
import { signedInKey } from '../../auth/msal';
import { useApiAction } from '../../hooks/useApiAction';
import { useApiQuery } from '../../hooks/useApiQuery';
import { useTeamId, useTeams } from '../../team/TeamContext';
import { Badge } from '../../ui/Badge';
import { Button } from '../../ui/Button';
import { Card } from '../../ui/Card';
import { Alert } from '../../ui/Feedback';
import { Field, Input } from '../../ui/Form';
import { QueryStatus } from '../QueryStatus';

/** Admin-only staging, explicit approval and recoverable publication of an opted-in demo set. */
export function DemoReseedPanel({ onCompleted }: { onCompleted: () => void }) {
  const teamId = useTeamId();
  const { team, isAdmin, reload: reloadTeams } = useTeams();
  const key = `zava.reseed.${signedInKey()}.${teamId}`;
  const [operationId, setOperationId] = useState<string | null>(() => sessionStorage.getItem(key));
  const [resumeId, setResumeId] = useState('');
  const status = useApiQuery(
    (signal) => getDemoReseed(teamId, operationId ?? '', signal),
    isAdmin && operationId ? `${teamId}:${operationId}` : null,
  );
  const operation =
    !status.loading &&
    !status.error &&
    status.data?.operationId === operationId &&
    status.data.teamId === teamId
      ? status.data
      : undefined;
  const pending = operation?.state === 'AwaitingApprovalOrApply';
  const followUpId = pending ? operation?.followUpId : undefined;
  const proposal = useApiQuery(
    (signal) => getFollowUp(followUpId ?? '', signal),
    isAdmin ? (followUpId ?? null) : null,
  );
  const stage = useApiAction(() => stageDemoReseed(teamId));
  const approve = useApiAction((id: string) => approveFollowUp(id));
  const apply = useApiAction((id: string) => applyFollowUp(id));
  const commit = useApiAction((id: string) => commitDemoReseed(teamId, id));
  const abort = useApiAction((id: string) => abortDemoReseed(teamId, id));
  const reloadStatus = status.reload;
  const reloadProposal = proposal.reload;
  const busy = stage.busy || approve.busy || apply.busy || commit.busy || abort.busy;
  const currentProposal =
    !proposal.loading && !proposal.error && proposal.data?.id === followUpId
      ? proposal.data
      : undefined;
  const canApprove = currentProposal?.status === 'Pending' || currentProposal?.status === 'Sent';

  useEffect(() => {
    if (!isAdmin || !pending) return;
    const timer = setInterval(() => {
      reloadStatus();
      reloadProposal();
    }, 3000);
    return () => clearInterval(timer);
  }, [isAdmin, pending, reloadStatus, reloadProposal]);

  const remember = (id: string) => {
    sessionStorage.setItem(key, id);
    setOperationId(id);
  };
  const refresh = () => {
    reloadStatus();
    reloadProposal();
  };
  if (!isAdmin) return null;

  return (
    <Card
      title="Re-seed demo"
      description="Stage the configured demo set, review its exact proposal, approve the ADO baseline, then publish application state atomically. Other teams and tenants are refused."
    >
      <QueryStatus
        loading={status.loading || proposal.loading}
        error={
          stage.error ??
          approve.error ??
          apply.error ??
          commit.error ??
          abort.error ??
          status.error ??
          proposal.error
        }
      />
      {!team?.isDemo && (
        <Alert tone="warn">This team is not explicitly flagged as demo. Re-seed is refused.</Alert>
      )}
      <div className="mt-3 flex flex-wrap items-end gap-3">
        <Button
          disabled={!team?.isDemo || busy || pending || status.loading}
          onClick={() =>
            void stage.run().then((id) => {
              if (id) remember(id);
            })
          }
        >
          Stage re-seed
        </Button>
        <Field label="Resume operation id" className="min-w-64 flex-1">
          <Input value={resumeId} onChange={(event) => setResumeId(event.target.value)} />
        </Field>
        <Button disabled={!resumeId.trim() || busy} onClick={() => remember(resumeId.trim())}>
          Resume
        </Button>
      </div>
      {operation && (
        <div className="mt-4 grid gap-3 text-sm">
          <Badge tone={operation.state === 'Completed' ? 'ok' : 'warn'}>{operation.state}</Badge>
          <p className="break-all">Operation: {operation.operationId}</p>
          <p>Demo teams: {operation.teamIds.join(', ')}</p>
          <p className="text-muted">{operation.boundary}</p>
          {operation.state === 'Completed' && (
            <Alert tone="info">
              Application publication completed. Prepare a fresh session for the next take.
            </Alert>
          )}
        </div>
      )}
      {pending && currentProposal && (
        <div className="mt-4 grid gap-3">
          <p className="break-all text-sm">
            Approval follow-up: {currentProposal.id} · {currentProposal.status}
          </p>
          <p className="whitespace-pre-wrap text-sm">{currentProposal.message}</p>
          <details>
            <summary className="cursor-pointer text-sm font-semibold">
              Exact proposed changes
            </summary>
            <pre className="mt-2 max-h-80 overflow-auto rounded bg-surface p-3 text-xs">
              {JSON.stringify(currentProposal.changes, null, 2)}
            </pre>
          </details>
          {currentProposal.applyError && <Alert tone="warn">{currentProposal.applyError}</Alert>}
          <div className="flex flex-wrap gap-2">
            <Button
              variant="danger"
              disabled={busy || !canApprove}
              onClick={() => {
                if (
                  followUpId &&
                  window.confirm(
                    `Approve the displayed Azure DevOps baseline for demo teams ${operation?.teamIds.join(', ')} as your signed-in Entra identity?`,
                  )
                )
                  void approve.run(followUpId).then(refresh);
              }}
            >
              Approve displayed baseline
            </Button>
            <Button
              disabled={busy || currentProposal.status !== 'Approved'}
              onClick={() => {
                if (followUpId) void apply.run(followUpId).then(refresh);
              }}
            >
              Retry approved ADO apply
            </Button>
            <Button
              variant="primary"
              disabled={busy || currentProposal.status !== 'Applied'}
              onClick={() => {
                if (operationId)
                  void commit.run(operationId).then((id) => {
                    if (id) {
                      reloadStatus();
                      reloadTeams();
                      onCompleted();
                    }
                  });
              }}
            >
              Publish re-seed
            </Button>
            <Button
              disabled={busy || !canApprove}
              onClick={() => {
                if (operationId) void abort.run(operationId).then(refresh);
              }}
            >
              Abort unapplied proposal
            </Button>
          </div>
        </div>
      )}
    </Card>
  );
}
