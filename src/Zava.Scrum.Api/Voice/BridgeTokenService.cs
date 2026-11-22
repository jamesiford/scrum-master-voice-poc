using System.Security.Cryptography;
using System.Text;

namespace Mcm.Scrum.Api.Voice;

/// <summary>Issues and checks the short-lived token that lets a bridge connect to one session's websocket (SPEC 8).</summary>
/// <remarks>PROD: signed with a key from Key Vault shared by all API instances; here the key is per process.</remarks>
public sealed class BridgeTokenService(TimeProvider time)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(4);
    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);

    /// <summary>Issues a token for a session.</summary>
    /// <param name="sessionId">The session.</param>
    /// <returns>The token.</returns>
    public string Issue(Guid sessionId)
    {
        var payload = $"{sessionId:N}.{time.GetUtcNow().Add(Lifetime).ToUnixTimeSeconds()}";
        return payload + "." + Sign(payload);
    }

    /// <summary>Checks a token for a session.</summary>
    /// <param name="token">The token from the websocket query string.</param>
    /// <param name="sessionId">The session the websocket is for.</param>
    /// <returns><see langword="true"/> when the token is genuine, unexpired and for this session.</returns>
    public bool IsValid(string? token, Guid sessionId)
    {
        var parts = token?.Split('.');
        if (parts is not { Length: 3 } || parts[0] != sessionId.ToString("N") || !long.TryParse(parts[1], out var expires))
        {
            return false;
        }

        var expected = Encoding.ASCII.GetBytes(Sign($"{parts[0]}.{parts[1]}"));
        return CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(parts[2]))
            && expires > time.GetUtcNow().ToUnixTimeSeconds();
    }

    private string Sign(string payload) => Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload)));
}
