import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { listTeams, type Team } from '../api/client';
import { isAuthEnabled, signedInKey, signedInRoles } from '../auth/msal';
import { Spinner } from '../ui/Feedback';

const ADMIN_ROLE = 'Zava.Scrum.Admin';

/** What the team switcher and every team page read. */
interface TeamState {
  /** The teams the caller may see: their own, or every team for an Admin. */
  teams: Team[];
  /** The selected team, or `undefined` when the caller is in no team. */
  team: Team | undefined;
  /** Whether the caller holds the Admin app role. Only decides what is shown; the API enforces it. */
  isAdmin: boolean;
  /** Selects a team and remembers the choice for this person. */
  select: (teamId: string) => void;
  /** Reloads the team list, for example after an edit. */
  reload: () => void;
}

const TeamContext = createContext<TeamState | undefined>(undefined);

const storageKey = () => `zava.team.${signedInKey()}`;

/**
 * Loads the caller's teams and keeps the selected one, remembered per user. Replaces the old build-time team: nothing is hard-wired.
 * @param props - The children to render once the teams are loaded.
 * @returns The provider.
 */
export function TeamProvider({ children }: { children: ReactNode }) {
  const [teams, setTeams] = useState<Team[]>();
  const [error, setError] = useState<string>();
  const [selected, setSelected] = useState<string | null>(() => localStorage.getItem(storageKey()));
  const [tick, setTick] = useState(0);
  const isAdmin = !isAuthEnabled() || signedInRoles().includes(ADMIN_ROLE);

  useEffect(() => {
    const controller = new AbortController();
    listTeams(false, controller.signal)
      .then((result) => {
        setTeams(result);
        setError(undefined);
      })
      .catch((e: unknown) => {
        if (!controller.signal.aborted) setError(e instanceof Error ? e.message : 'Request failed');
      });
    return () => controller.abort();
  }, [tick]);

  const select = useCallback((teamId: string) => {
    localStorage.setItem(storageKey(), teamId);
    setSelected(teamId);
  }, []);
  const reload = useCallback(() => setTick((t) => t + 1), []);

  const value = useMemo<TeamState | undefined>(() => {
    if (!teams) return undefined;
    const team = teams.find((t) => t.id === selected) ?? teams[0];
    return { teams, team, isAdmin, select, reload };
  }, [teams, selected, isAdmin, select, reload]);

  if (error) {
    return (
      <p role="alert" className="p-8 text-sm text-bad">
        The teams could not be loaded: {error}
      </p>
    );
  }
  if (!value) {
    return (
      <div className="grid min-h-full place-items-center">
        <Spinner label="Loading your teams" />
      </div>
    );
  }
  return <TeamContext.Provider value={value}>{children}</TeamContext.Provider>;
}

/**
 * Reads the team state.
 * @returns The teams, the selected team, whether the caller is an Admin, and the actions.
 */
export function useTeams(): TeamState {
  const value = useContext(TeamContext);
  if (!value) throw new Error('useTeams must be used inside TeamProvider');
  return value;
}

/**
 * Reads the selected team's id. Only call it on pages rendered when a team exists (the shell guarantees that).
 * @returns The team id.
 */
export function useTeamId(): string {
  const { team } = useTeams();
  if (!team) throw new Error('No team is selected');
  return team.id;
}
