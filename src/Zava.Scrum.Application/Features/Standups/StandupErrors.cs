using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Standups;

/// <summary>Error codes for standup use cases.</summary>
public static class StandupErrors
{
    /// <summary>Gets the error for steering a standup that is not running here.</summary>
    /// <param name="id">The session.</param>
    /// <returns>The error.</returns>
    public static Error NotLive(Guid id) => Error.Conflict("session.not_live", $"Session '{id}' is not running.");

    /// <summary>Gets the error for joining a Teams meeting without a meeting link.</summary>
    public static Error JoinUrlRequired { get; } = Error.Invalid("session.join_url_required", "A Teams meeting link is required for a Teams standup.");

    /// <summary>Gets the error for a roster with no active member.</summary>
    public static Error RosterEmpty { get; } =
        Error.Conflict("standup.roster_empty", "The roster has no active members. Sync the roster first.");

    /// <summary>Gets the error for an unknown session.</summary>
    /// <param name="id">The id that was not found.</param>
    /// <returns>The error.</returns>
    public static Error SessionNotFound(Guid id) => Error.NotFound("session.not_found", $"Session '{id}' does not exist.");

    /// <summary>Gets the error for a member who is not in the session's pre-brief.</summary>
    /// <param name="memberId">The member id.</param>
    /// <returns>The error.</returns>
    public static Error MemberNotInBrief(string memberId) => Error.NotFound("session.member_not_in_brief", $"Member '{memberId}' is not part of this standup.");
}
