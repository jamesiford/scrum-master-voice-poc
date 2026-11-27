import {
  BarChart3,
  BookOpenText,
  Cpu,
  Building2,
  Network,
  UserRoundPlus,
  RotateCcw,
  CalendarClock,
  CheckSquare,
  ClipboardList,
  LayoutDashboard,
  Menu,
  MessageSquareText,
  Radio,
  Settings2,
  Users,
  X,
  Mic,
  type LucideIcon,
} from 'lucide-react';
import { useState } from 'react';
import { HealthBadge } from './features/health/HealthBadge';
import { FollowUpsPage } from './features/followups/FollowUpsPage';
import { AuditPage } from './features/audit/AuditPage';
import { LivePage } from './features/live/LivePage';
import { OverviewPage } from './features/overview/OverviewPage';
import { ModelsPage } from './features/models/ModelsPage';
import { RetrospectivePage } from './features/retrospective/RetrospectivePage';
import { StoriesPage } from './features/stories/StoriesPage';
import { PlanningPage } from './features/planning/PlanningPage';
import { PromptsPage } from './features/prompts/PromptsPage';
import { ReportsPage } from './features/reports/ReportsPage';
import { RosterPage } from './features/roster/RosterPage';
import { SchedulePage } from './features/schedule/SchedulePage';
import { SessionsPage } from './features/sessions/SessionsPage';
import { TeamPage } from './features/team/TeamPage';
import { FleetPage } from './features/admin/FleetPage';
import { OnboardingWizard } from './features/admin/OnboardingWizard';
import { TeamsAdminPage } from './features/admin/TeamsAdminPage';
import { TeamSwitcher } from './team/TeamSwitcher';
import { useTeams } from './team/TeamContext';
import { EmptyState } from './ui/Feedback';
import { useRoute, type PageId } from './hooks/useRoute';
import { cx } from './ui/cx';

interface NavItem {
  id: PageId;
  label: string;
  icon: LucideIcon;
}

/** Admin-only pages. The API enforces the role; this only decides what is shown. */
const adminGroup: { title: string; items: NavItem[] } = {
  title: 'Administration',
  items: [
    { id: 'fleet', label: 'Fleet', icon: Network },
    { id: 'teams', label: 'Teams', icon: Building2 },
    { id: 'onboarding', label: 'Onboard a team', icon: UserRoundPlus },
  ],
};

const adminPages: PageId[] = ['fleet', 'teams', 'onboarding'];

/** Navigation follows the work: run a standup, act on the result, learn from it, then configure. */
const groups: { title: string; items: NavItem[] }[] = [
  { title: '', items: [{ id: 'overview', label: 'Overview', icon: LayoutDashboard }] },
  {
    title: 'Standups',
    items: [
      { id: 'sessions', label: 'Sessions', icon: ClipboardList },
      { id: 'live', label: 'Live console', icon: Radio },
      { id: 'followups', label: 'Follow-ups', icon: CheckSquare },
      { id: 'audit', label: 'Audit chain', icon: Network },
    ],
  },
  {
    title: 'Insights',
    items: [
      { id: 'reports', label: 'Reports', icon: BarChart3 },
      { id: 'planning', label: 'Planning', icon: MessageSquareText },
      { id: 'retrospective', label: 'Retrospective', icon: RotateCcw },
      { id: 'stories', label: 'Story Writer', icon: BookOpenText },
      { id: 'models', label: 'Models and email', icon: Cpu },
    ],
  },
  {
    title: 'Configure',
    items: [
      { id: 'schedule', label: 'Schedule', icon: CalendarClock },
      { id: 'prompts', label: 'Prompts', icon: Settings2 },
      { id: 'team', label: 'Team', icon: Users },
      { id: 'roster', label: 'Roster', icon: Users },
    ],
  },
];

