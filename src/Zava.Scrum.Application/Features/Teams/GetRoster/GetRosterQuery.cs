using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Application.Features.Teams.GetRoster;

/// <summary>Reads a team's roster.</summary>
/// <param name="TeamId">Team id.</param>
public sealed record GetRosterQuery(string TeamId) : IQuery<IReadOnlyList<Member>>;
