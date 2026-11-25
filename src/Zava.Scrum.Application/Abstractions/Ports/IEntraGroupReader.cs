namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Reads the members of an Entra group (Microsoft Graph), for the optional per-team membership sync.</summary>
public interface IEntraGroupReader
{
    /// <summary>Lists a group's user members.</summary>
    /// <param name="groupId">The group's object id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The members' object ids, or <see langword="null"/> when the group could not be read.</returns>
    Task<IReadOnlyList<Guid>?> ListMembersAsync(Guid groupId, CancellationToken ct);
}
