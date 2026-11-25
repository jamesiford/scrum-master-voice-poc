import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import type { ReactNode } from 'react';
import { Button } from '../ui/Button';
import { apiScopes, isAuthEnabled } from './msal';

/**
 * Renders children once signed in, or a sign-in prompt. With no client id configured the gate is open.
 * The gate is open only when the API reports no app registration, which the API permits only in its Development environment.
 * @param props - Children to protect.
 * @returns The gate.
 */
export function SignInGate({ children }: { children: ReactNode }) {
  const { instance } = useMsal();
  const signedIn = useIsAuthenticated();
  if (!isAuthEnabled() || signedIn) return <>{children}</>;
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
          onClick={() => void instance.loginRedirect({ scopes: apiScopes() })}
        >
          Sign in
        </Button>
      </div>
    </main>
  );
}
