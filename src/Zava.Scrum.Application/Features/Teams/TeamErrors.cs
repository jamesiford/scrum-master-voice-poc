using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Teams;

/// <summary>Error codes for team and roster use cases.</summary>
public static class TeamErrors
{
    /// <summary>Gets the error for an unknown team.</summary>
    /// <param name="teamId">The id that was not found.</param>
    /// <returns>The error.</returns>
    public static Error NotFound(string teamId) => Error.NotFound("team.not_found", $"Team '{teamId}' does not exist.");

    /// <summary>Gets the error for a request that names no team.</summary>
    public static Error NoTeam { get; } = Error.Invalid("team.none", "This request does not resolve to a team.");

    /// <summary>Gets the error for a team that is archived and so read-only.</summary>
    /// <param name="teamId">The team.</param>
    /// <returns>The error.</returns>
    public static Error Archived(string teamId) => Error.Conflict("team.archived", $"Team '{teamId}' is archived and read-only.");

    /// <summary>Gets the error for a Teams conversation that is bound to no team.</summary>
    public static Error Unbound { get; } = Error.NotFound("team.conversation_unbound", "This conversation is not bound to a team.");

    /// <summary>Gets the error for a Teams conversation bound to more than one team.</summary>
    public static Error AmbiguousBinding { get; } = Error.Conflict("team.conversation_ambiguous", "This conversation is bound to more than one team. An Admin must fix the bindings.");

    /// <summary>Gets the error for a user in several teams who did not say which: the choices are listed, never guessed.</summary>
    /// <param name="names">The team names.</param>
    /// <returns>The error.</returns>
    public static Error Ambiguous(IEnumerable<string> names) => Error.Conflict("team.ambiguous", $"You are in more than one team. Which one? {string.Join(", ", names)}");

    /// <summary>Gets the error for a team id that is already registered.</summary>
    /// <param name="teamId">The id.</param>
    /// <returns>The error.</returns>
    public static Error Exists(string teamId) => Error.Conflict("team.exists", $"Team '{teamId}' is already registered.");

    /// <summary>Gets the error for an Azure DevOps organisation owned by another Entra tenant.</summary>
    /// <param name="organisation">The organisation URL.</param>
    public static Error OtherTenant(string organisation) => Error.Invalid(
        "team.org_other_tenant",
        $"The Azure DevOps organisation '{organisation}' belongs to a different Entra tenant. A managed identity from this tenant cannot be added to it, so it cannot be registered.");

    /// <summary>Gets the error for an organisation whose tenant could not be determined.</summary>
    /// <param name="organisation">The organisation URL.</param>
    public static Error TenantUnknown(string organisation) => Error.Invalid(
        "team.org_tenant_unknown",
        $"Could not confirm that '{organisation}' belongs to this Entra tenant. Check the organisation URL and try again.");

    /// <summary>Gets the error for an organisation URL that is not an Azure DevOps organisation.</summary>
    public static Error BadOrganisation { get; } = Error.Invalid("team.org_invalid", "The Azure DevOps organisation must be https://dev.azure.com/{name} or https://{name}.visualstudio.com.");}
