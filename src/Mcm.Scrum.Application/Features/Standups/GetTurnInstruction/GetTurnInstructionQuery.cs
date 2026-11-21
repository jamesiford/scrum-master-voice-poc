using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Voice;

namespace Mcm.Scrum.Application.Features.Standups.GetTurnInstruction;

/// <summary>Renders the instruction for one agent turn from the prompt files in <c>agents/instructions</c>.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Reason">Why the agent is speaking.</param>
/// <param name="MemberId">The member the turn concerns, if any.</param>
/// <param name="Question">The grounded question to ask (for <see cref="SpeakReason.Clarify"/>).</param>
public sealed record GetTurnInstructionQuery(Guid SessionId, SpeakReason Reason, string? MemberId = null, string? Question = null) : IQuery<string>;
