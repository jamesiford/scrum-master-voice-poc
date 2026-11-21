import { MessageBar, MessageBarBody, Spinner } from '@fluentui/react-components';

/**
 * Shows a plain error, or a spinner while loading. Failures are surfaced instead of silent (demo reliability).
 * @param props - Loading flag and optional error text.
 * @returns The status element, or nothing when idle and healthy.
 */
export function QueryStatus({ loading, error }: { loading: boolean; error: string | undefined }) {
  if (error) {
    return (
      <MessageBar intent="error">
        <MessageBarBody>{error}</MessageBarBody>
      </MessageBar>
    );
  }
  return loading ? <Spinner size="tiny" label="Loading" /> : null;
}
