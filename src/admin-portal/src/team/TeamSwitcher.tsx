import { useTeams } from './TeamContext';
import { Select } from '../ui/Form';

/**
 * The team switcher: lists only the teams the caller may see and remembers the choice per person. A single team shows as plain text.
 * @returns The switcher.
 */
export function TeamSwitcher() {
  const { teams, team, select } = useTeams();
  if (!team) return <p className="text-xs text-muted">No team yet</p>;
  if (teams.length === 1) return <p className="text-xs text-muted">{team.name}</p>;
  return (
    <Select
      aria-label="Team"
      value={team.id}
      onChange={(e) => select(e.target.value)}
      className="mt-1 h-8 text-xs"
    >
      {teams.map((t) => (
        <option key={t.id} value={t.id}>
          {t.name}
          {t.status === 'Active' ? '' : ` (${t.status.toLowerCase()})`}
        </option>
      ))}
    </Select>
  );
}
