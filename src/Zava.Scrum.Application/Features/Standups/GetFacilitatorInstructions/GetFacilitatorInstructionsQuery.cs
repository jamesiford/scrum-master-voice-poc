using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Standups.GetFacilitatorInstructions;

/// <summary>Renders the facilitator's system prompt (<c>agents/instructions/facilitator.md</c>) for a session.</summary>
/// <param name="SessionId">Session id.</param>
public sealed record GetFacilitatorInstructionsQuery(Guid SessionId) : IQuery<string>;
