import { Tab, TabList, Title3 } from '@fluentui/react-components';
import { useState } from 'react';
import { FollowUpsPage } from './features/followups/FollowUpsPage';
import { HealthBadge } from './features/health/HealthBadge';
import { RosterPage } from './features/roster/RosterPage';
import { SessionsPage } from './features/sessions/SessionsPage';
import { TeamPage } from './features/team/TeamPage';

type PageId = 'sessions' | 'followups' | 'team' | 'roster';

/**
 * Portal shell with the M1 pages. Schedule, Prompts, Live console and Reports arrive in later milestones.
 * @returns The shell layout.
 */
export function App() {
  const [page, setPage] = useState<PageId>('sessions');
  return (
    <div>
      <header style={{ display: 'flex', gap: 16, alignItems: 'center', padding: '12px 24px' }}>
        <Title3>MCM Scrum Agent</Title3>
        <HealthBadge />
      </header>
      <nav style={{ padding: '0 24px' }}>
        <TabList selectedValue={page} onTabSelect={(_, d) => setPage(d.value as PageId)}>
          <Tab value="sessions">Sessions</Tab>
          <Tab value="followups">Follow-ups</Tab>
          <Tab value="team">Team</Tab>
          <Tab value="roster">Roster</Tab>
        </TabList>
      </nav>
      <main style={{ padding: 24 }}>
        {page === 'sessions' && <SessionsPage />}
        {page === 'followups' && <FollowUpsPage />}
        {page === 'team' && <TeamPage />}
        {page === 'roster' && <RosterPage />}
      </main>
    </div>
  );
}
