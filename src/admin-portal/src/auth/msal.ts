import {
  InteractionRequiredAuthError,
  PublicClientApplication,
  type Configuration,
} from '@azure/msal-browser';

/** Public sign-in values served by the API at `/portal-config.json` (neither is a secret). */
export interface PortalConfig {
  clientId: string | null;
  tenantId: string | null;
}

let instance: PublicClientApplication | undefined;
let clientId: string | undefined;

/**
 * Whether sign-in is configured. False only when the API reports no app registration, which the API allows only in its
 * Development environment (ADR 0017); then the API supplies a local principal and no token is sent.
 * @returns True once {@link initAuth} found a client id.
 */
export function isAuthEnabled(): boolean {
  return clientId !== undefined;
}

/**
 * Creates and initialises the MSAL instance from the API's public config. Call once before rendering.
 * @param config - The public sign-in values from the API.
 * @returns The shared MSAL instance.
 */
export async function initAuth(config: PortalConfig): Promise<PublicClientApplication> {
  clientId = config.clientId ?? undefined;
  const configuration: Configuration = {
    auth: {
      clientId: clientId ?? '00000000-0000-0000-0000-000000000000',
      authority: `https://login.microsoftonline.com/${config.tenantId ?? 'organizations'}`,
      redirectUri: window.location.origin,
    },
    cache: { cacheLocation: 'sessionStorage' },
  };
  instance = new PublicClientApplication(configuration);
  await instance.initialize();
  return instance;
}

/**
 * The scope the API validates: the portal app registration's own `access_as_user`.
 * @returns The scope list, or an empty list when sign-in is not configured.
 */
export function apiScopes(): string[] {
  return clientId ? [`api://${clientId}/access_as_user`] : [];
}

/**
 * Gets a bearer token for the API, refreshing silently and falling back to a redirect when interaction is needed.
 * @returns The access token, or `undefined` when sign-in is not configured or nobody is signed in.
 */
export async function getAccessToken(): Promise<string | undefined> {
  if (!instance || !clientId) return undefined;
  const account = instance.getActiveAccount() ?? instance.getAllAccounts()[0];
  if (!account) return undefined;
  try {
    return (await instance.acquireTokenSilent({ scopes: apiScopes(), account })).accessToken;
  } catch (e: unknown) {
    if (e instanceof InteractionRequiredAuthError) {
      await instance.acquireTokenRedirect({ scopes: apiScopes(), account });
    }
    return undefined;
  }
}

/**
 * The signed-in person's display name, for the "Deciding as" text. It is only a label: the API takes the identity from the token.
 * @returns The name, or `undefined` when nobody is signed in.
 */
export function signedInName(): string | undefined {
  const account = instance?.getActiveAccount() ?? instance?.getAllAccounts()[0];
  return account?.name ?? account?.username;
}

/**
 * The app roles on the signed-in person's ID token, used only to decide what to show (the API enforces every role itself).
 * @returns The role values, empty when nobody is signed in or sign-in is not configured.
 */
export function signedInRoles(): string[] {
  const account = instance?.getActiveAccount() ?? instance?.getAllAccounts()[0];
  const roles = account?.idTokenClaims?.['roles'];
  return Array.isArray(roles) ? roles.map(String) : [];
}

/**
 * A stable key for the signed-in person, so per-user choices (the selected team) are remembered per user.
 * @returns The Entra account id, or `local` when sign-in is not configured.
 */
export function signedInKey(): string {
  const account = instance?.getActiveAccount() ?? instance?.getAllAccounts()[0];
  return account?.localAccountId ?? 'local';
}
