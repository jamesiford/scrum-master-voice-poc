using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.ListTeams;

/// <summary>Lists the teams a caller may see: all for an Admin, otherwise only those they are in.</summary>
/// <param name="Caller">Who is asking.</param>
/// <param name="IncludeArchived">Include archived teams (Admin only).</param>
public sealed record ListTeamsQuery(CallerContext Caller, bool IncludeArchived = false) : IQuery<IReadOnlyList<Team>>;
