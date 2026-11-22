using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Retrospectives.PrepareRetrospective;

/// <summary>Creates a retrospective session (<c>Kind=Retrospective</c>) with the team's roster and an opening built from the sprint. A command: it stores the session.</summary>
/// <param name="TeamId">The team.</param>
public sealed record PrepareRetrospectiveCommand(string TeamId) : ICommand<Guid>;
