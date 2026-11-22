import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import type { ReactNode } from 'react';
import { Button } from '../ui/Button';
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
  if (!authEnabled || signedIn) return <>{children}</>;
  return (
    <main className="grid min-h-full place-items-center p-6">
      <div className="w-full max-w-sm rounded-2xl border border-line bg-surface p-8 text-center shadow-card">
        <h1 className="text-xl font-semibold">Zava Scrum Agent</h1>
        <p className="mt-2 text-sm text-muted">
          Sign in to manage standups, follow-ups and reports.
        </p>
        <Button
          variant="primary"
          className="mt-6 w-full"
          onClick={() => void instance.loginRedirect()}
        >
          Sign in
        </Button>
      </div>
    </main>
  );
}
