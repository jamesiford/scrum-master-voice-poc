using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Standups.CompleteSpeakerTurn;

/// <summary>Marks a member's turn as over so the next speaker query moves on.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="MemberId">Member id.</param>
public sealed record CompleteSpeakerTurnCommand(Guid SessionId, string MemberId) : ICommand;
