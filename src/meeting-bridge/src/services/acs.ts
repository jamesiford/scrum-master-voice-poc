import { DefaultAzureCredential } from '@azure/identity';
import { CommunicationIdentityClient } from '@azure/communication-identity';

/** Issues short-lived Azure Communication Services tokens for the Teams call page. */
export interface AcsTokenService {
  /**
   * Creates an ACS identity with VoIP rights.
   * @returns A token for the call page.
   */
  issue(): Promise<{ token: string; userId: string; expiresOn: string }>;
}

/**
 * Creates the token service. It signs in with Entra (the app's managed identity holds a role on the Communication Services resource), so no
 * ACS access key or connection string exists anywhere in the deployment (ADR 0020).
 * @param endpoint - `Acs__Endpoint`.
 * @param tenantId - Entra tenant to sign in to.
 * @returns The service, or `undefined` when ACS is not configured.
 */
export function createAcsTokenService(
  endpoint: string | undefined,
  tenantId: string | undefined,
): AcsTokenService | undefined {
  if (!endpoint) return undefined;
  const client = new CommunicationIdentityClient(
    endpoint,
    new DefaultAzureCredential({ tenantId }),
  );
  return {
    async issue() {
      const { user, token, expiresOn } = await client.createUserAndToken(['voip']);
      return { token, userId: user.communicationUserId, expiresOn: expiresOn.toISOString() };
    },
  };
}
