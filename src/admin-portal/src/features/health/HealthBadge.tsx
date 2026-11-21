import { Badge } from '../../ui/Badge';
import { useHealth } from '../../hooks/useHealth';

/**
 * Shows "API healthy" once the API answers through the full mediator pipeline (M0 demo check).
 * @returns The status badge.
 */
export function HealthBadge() {
  const state = useHealth();
  if (state.kind === 'loading') return <Badge dot>Checking API</Badge>;
  if (state.kind === 'unhealthy')
    return (
      <Badge tone="bad" dot>
        API unreachable
      </Badge>
    );
  return (
    <Badge tone="ok" dot>
      API healthy
    </Badge>
  );
}
