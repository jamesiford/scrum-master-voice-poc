# Zava.Scrum.MediaBot

Windows/x64 application-hosted Teams media adapter (ADR 0023), separate from the API and the Node ACS bridge. Uses the real Graph Communications media SDK; has no Application or Infrastructure project dependency and no audio storage.

`infra\hooks\media-host.ps1` publishes and provisions the runtime after opt-in Bicep provisioning. `infra\hooks\media-bootstrap.ps1` installs the trusted machine certificate and startup task. Do not run these deployment hooks in conductor step 6a. Certificate and meeting RSC actions are in `docs\DEPLOY.md`.

Run on a provisioned Windows host: `Zava.Scrum.MediaBot.exe`, with `MediaBot` settings in the protected `C:\ProgramData\ZavaMedia\app\appsettings.json`. HTTPS control uses `x-bridge-key`; Graph callbacks use the SDK's authentication. The API capability additionally binds the session and explicit team before a join. Ports: TCP 443 and 8445. Default maximum concurrent meetings: one.

Local checks:

```pwsh
dotnet build src\Zava.Scrum.MediaBot
dotnet test src\tests\Zava.Scrum.MediaBot.Tests
dotnet test src\tests\Zava.Scrum.Api.Tests --filter FullyQualifiedName~GraphMediaVoiceGatewayTests
dotnet test src\tests\Zava.Scrum.ArchitectureTests
pwsh eng\check-media-sdk.ps1
```

PCM is 24 kHz/PCM16/mono, 960 bytes per 20 ms. Unmatched identities stay guests. No transcript or meeting-derived follow-up is saved; guests' PCM is never forwarded to the model. Return playback and input queues are bounded. A reconnect retains the Graph call but resets the ephemeral voice conversation. Live Teams acceptance is deferred to step 6b.
