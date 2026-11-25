using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.TestConnection;

/// <summary>Tests an Azure DevOps connection with the API's managed identity and reports exactly which step failed. Stores nothing.</summary>
/// <param name="TeamId">The saved team to test, or <see langword="null"/> to test <paramref name="Ado"/> (the onboarding wizard, before the team exists).</param>
/// <param name="Ado">The connection to test when no team is given.</param>
public sealed record TestTeamConnectionQuery(string? TeamId, AdoSettings? Ado = null) : IQuery<ConnectionReport>;
