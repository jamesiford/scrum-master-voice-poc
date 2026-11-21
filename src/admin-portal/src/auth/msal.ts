import { PublicClientApplication, type Configuration } from '@azure/msal-browser';

/** Entra app registration client id. When unset, sign-in is bypassed (DEMO shortcut). */
export const clientId: string | undefined = import.meta.env['VITE_AAD_CLIENT_ID'] || undefined;

/** Entra tenant id used to build the authority. */
const tenantId: string = import.meta.env['VITE_AAD_TENANT_ID'] || 'organizations';

/** Whether MSAL sign-in is configured. */
export const authEnabled = clientId !== undefined;

const configuration: Configuration = {
  auth: {
    clientId: clientId ?? '00000000-0000-0000-0000-000000000000',
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: window.location.origin,
  },
  cache: { cacheLocation: 'sessionStorage' },
};

/** Shared MSAL instance. */
export const msalInstance = new PublicClientApplication(configuration);
