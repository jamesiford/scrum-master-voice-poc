import { MsalProvider } from '@azure/msal-react';
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { SignInGate } from './auth/SignInGate';
import { msalInstance } from './auth/msal';

const root = document.getElementById('root');
if (!root) {
  throw new Error('Root element not found');
}

void msalInstance.initialize().then(() => {
  createRoot(root).render(
    <StrictMode>
      <MsalProvider instance={msalInstance}>
        <FluentProvider theme={webLightTheme}>
          <SignInGate>
            <App />
          </SignInGate>
        </FluentProvider>
      </MsalProvider>
    </StrictMode>,
  );
});
