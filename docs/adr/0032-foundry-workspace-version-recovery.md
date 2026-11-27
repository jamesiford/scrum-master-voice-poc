# ADR 0032: Bounded hosted-version workspace recovery

Date:27November2026. Status:accepted implementation; repeat deployment pending.

## Context

The invalid second G8 up exited1 after1036.6886s. Hosted image build/push
succeeded, but version creation returnedHTTP409:
`The project workspace identity changed during version creation. Read the
current agent and retry.` Request279ff8f7e9fdc6c39b993d57de16d99f.
One actual current-agent GET returned200 and the first cycle's instance
principal, despite workspace recreation. This is not an authentication
failure or an unavailable image. The reduced native-disabled deployment
cannot count as a full cycle, independently of this service failure.

## Decision

AgentDeploy recognises only that409/message, logs it, reads the current
agent and retries creation at most twice. Authentication, unrelated
conflicts and failed reads propagate immediately. The original active/
failed-version polling remains unchanged. No new retry framework, SDK
upgrade, manual resource or authentication loop is introduced.

The small retry method is shared as source with existing AgentHost tests,
without importing the tool's isolated preview SDK into the stable solution
(ADR0021). Generic return typing only preserves that SDK boundary.

Current Learn confirms
[GetAgent](https://learn.microsoft.com/dotnet/api/azure.ai.projects.agents.agentadministrationclient.getagent?view=azure-dotnet)
and
[CreateAgentVersion](https://learn.microsoft.com/dotnet/api/azure.ai.projects.agents.agentadministrationclient.createagentversion?view=azure-dotnet).
The bounded read/retry follows the explicit actual service response.

## Validation and consequences

Focused retry-path tests6/6 and all three builds pass; solution .NET has
0warnings/errors and formatting passes. The isolated preview tool retains
its existing NU1603 dependency-resolution warning; package versions remain
unchanged. Tests cover read-before-create order, the two-retry cap,
unrelated409/403/429 no-retry and refused refresh propagation.
Live recovery and the second complete empty native-enabled up/verify
remain necessary; implementation alone is not G8 acceptance.
