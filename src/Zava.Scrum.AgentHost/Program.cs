using Azure.AI.AgentServer.Invocations;
using Zava.Scrum.AgentHost.Voice;

// The Foundry hosted agent (ADR 0015 S6, ADR 0021). The platform fixes the port (8088) and the routes: /invocations_ws for voice and the
// platform's own readiness route. Today the host carries the voice relay only; the ceremony agents and channel handlers follow.
InvocationsServer.Run<VoiceRelayHandler>();
