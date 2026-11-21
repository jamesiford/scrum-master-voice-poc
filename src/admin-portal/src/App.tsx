import {
  BarChart3,
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
import { LivePage } from './features/live/LivePage';
import { OverviewPage } from './features/overview/OverviewPage';
import { PlanningPage } from './features/planning/PlanningPage';
import { PromptsPage } from './features/prompts/PromptsPage';
import { ReportsPage } from './features/reports/ReportsPage';
import { RosterPage } from './features/roster/RosterPage';
import { SchedulePage } from './features/schedule/SchedulePage';
import { SessionsPage } from './features/sessions/SessionsPage';
import { TeamPage } from './features/team/TeamPage';
import { useRoute, type PageId } from './hooks/useRoute';
import { cx } from './ui/cx';

interface NavItem {
  id: PageId;
  label: string;
  icon: LucideIcon;
}

/** Navigation follows the work: run a standup, act on the result, learn from it, then configure. */
const groups: { title: string; items: NavItem[] }[] = [
  { title: '', items: [{ id: 'overview', label: 'Overview', icon: LayoutDashboard }] },
  {
    title: 'Standups',
    items: [
      { id: 'sessions', label: 'Sessions', icon: ClipboardList },
      { id: 'live', label: 'Live console', icon: Radio },
      { id: 'followups', label: 'Follow-ups', icon: CheckSquare },
    ],
  },
  {
    title: 'Insights',
    items: [
      { id: 'reports', label: 'Reports', icon: BarChart3 },
      { id: 'planning', label: 'Planning', icon: MessageSquareText },
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
  switch (page) {
    case 'overview':
      return <OverviewPage go={go} />;
    case 'sessions':
      return <SessionsPage />;
    case 'live':
      return <LivePage />;
    case 'followups':
      return <FollowUpsPage />;
    case 'reports':
      return <ReportsPage />;
    case 'planning':
      return <PlanningPage />;
    case 'schedule':
      return <SchedulePage />;
    case 'prompts':
      return <PromptsPage />;
    case 'team':
      return <TeamPage />;
    case 'roster':
      return <RosterPage />;
  }
}

function Nav({ page, go, onPick }: { page: PageId; go: (p: PageId) => void; onPick: () => void }) {
  return (
    <nav aria-label="Main" className="flex flex-col gap-6 px-3 py-4">
      {groups.map((g) => (
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
            <p className="text-xs text-muted">Payments (Demo)</p>
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
