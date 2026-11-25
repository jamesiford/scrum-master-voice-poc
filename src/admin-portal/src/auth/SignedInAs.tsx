import { useMsal } from '@azure/msal-react';
import { isAuthEnabled, signedInName } from './msal';

/**
 * Shows who a decision will be recorded as. The name is a label only: the API reads the person's Entra object id, tenant and roles
 * from the validated token and ignores anything typed here (ADR 0017).
 * @returns The text.
 */
export function SignedInAs() {
  // Reading the accounts re-renders this component after sign-in.
  useMsal();
  const name = isAuthEnabled() ? signedInName() : 'the local developer';
  return (
    <p className="text-sm text-muted">
      Deciding as <span className="font-medium text-ink">{name ?? 'nobody'}</span>. Your Entra
      identity is recorded on every change written to Azure DevOps.
    </p>
  );
}
