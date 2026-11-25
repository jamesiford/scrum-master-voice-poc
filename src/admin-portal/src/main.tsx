import { MsalProvider } from '@azure/msal-react';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { TeamProvider } from './team/TeamContext';
import { SignInGate } from './auth/SignInGate';
import { getPortalConfig } from './api/client';
import { initAuth } from './auth/msal';
import './index.css';

const root = document.getElementById('root');
if (!root) {
  throw new Error('Root element not found');
}

void getPortalConfig()
  .then(initAuth)
  .then((msalInstance) => {
    createRoot(root).render(
      <StrictMode>
        <MsalProvider instance={msalInstance}>
          <SignInGate>
            <TeamProvider>
              <App />
            </TeamProvider>
          </SignInGate>
        </MsalProvider>
      </StrictMode>,
    );
  });
