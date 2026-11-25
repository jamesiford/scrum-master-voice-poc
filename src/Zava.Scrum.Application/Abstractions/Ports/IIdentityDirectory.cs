namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Resolves a user principal name to an Entra object id (Microsoft Graph).</summary>
public interface IIdentityDirectory
{
    /// <summary>Finds a user's object id.</summary>
    /// <param name="userPrincipalName">The user principal name or mail address.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The object id, or <see langword="null"/> when no such user exists or the directory could not be reached.</returns>
    Task<Guid?> FindObjectIdAsync(string userPrincipalName, CancellationToken ct);
}
