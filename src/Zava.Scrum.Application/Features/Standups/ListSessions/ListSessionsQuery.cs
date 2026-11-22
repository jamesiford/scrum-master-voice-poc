using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Standups.ListSessions;

/// <summary>Lists a team's sessions, newest first.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record ListSessionsQuery(string TeamId) : IQuery<IReadOnlyList<SessionSummaryDto>>;
