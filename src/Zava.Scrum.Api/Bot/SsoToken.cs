using System.Text;
using System.Text.Json;

namespace Zava.Scrum.Api.Bot;

/// <summary>Reads the <c>oid</c> and <c>tid</c> claims from the Teams single sign-on token the Bot Service token store returned.</summary>
/// <remarks>
/// The payload is read, not trusted: the Bot Service issued the token to the SDK, and the on-behalf-of exchange validates it at Entra before
/// any write. The approval rule then compares these ids with the approver recorded from the activity (<c>ApproverAssertion</c>).
/// </remarks>
internal static class SsoToken
{
    /// <summary>Tries to read the object id and tenant id.</summary>
    /// <param name="token">The compact JWT.</param>
    /// <param name="oid">The object id.</param>
    /// <param name="tid">The tenant id.</param>
    /// <returns><see langword="true"/> when both claims are present and are GUIDs.</returns>
    public static bool TryReadIdentity(string? token, out Guid oid, out Guid tid)
    {
        oid = tid = Guid.Empty;
        var parts = token?.Split('.');
        if (parts is not { Length: 3 })
        {
            return false;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            return doc.RootElement.TryGetProperty("oid", out var o) && Guid.TryParse(o.GetString(), out oid)
                && doc.RootElement.TryGetProperty("tid", out var t) && Guid.TryParse(t.GetString(), out tid);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return false;
        }
    }
}
