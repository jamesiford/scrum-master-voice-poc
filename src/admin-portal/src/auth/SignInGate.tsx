import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { Button, Text } from '@fluentui/react-components';
import type { ReactNode } from 'react';
import { authEnabled } from './msal';

/**
 * Renders children once signed in, or a sign-in prompt. With no client id configured the gate is open.
 * DEMO: auth bypass exists only so the shell runs locally before an app registration exists.
 * @param props - Children to protect.
 * @returns The gate.
 */
export function SignInGate({ children }: { children: ReactNode }) {
  const { instance } = useMsal();
  const signedIn = useIsAuthenticated();

  if (!authEnabled || signedIn) {
    return <>{children}</>;
  }

  return (
    <div style={{ padding: 24 }}>
      <Text as="p" size={500}>
        Sign in to manage the MCM Scrum Agent.
      </Text>
      <Button appearance="primary" onClick={() => void instance.loginRedirect()}>
        Sign in
      </Button>
    </div>
  );
}
