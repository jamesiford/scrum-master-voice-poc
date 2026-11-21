using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.ListSessions;

/// <summary>Lists a team's sessions, newest first.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record ListSessionsQuery(string TeamId) : IQuery<IReadOnlyList<SessionSummaryDto>>;
