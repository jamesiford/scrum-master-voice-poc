using System.Text.Json;
using Microsoft.Extensions.Logging;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Infrastructure.Identity;

/// <summary>Reads an Entra group's user members with Microsoft Graph <c>GET /groups/{id}/members</c> (the managed identity holds <c>GroupMember.Read.All</c>).</summary>
/// <remarks>VERIFY: https://learn.microsoft.com/graph/api/group-list-members (application permission GroupMember.Read.All).</remarks>
public sealed class GraphEntraGroupReader(HttpClient http, ILogger<GraphEntraGroupReader> logger) : IEntraGroupReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>?> ListMembersAsync(Guid groupId, CancellationToken ct)
    {
        var members = new List<Guid>();
        var url = $"v1.0/groups/{groupId}/members/microsoft.graph.user?$select=id&$top=999";
        try
        {
            while (url is not null)
            {
                using var response = await http.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Graph could not list group {GroupId}: {Status}", groupId, (int)response.StatusCode);
                    return null;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                members.AddRange(doc.RootElement.GetProperty("value").EnumerateArray().Select(u => Guid.Parse(u.GetProperty("id").GetString()!)));
                url = doc.RootElement.TryGetProperty("@odata.nextLink", out var next) ? next.GetString() : null;
            }

            return members;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or FormatException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            logger.LogWarning(ex, "Could not reach Graph to list group {GroupId}", groupId);
            return null;
        }
    }
}
