import type { MemberBrief, Session } from '../../api/client';
import { Badge, type Tone } from '../../ui/Badge';
import { Card } from '../../ui/Card';

function nameOf(brief: MemberBrief[] | undefined, memberId: string | null): string {
  return (
    brief?.find((m) => m.memberId === memberId)?.displayName ?? (memberId ? memberId : 'Agent')
  );
}

const eventTone: Record<string, Tone> = {
  Blocker: 'bad',
  ParkingLot: 'warn',
  Commitment: 'ok',
  Question: 'brand',
  Bridge: 'neutral',
};

/**
 * Shows what happened in a standup: key metrics and the event timeline (SPEC 13 Sessions).
 * @param props - The session to show.
 * @returns The timeline view, or nothing for a session that has not run.
 */
export function SessionTimeline({ session }: { session: Session }) {
  if (session.status === 'Prepared') return null;
  const members = session.preBrief?.members;
  const events = session.events.filter((e) => e.type !== 'ToolCall');
  const metric = (label: string, value: string) => (
    <div>
      <p className="text-xs text-muted">{label}</p>
      <p className="text-lg font-semibold">{value}</p>
    </div>
  );
  return (
    <Card
      title="Standup run"
      actions={
        <Badge tone={session.status === 'Live' ? 'ok' : 'neutral'} dot>
          {session.status}
        </Badge>
      }
    >
      <div className="grid gap-6">
        <div className="flex flex-wrap gap-8">
          {metric('Latency p50', `${session.p50LatencyMs || '–'} ms`)}
          {metric('Latency p95', `${session.p95LatencyMs || '–'} ms`)}
          {metric('Questions asked', String(session.questionsAsked))}
          {metric('Timebox', `${session.standupMinutes} min`)}
        </div>
        <ol aria-label="Events" className="relative grid gap-3 border-l border-line pl-5">
          {events.map((e) => (
            <li key={e.id} className="relative text-sm">
              <span
                className="absolute -left-[26px] top-1.5 size-2 rounded-full bg-line"
                aria-hidden
              />
              <div className="flex flex-wrap items-center gap-2">
                <Badge tone={eventTone[e.type] ?? 'neutral'}>{e.type}</Badge>
                <span className="font-medium">
                  {e.memberId || e.type === 'Transcript' || e.type === 'Question'
                    ? nameOf(members, e.memberId)
                    : ''}
                </span>
              </div>
              <p className="mt-1 text-muted">{e.text}</p>
            </li>
          ))}
        </ol>
      </div>
    </Card>
  );
}
