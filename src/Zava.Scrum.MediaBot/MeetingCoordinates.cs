using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Graph.Models;

namespace Zava.Scrum.MediaBot;

/// <summary>Parses calendar links without Graph lookup permissions or invented chat ids (ADR 0023).</summary>
public static class MeetingCoordinates
{
    /// <summary>Reads the meeting coordinates and rejects links outside the deployment tenant.</summary>
    /// <param name="joinUrl">The stored Teams calendar link.</param>
    /// <param name="tenantId">The allowed tenant.</param>
    /// <returns>Chat and meeting coordinates for the Communications SDK.</returns>
    public static (ChatInfo? Chat, MeetingInfo Meeting) Parse(string joinUrl, string tenantId)
    {
        if (!Uri.TryCreate(joinUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || uri.Host is not ("teams.microsoft.com" or "teams.cloud.microsoft"))
        {
            throw new ArgumentException("A Microsoft Teams HTTPS meeting link is required.", nameof(joinUrl));
        }

        // Meeting passcodes are URI values, not form fields: a literal '+' must remain a plus.
        var query = QueryHelpers.ParseQuery(uri.Query.Replace("+", "%2B", StringComparison.Ordinal));
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments is ["meet", var meetingId] && meetingId.All(char.IsAsciiDigit))
        {
            return (null, new JoinMeetingIdMeetingInfo { JoinMeetingId = meetingId, Passcode = query["p"].ToString() });
        }

        if (segments is not ["l", "meetup-join", var thread, var message] || !query.TryGetValue("context", out var context))
        {
            throw new ArgumentException("The Teams meeting link has no supported meeting coordinates.", nameof(joinUrl));
        }

        using var json = JsonDocument.Parse(context.ToString());
        var tenant = json.RootElement.GetProperty("Tid").GetString();
        if (!Guid.TryParse(tenant, out var actual) || !Guid.TryParse(tenantId, out var allowed) || actual != allowed)
        {
            throw new ArgumentException("The meeting belongs to another tenant.", nameof(joinUrl));
        }

        var organiser = json.RootElement.GetProperty("Oid").GetGuid();
        var identity = new Identity { Id = organiser.ToString(), AdditionalData = new Dictionary<string, object> { ["tenantId"] = tenantId } };
        return (new ChatInfo { ThreadId = Uri.UnescapeDataString(thread), MessageId = Uri.UnescapeDataString(message) },
            new OrganizerMeetingInfo { Organizer = new IdentitySet { User = identity } });
    }
}
