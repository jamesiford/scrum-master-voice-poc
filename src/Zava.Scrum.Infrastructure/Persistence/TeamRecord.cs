using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>One team-owned record with indexed routing fields and the unchanged persistence payload.</summary>
public sealed record TeamRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("teamId")] string TeamId,
    [property: JsonPropertyName("collection")] string Collection,
    [property: JsonPropertyName("sessionId")] string SessionId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("generation")] long Generation,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("json")] JsonElement Json)
{
    /// <summary>Returns a deterministic ASCII id without Cosmos-reserved characters.</summary>
    public static string IdFor(string collection, string key) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(collection + "\n" + key)));
}
