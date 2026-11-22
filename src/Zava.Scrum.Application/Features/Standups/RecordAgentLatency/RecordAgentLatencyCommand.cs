using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Standups.RecordAgentLatency;

/// <summary>Stores one end-of-speech to first-audio sample (the demo latency metric).</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Milliseconds">The sample.</param>
public sealed record RecordAgentLatencyCommand(Guid SessionId, int Milliseconds) : ICommand;
