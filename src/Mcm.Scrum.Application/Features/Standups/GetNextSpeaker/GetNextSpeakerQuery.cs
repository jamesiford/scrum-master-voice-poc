using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.GetNextSpeaker;

/// <summary>Who speaks next, in speaking order, and whether they are absent.</summary>
/// <param name="SessionId">Session id.</param>
public sealed record GetNextSpeakerQuery(Guid SessionId) : IQuery<NextSpeakerDto>;
