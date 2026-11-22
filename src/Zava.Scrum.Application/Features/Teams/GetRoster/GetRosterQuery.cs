using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Domain.Members;

namespace Mcm.Scrum.Application.Features.Teams.GetRoster;

/// <summary>Reads a team's roster.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record GetRosterQuery(string TeamId) : IQuery<IReadOnlyList<Member>>;
