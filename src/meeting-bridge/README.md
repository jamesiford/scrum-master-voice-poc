# meeting-bridge

Node 22 + TypeScript bridge (SPEC 7.3). M2 serves the **Simulated Room** and the **demo cast**; ACS interop arrives in M4.

M11 6a adds a separate Windows-native alternative in `src\Zava.Scrum.MediaBot` (ADR 0023). API `Bridge:Mode=Acs` remains the default; `Simulated` uses this project's browser room and refuses Teams joins; `GraphMedia` uses the Windows host, not Chromium. The session API still accepts `simulated` or `acs`: the latter selects the configured Teams adapter. The Simulated Room remains available with every adapter selection. Unlike this bridge's existing saved-update path, GraphMedia does not save audio-derived transcripts or follow-ups. See `docs\DEMO.md` and `docs\DEPLOY.md` for switching and the TLS/RSC gates.

- **Layout:** `src/domain` (pure cast planner), `src/services` (cast file loader, Azure Speech over REST with an Entra token), `src/routes` (HTTP), `src/web` (browser code: room page, audio, microphone worklet; bundled by esbuild into `public/`).
- **Routes:** `/room/{sessionId}` (the room), `/static/*`, `GET /cast/script`, `POST /cast/line`, `/healthz`.
- **Run:** `npm install && npm start` (port 5090, or `PORT`). `npm start` bundles the browser code first. Settings come from environment variables: `Demo__CastFile`, `SPEECH_ENDPOINT` (Azure Speech resource endpoint), `AZURE_TENANT_ID`.
- **Open a room:** start a prepared session from the portal (Sessions > Start in Simulated Room). The link carries a short-lived token and the API address. Tick **Autopilot** to let the cast play your lines too, so one person can run a five-person standup with no microphone.
- **Cast:** `demo/cast/payments-standup.yaml`, re-read for every session, so you can edit lines during a demo.
- **Checks:** `npm run typecheck`, `npm run lint`, `npm test` (cast planner), and `npm run e2e` (the scripted standup in Microsoft Edge; needs the stack running, `az login` and a seeded sandbox).
