import { Badge } from '@fluentui/react-components';
import { useHealth } from '../../hooks/useHealth';

/**
 * Shows "API healthy" once the API answers through the full mediator pipeline (M0 demo check).
 * @returns The status badge.
 */
export function HealthBadge() {
  const state = useHealth();

  if (state.kind === 'loading') {
    return <Badge appearance="outline">Checking API...</Badge>;
  }
  if (state.kind === 'unhealthy') {
    return (
      <Badge appearance="filled" color="danger">
        API unreachable: {state.message}
      </Badge>
    );
  }
  return (
    <Badge appearance="filled" color="success">
      API healthy
    </Badge>
  );
}
