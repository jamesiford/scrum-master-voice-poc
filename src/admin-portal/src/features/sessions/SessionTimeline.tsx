import { Badge, Text, Title3 } from '@fluentui/react-components';
import type { MemberBrief, Session } from '../../api/client';

function nameOf(brief: MemberBrief[] | undefined, memberId: string | null): string {
  return (
    brief?.find((m) => m.memberId === memberId)?.displayName ?? (memberId ? memberId : 'Agent')
  );
}

/**
 * Shows what happened in a standup: key metrics and the event timeline (SPEC 13 Sessions).
 * @param props - The session to show.
 * @returns The timeline view, or nothing for a session that has not run.
 */
export function SessionTimeline({ session }: { session: Session }) {
  if (session.status === 'Prepared') return null;
  const members = session.preBrief?.members;
  const events = session.events.filter((e) => e.type !== 'ToolCall');
  return (
    <div style={{ display: 'grid', gap: 8 }}>
      <Title3>Standup run</Title3>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
        <Badge appearance="filled">{session.status}</Badge>
        <Text>
          Latency p50 <b>{session.p50LatencyMs || '--'} ms</b>, p95{' '}
          <b>{session.p95LatencyMs || '--'} ms</b>
        </Text>
        <Text>
          Questions asked <b>{session.questionsAsked}</b>
        </Text>
        <Text>
          Timebox <b>{session.standupMinutes} min</b>
        </Text>
      </div>
      <ol aria-label="Events" style={{ margin: 0, paddingLeft: 20, display: 'grid', gap: 4 }}>
        {events.map((e) => (
          <li key={e.id}>
            <Badge
              appearance="tint"
              size="small"
              color={
                e.type === 'Blocker'
                  ? 'danger'
                  : e.type === 'ParkingLot'
                    ? 'warning'
                    : 'informative'
              }
            >
              {e.type}
            </Badge>{' '}
            <b>
              {e.type === 'Transcript' || e.type === 'Question'
                ? nameOf(members, e.memberId)
                : e.memberId
                  ? nameOf(members, e.memberId)
                  : ''}
            </b>{' '}
            {e.text}
          </li>
        ))}
      </ol>
    </div>
  );
}