function Page({ page, go }: { page: PageId; go: (p: PageId) => void }) {
  const { team, isAdmin } = useTeams();
  if (adminPages.includes(page)) {
    if (!isAdmin) {
      return (
        <EmptyState title="Administrators only" hint="Ask an administrator to open this page." />
      );
    }
    return page === 'fleet' ? (
      <FleetPage />
    ) : page === 'teams' ? (
      <TeamsAdminPage />
    ) : (
      <OnboardingWizard />
    );
  }
  if (!team) {
    return (
      <EmptyState
        title="You are not in a team yet"
        hint="Ask an administrator or a team lead to add you to a team."
      />
    );
  }
  switch (page) {
    case 'overview':
      return <OverviewPage go={go} />;
    case 'sessions':
      return <SessionsPage />;
    case 'live':
      return <LivePage key={team.id} />;
    case 'followups':
      return <FollowUpsPage />;
    case 'audit':
      return <AuditPage key={team.id} />;
    case 'reports':
      return <ReportsPage />;
    case 'planning':
      return <PlanningPage />;
    case 'retrospective':
      return <RetrospectivePage />;
    case 'stories':
      return <StoriesPage />;
    case 'models':
      return <ModelsPage />;
    case 'schedule':
      return <SchedulePage />;
    case 'prompts':
      return <PromptsPage />;
    case 'team':
      return <TeamPage />;
    case 'roster':
      return <RosterPage />;
    default:
      return null;
  }
}

function Nav({ page, go, onPick }: { page: PageId; go: (p: PageId) => void; onPick: () => void }) {
  const { isAdmin } = useTeams();
  const shown = isAdmin ? [...groups, adminGroup] : groups;
  return (
    <nav aria-label="Main" className="flex flex-col gap-6 px-3 py-4">
      {shown.map((g) => (
        <div key={g.title || 'top'} className="grid gap-1">
          {g.title && (
            <p className="px-3 pb-1 text-xs font-medium uppercase tracking-wide text-muted">
              {g.title}
            </p>
          )}
          {g.items.map(({ id, label, icon: Icon }) => (
            <a
              key={id}
              href={`#/${id}`}
              aria-current={page === id ? 'page' : undefined}
              onClick={() => {
                go(id);
                onPick();
              }}
              className={cx(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition',
                page === id
                  ? 'bg-brand-soft text-brand'
                  : 'text-muted hover:bg-subtle hover:text-ink',
              )}
            >
              <Icon className="size-4" aria-hidden />
              {label}
            </a>
          ))}
        </div>
      ))}
    </nav>
  );
}

/**
 * Portal shell: a persistent sidebar grouped by task, a slim top bar with status, and the current page.
 * @returns The shell layout.
 */
export function App() {
  const { page, go } = useRoute();
  const [open, setOpen] = useState(false);
  return (
    <div className="flex min-h-full">
      <aside
        className={cx(
          'fixed inset-y-0 left-0 z-30 w-64 overflow-y-auto border-r border-line bg-surface transition-transform lg:static lg:translate-x-0',
          open ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <div className="flex h-16 items-center gap-3 border-b border-line px-5">
          <span className="grid size-8 place-items-center rounded-lg bg-brand text-brand-ink">
            <Mic className="size-4" aria-hidden />
          </span>
          <div className="leading-tight">
            <p className="text-sm font-semibold">Scrum Agent</p>
            <TeamSwitcher />
          </div>
        </div>
        <Nav page={page} go={go} onPick={() => setOpen(false)} />
      </aside>
      {open && (
        <button
          aria-label="Close menu"
          className="fixed inset-0 z-20 bg-black/40 lg:hidden"
          onClick={() => setOpen(false)}
        />
      )}
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-10 flex h-16 items-center justify-between border-b border-line bg-surface/80 px-4 backdrop-blur lg:px-8">
          <button
            aria-label="Open menu"
            className="rounded-lg p-2 text-muted hover:bg-subtle lg:hidden"
            onClick={() => setOpen((v) => !v)}
          >
            {open ? <X className="size-5" /> : <Menu className="size-5" />}
          </button>
          <div className="ml-auto">
            <HealthBadge />
          </div>
        </header>
        <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 lg:px-8">
          <Page page={page} go={go} />
        </main>
      </div>
    </div>
  );
}
