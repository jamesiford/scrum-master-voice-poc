using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Features.Standups.GetTurnInstruction;

/// <summary>Renders the instruction for one agent turn from the prompt files in <c>agents/instructions</c>.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Reason">Why the agent is speaking.</param>
/// <param name="MemberId">The member the turn concerns, if any.</param>
/// <param name="Question">The grounded question to ask (for <see cref="SpeakReason.Clarify"/>).</param>
/// <param name="Notes">Facts the template may mention: what was just noted (clarify) or flagged items nobody covered (recap).</param>
public sealed record GetTurnInstructionQuery(Guid SessionId, SpeakReason Reason, string? MemberId = null, string? Question = null, string? Notes = null) : IQuery<string>
{
    /// <summary>Gets whether independent team and prompt reads may overlap for this turn (ADR 0044).</summary>
    public bool ParallelContextReads { get; init; }
}
