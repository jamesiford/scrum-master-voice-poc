using Zava.Scrum.Application.Tools;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Settings for one Voice Live session.</summary>
/// <param name="Instructions">The rendered facilitator prompt.</param>
/// <param name="Tools">Tools the model may call.</param>
/// <param name="SilenceMs">Silence the service waits before ending a user turn.</param>
public sealed record VoiceSessionConfig(string Instructions, IReadOnlyList<ToolDefinition> Tools, int SilenceMs);
