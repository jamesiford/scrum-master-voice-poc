using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Stories.ListParkedTopics;

/// <summary>Lists topics parked in recent standups, the natural inputs for the Story Writer.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record ListParkedTopicsQuery(string TeamId) : IQuery<IReadOnlyList<string>>;
