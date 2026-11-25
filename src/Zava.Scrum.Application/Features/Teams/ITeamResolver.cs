using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams;

/// <summary>Who is asking: an Entra object id and whether they hold the Admin app role.</summary>
/// <param name="ObjectId">The caller's Entra object id.</param>
/// <param name="IsAdmin">Whether the caller holds <c>Zava.Scrum.Admin</c> (every team, and the registry).</param>
public sealed record CallerContext(Guid ObjectId, bool IsAdmin);

/// <summary>
/// The one place a request is turned into a team (ADR 0018). Every entry point uses it: the portal (an explicit team id, checked against the
/// caller's teams), the Teams bot (conversation to team through the bindings), Copilot (the user's memberships), the scheduler and the media
/// bot (the team id on the session). A request that resolves to no team, or to one the caller cannot reach, is refused and logged.
/// </summary>
public interface ITeamResolver
{
    /// <summary>Resolves a team for a signed-in caller and checks the permission.</summary>
    /// <param name="caller">The caller.</param>
    /// <param name="teamId">The team id the request names.</param>
    /// <param name="permission">What the request needs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The team, or not found (also when the caller is not in it) or forbidden.</returns>
    Task<Result<Team>> ForCallerAsync(CallerContext caller, string? teamId, TeamPermission permission, CancellationToken ct);

    /// <summary>Lists the teams a caller may see: every non-archived team for an Admin, otherwise the teams they are in.</summary>
    /// <param name="caller">The caller.</param>
    /// <param name="includeArchived">Whether to include archived teams (Admin only).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The teams.</returns>
    Task<IReadOnlyList<Team>> ListForCallerAsync(CallerContext caller, bool includeArchived, CancellationToken ct);

    /// <summary>Resolves the team a Teams conversation is bound to.</summary>
    /// <param name="tenantId">The tenant on the activity.</param>
    /// <param name="conversationId">The conversation id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The team, or not found when nothing is bound (the bot then sends the onboarding card and does nothing else).</returns>
    Task<Result<Team>> ForConversationAsync(string? tenantId, string? conversationId, CancellationToken ct);

    /// <summary>Resolves the team of a user for Copilot: their memberships. With more than one it asks which; it never guesses.</summary>
    /// <param name="objectId">The user.</param>
    /// <param name="chosenTeamId">The team the user named, if they did.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The team, or <c>team.ambiguous</c> listing the choices, or not found.</returns>
    Task<Result<Team>> ForUserAsync(Guid objectId, string? chosenTeamId, CancellationToken ct);

    /// <summary>Resolves the team a session belongs to (the scheduler and the media bot carry only the session).</summary>
    /// <param name="sessionId">The session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The team, or not found.</returns>
    Task<Result<Team>> ForSessionAsync(Guid sessionId, CancellationToken ct);
}
