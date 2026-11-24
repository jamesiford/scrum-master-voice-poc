# Spikes

Throwaway experiments for M11 (ADR 0015). Nothing here is referenced by `Zava.Scrum.sln`, and nothing here ships. The `Directory.Build.props` and
`Directory.Packages.props` in this folder stop the repository's strict build settings and central package versions applying to the spikes.

| Folder | Question | What it does |
| --- | --- | --- |
| `s3-hosted-agent` | S3, S4 | A minimal hosted agent that serves `/invocations` and `/invocations_ws`, reports process start time and a `$HOME` marker, and can probe Azure DevOps with its own identity (`?probe=env`, `?probe=ado`). |
| `s3-deploy` | S3 | Creates a hosted agent version, times provisioning, the cold and warm first request and a WebSocket binary round trip. Needs `S3_IMAGE`. |
| `s1-pkg` | S1 | The Teams manifest that Foundry generated for the spike agent (`POST .../microsoft365/zip`), kept as evidence. |

The spike agent `s3-spike` and the `AcrPull` role assignment it needed are listed in ADR 0015 under clean-up.
