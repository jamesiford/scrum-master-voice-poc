import { Text, Title3 } from '@fluentui/react-components';
import { TEAM_ID } from '../../api/config';
import { getPlanning } from '../../api/client';
import { useApiQuery } from '../../hooks/useApiQuery';
import { QueryStatus } from '../QueryStatus';

/**
 * Planning page: load per member, what would carry over, items that fail the Definition of Ready, and suggested owners for
 * unassigned items. Suggestions are advice only; assigning needs an approved follow-up.
 * @returns The page.
 */
export function PlanningPage() {
  const prep = useApiQuery((signal) => getPlanning(TEAM_ID, signal), TEAM_ID);
  const p = prep.data;
  return (
    <section style={{ display: 'grid', gap: 12 }}>
      <QueryStatus loading={prep.loading} error={prep.error} />
      {p && (
        <>
          <Title3>Load per person</Title3>
          {p.loads.map((l) => (
            <Text key={l.memberId}>
              {l.displayName}: {l.openItems} open items, {l.openPoints} points, {l.remainingHours}{' '}
              hours
            </Text>
          ))}
          <Title3>Would carry over ({p.carryOver.length})</Title3>
          {p.carryOver.map((c) => (
            <Text key={c.workItemId}>
              {c.workItemId} {c.title} ({c.state}, {c.points ?? 'no'} points,{' '}
              {c.ownerName ?? 'unowned'})
            </Text>
          ))}
          <Title3>Not ready ({p.notReady.length})</Title3>
          {p.notReady.map((n) => (
            <Text key={n.workItemId}>
              {n.workItemId} {n.title}: {n.reasons.join(', ')}
            </Text>
          ))}
          <Title3>Suggested owners</Title3>
          {p.suggestions.length === 0 && <Text>Nothing is unassigned.</Text>}
          {p.suggestions.map((s) => (
            <Text key={s.workItemId}>
              {s.workItemId} {s.title} to {s.displayName}. {s.reason}
            </Text>
          ))}
        </>
      )}
    </section>
  );
}
