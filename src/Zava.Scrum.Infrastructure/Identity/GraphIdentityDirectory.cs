using System.Text.Json;
using Microsoft.Extensions.Logging;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Infrastructure.Identity;

/// <summary>
/// Resolves a user principal name to an Entra object id with Microsoft Graph <c>GET /users/{upn}</c>. The managed identity holds the
/// <c>User.Read.All</c> application permission, granted by <c>infra/hooks/entra-post.ps1</c>.
/// </summary>
/// <remarks>VERIFY: https://learn.microsoft.com/graph/api/user-get (permission User.Read.All).</remarks>
public sealed class GraphIdentityDirectory(HttpClient http, ILogger<GraphIdentityDirectory> logger) : IIdentityDirectory
{
    /// <inheritdoc />
    public async Task<Guid?> FindObjectIdAsync(string userPrincipalName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrincipalName);
        try
        {
            using var response = await http.GetAsync($"v1.0/users/{Uri.EscapeDataString(userPrincipalName)}?$select=id", ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("Graph did not resolve {UserPrincipalName}: {Status}", userPrincipalName, (int)response.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.TryGetProperty("id", out var id) && Guid.TryParse(id.GetString(), out var guid) ? guid : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            logger.LogWarning(ex, "Could not reach Graph to resolve {UserPrincipalName}", userPrincipalName);
            return null;
        }
    }
}
