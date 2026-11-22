using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Standups.PrepareStandup;

/// <summary>Reads the sprint, builds the pre-brief and creates a prepared session. Returns the session id.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record PrepareStandupCommand(string TeamId) : ICommand<Guid>;
