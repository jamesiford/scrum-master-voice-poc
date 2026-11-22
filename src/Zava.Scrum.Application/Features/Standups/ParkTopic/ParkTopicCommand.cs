using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.ParkTopic;

/// <summary>Takes a topic offline and records it in the parking lot.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="Topic">Short description of the topic.</param>
public sealed record ParkTopicCommand(Guid SessionId, string Topic) : ICommand;
