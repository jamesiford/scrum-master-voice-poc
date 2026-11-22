using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Standups.GetTimeRemaining;

/// <summary>Time left in the standup's timebox.</summary>
/// <param name="SessionId">Session id.</param>
public sealed record GetTimeRemainingQuery(Guid SessionId) : IQuery<TimeRemainingDto>;
