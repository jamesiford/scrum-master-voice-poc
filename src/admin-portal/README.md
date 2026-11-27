# admin-portal

React 19 + Vite + Fluent UI v9 + MSAL.js. M0 is the shell with sign-in and an "API healthy" badge.

- **Layout:** `src/features/*` (UI), `src/hooks` (data access), `src/api` (the one typed client; components never call `fetch`).
- **Auth:** set `VITE_AAD_CLIENT_ID` (and optionally `VITE_AAD_TENANT_ID`) to require sign-in. Unset, the gate is open (DEMO shortcut).
- **Run:** `npm install && npm run dev` (proxies `/api` to `API_URL`, default http://localhost:5080).
- **Checks:** `npm run typecheck`, `npm run lint`, `npm run build`, `npm run test` (Node 22.6+; focused roster/guest speaker-label rules).
