# Zava.Scrum.AgentHost

The Foundry hosted agent (ADR 0015 S6, ADR 0021). A container image run by Foundry Agent Service on port 8088. Today it serves one route, `/invocations_ws`, a stateless relay between a caller and Azure Voice Live, plus the platform's own readiness route.

- **What it does.** Opens a Voice Live session with the agent's own identity (no keys) and forwards every text and binary frame both ways, unchanged and in order. It refuses a connection with no `team` query parameter (close 1008): the caller resolves the team, the host never infers it. The model arrives as `?model=`.
- **State.** None. A restart loses nothing; the caller reconnects with the same `agent_session_id` and the session is read from Cosmos by the API.
- **May reference.** Nothing in the solution yet. It must never reference Azure DevOps credentials; an architecture test scans this folder for them (`DelegatedWriteRulesTests`).
- **Build and deploy.** `Dockerfile.agenthost` at the repository root; `infra/hooks/agent-host.ps1` builds it in the environment registry and creates an immutable agent version. `infra/hooks/canary.ps1` moves traffic between versions.
- **Tests.** `dotnet test src/tests/Zava.Scrum.AgentHost.Tests` (relay frames, no-team refusal, kill and resume on loopback sockets).
- **Not here yet.** The voice gateway, ceremony agents, Teams and Copilot handlers and the Activity protocol endpoint are still in `Zava.Scrum.Api`; see ADR 0021.
