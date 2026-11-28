# Zava.Scrum.MediaBot

Windows/x64 application-hosted Teams media adapter (ADR 0023), separate from the API and the Node ACS bridge. Uses the real Graph Communications media SDK; has no Application or Infrastructure project dependency and no audio storage.

`infra\hooks\media-host.ps1` publishes and bootstraps the runtime after opt-in Bicep provisioning. The existing-environment `media-provision.ps1` helper deploys that same module without resetting working containers. `media-certificate.ps1` obtains trusted TLS for the Azure public-IP DNS label using HTTP-01 and VM-identity vault import. `media-bootstrap.ps1` installs the machine certificate and SYSTEM startup task; the host hook registers the public app credential before starting it. Rerun monthly for renewal; no unattended renewal scheduler exists. Meeting catalogue/RSC actions are in `docs\DEPLOY.md`.

Run on a provisioned Windows host: `Zava.Scrum.MediaBot.exe`, with `MediaBot` settings in the protected `C:\ProgramData\ZavaMedia\app\appsettings.json`. HTTPS control uses `x-bridge-key`; Graph callbacks use the SDK's authentication. The API capability additionally binds the session and explicit team before a join. Ports: TCP 443 and 8445; TCP 80 only serves a temporary certificate challenge listener. Default maximum concurrent meetings: one. Public `/healthz` reports `recording:false` and `maxConcurrentMeetings:1`, not a load-tested capacity claim.

Local checks:

```pwsh
dotnet build src\Zava.Scrum.MediaBot
dotnet test src\tests\Zava.Scrum.MediaBot.Tests
dotnet test src\tests\Zava.Scrum.Api.Tests --filter FullyQualifiedName~GraphMediaVoiceGatewayTests
dotnet test src\tests\Zava.Scrum.ArchitectureTests
pwsh eng\check-media-sdk.ps1
```

PCM is 24 kHz/PCM16/mono, 960 bytes per 20 ms. Unmatched identities stay guests. No transcript or meeting-derived follow-up is saved; guests' PCM is never forwarded to the model. Return playback and input queues are bounded. A reconnect retains the Graph call but resets the ephemeral voice conversation. Hosted in zavaw3/westus3 on D4s_v5; native G1/G2/G6 and human barge-in remain n=0 pending the documented meeting prerequisites. Synthetic tagged-frame/cancellation probes are not native acceptance. Shared opening-state speech does not cancel. ACS and Direct voice remain defaults.

ADR 0024 adds a browser-free SkiaSharp 1280x720 RGB24 renderer and a real
send-only VBSS socket for API `Board:Mode=MediaShare`; default `Share` leaves
native audio independent. Authenticated team/session board changes trigger a
draw; cached pixels keep the stream alive at one fps. The worker owns sharer/
viewer transitions and reconnect renegotiation separately from voice, reports
failures to the portal and clears its ephemeral snapshot when the call ends.
No Application dependency, meeting data repository or personal tab is added.

`/healthz` renders at startup and reports `boardRendererReady`, `boardWidth`
and `boardHeight`; this proves the installed native Skia payload runs, not that
a Teams participant saw the share or that the encoder meets the cap. Genuine
Chromium/native goldens and their measured pixel differences are retained in
the tests. ADR0045 supersedes pixel identity with content, legibility and
delivery requirements. Larger standup text and coverage retain the same
facts; received recap submission precedes call deletion. Actual Teams
reception/encoder evidence remains unrun, so keep ACS.
Native13a842d exact DLL/version/font and hosting were verified12:49:15UTC
on28November2026; intentional Deallocated confirmed12:58:26UTC.
API/ACS stay running. Follow DEPLOY.md's new scoped allocation/readiness
prerequisite before a real rehearsal; historical readiness is not uptime.

Four-state OCR/contact sheets use the commands below after the focused tests.
Final strict OCR52/53 at1080p/480p fails zero-days recognition; glyph12/18/8px
passes. Preserve that failure, not an earlier better OCR cohort. `BoardReceiverTests`
uses real loopback TCP/RGB24 and independently hashes received frames;
it is not Graph encoding or Teams visibility proof.

Targeted board checks:

```pwsh
dotnet test src\tests\Zava.Scrum.MediaBot.Tests --filter "FullyQualifiedName~BoardRendererTests|FullyQualifiedName~BoardShareSessionTests|FullyQualifiedName~BoardReceiverTests"
dotnet test src\tests\Zava.Scrum.Api.Tests --filter "FullyQualifiedName~GraphMediaVoiceGatewayTests|FullyQualifiedName~EphemeralMediaBoardTests"
powershell.exe -NoProfile -ExecutionPolicy RemoteSigned -File eng\measure-board-ocr.ps1
```

The OCR command explicitly exits1 when the frozen acceptance threshold fails;
passing renderer/unit tests are not a substitute for that result.
