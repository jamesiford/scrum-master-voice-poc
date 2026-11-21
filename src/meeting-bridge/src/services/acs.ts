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
 * Creates the token service. A connection string (Container Apps secret) wins; otherwise the endpoint is used with Entra.
 * @param connectionString - `ACS_CONNECTION_STRING`.
 * @param endpoint - `Acs__Endpoint`.
 * @param tenantId - Entra tenant to sign in to.
 * @returns The service, or `undefined` when ACS is not configured.
 */
export function createAcsTokenService(
  connectionString: string | undefined,
  endpoint: string | undefined,
  tenantId: string | undefined,
): AcsTokenService | undefined {
  // PROD: use the Entra path only, with a role assignment, and drop the connection string.
  const client = connectionString
    ? new CommunicationIdentityClient(connectionString)
    : endpoint
      ? new CommunicationIdentityClient(endpoint, new DefaultAzureCredential({ tenantId }))
      : undefined;
  if (!client) return undefined;
  return {
    async issue() {
      const { user, token, expiresOn } = await client.createUserAndToken(['voip']);
      return { token, userId: user.communicationUserId, expiresOn: expiresOn.toISOString() };
    },
  };
}
