using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.GetLiveState;

/// <summary>Reads the running floor and timebox state as JSON, for the Live console.</summary>
/// <param name="SessionId">The standup.</param>
public sealed record GetLiveStateQuery(Guid SessionId) : IQuery<string>;