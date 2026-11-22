using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.EndStandup;

/// <summary>Ends the standup. Safe to send twice.</summary>
/// <param name="SessionId">Session id.</param>
public sealed record EndStandupCommand(Guid SessionId) : ICommand;
