using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Teams;

/// <summary>Error codes for team and roster use cases.</summary>
public static class TeamErrors
{
    /// <summary>Gets the error for an unknown team.</summary>
    /// <param name="teamId">The id that was not found.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string teamId) => Error.NotFound("team.not_found", $"Team '{teamId}' does not exist.");
}
