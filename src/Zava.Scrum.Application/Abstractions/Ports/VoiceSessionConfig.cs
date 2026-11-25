using Zava.Scrum.Application.Tools;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Settings for one Voice Live session.</summary>
/// <param name="Instructions">The rendered facilitator prompt.</param>
/// <param name="Tools">Tools the model may call.</param>
/// <param name="SilenceMs">Silence the service waits before ending a user turn.</param>
/// <param name="TeamId">The team the session belongs to, resolved before the call; the hosted relay refuses a call without one.</param>
/// <param name="SessionId">The standup session, which is also the hosted agent session id so a reconnect reattaches to the same sandbox.</param>
public sealed record VoiceSessionConfig(string Instructions, IReadOnlyList<ToolDefinition> Tools, int SilenceMs, string? TeamId = null, Guid? SessionId = null);
