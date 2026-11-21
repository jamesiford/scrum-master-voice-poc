import { Badge, Card, CardHeader, Text, Title3 } from '@fluentui/react-components';
import type { BriefItem, Flag, PreBrief } from '../../api/client';

const flagColor: Record<Flag, 'danger' | 'warning' | 'important' | 'informative' | 'brand'> = {
  Blocked: 'danger',
  Overdue: 'danger',
  Stale: 'warning',
  NoRecentUpdate: 'informative',
  Unassigned: 'important',
};

const flagLabel: Record<Flag, string> = {
  Blocked: 'Blocked',
  Overdue: 'Overdue',
  Stale: 'Stale',
  NoRecentUpdate: 'No recent update',
  Unassigned: 'Unassigned',
};

function ItemLine({ item }: { item: BriefItem }) {
  return (
    <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
      <Text weight="semibold">
        {item.type} {item.id.value}
      </Text>
      <Text>
        {item.title} ({item.state}, {item.daysInState}d)
      </Text>
      {item.flags.map((f) => (
        <Badge key={f} appearance="filled" color={flagColor[f]}>
          {flagLabel[f]}
        </Badge>
      ))}
    </div>
  );
}

/**
 * Renders a pre-brief: sprint position, team risks, then each member's items and suggested question.
 * @param props - The pre-brief to show.
 * @returns The pre-brief view.
 */
export function PreBriefView({ brief }: { brief: PreBrief }) {
  const { burndown } = brief;
  return (
    <div style={{ display: 'grid', gap: 12 }}>
      <Title3>Pre-brief</Title3>
      <Text>
        <b>Goal:</b> {brief.sprintGoal || 'not set'} <b>Iteration:</b> {brief.iterationPath}
      </Text>
      <Text>
        {burndown.completedPoints} of {burndown.committedPoints} points done,{' '}
        {burndown.remainingPoints} left, {burndown.daysLeft} days to go{' '}
        <Badge appearance="filled" color={burndown.atRisk ? 'danger' : 'success'}>
          {burndown.atRisk ? 'Burndown at risk' : 'On pace'}
        </Badge>
      </Text>
      <Text>{brief.summary}</Text>
      {brief.teamRisks.length > 0 && (
        <ul aria-label="Team risks" style={{ margin: 0 }}>
          {brief.teamRisks.map((r) => (
            <li key={r}>{r}</li>
          ))}
        </ul>
      )}
      {brief.members.map((m) => (
        <Card key={m.memberId}>
          <CardHeader
            header={<Text weight="semibold">{m.displayName}</Text>}
            description={<Text italic>{m.suggestedQuestion}</Text>}
          />
          {m.items.length === 0 ? (
            <Text>No open items.</Text>
          ) : (
            m.items.map((i) => <ItemLine key={i.id.value} item={i} />)
          )}
        </Card>
      ))}
      {brief.unassignedItems.length > 0 && (
        <Card>
          <CardHeader header={<Text weight="semibold">Unassigned</Text>} />
          {brief.unassignedItems.map((i) => (
            <ItemLine key={i.id.value} item={i} />
          ))}
        </Card>
      )}
    </div>
  );
}
