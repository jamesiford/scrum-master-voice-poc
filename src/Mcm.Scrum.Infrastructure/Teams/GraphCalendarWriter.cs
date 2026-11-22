using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Teams;

/// <summary>
/// Creates Teams online meetings in the agent's demo-tenant calendar with Microsoft Graph and invites the roster (M9d). Each meeting is one
/// event with <c>isOnlineMeeting</c>; the <c>transactionId</c> makes a retry create it once.
/// </summary>
/// <remarks>
/// VERIFY: https://learn.microsoft.com/graph/api/user-post-events (application permission Calendars.ReadWrite; <c>onlineMeeting.joinUrl</c> in the response).
/// Scope it to the agent's mailbox with an application access policy: https://learn.microsoft.com/graph/auth-limit-mailbox-access.
/// PROD: one recurring series for the standup, the team's own organiser mailbox, and updating or cancelling meetings when the sprint changes.
/// </remarks>
public sealed class GraphCalendarWriter(HttpClient http, IOptions<CalendarOptions> options, ILogger<GraphCalendarWriter> logger) : ICalendarWriter
{
    /// <inheritdoc />
    public bool Enabled => !string.IsNullOrWhiteSpace(options.Value.Organizer);

    /// <inheritdoc />
    public async Task<Result<CreatedMeeting>> CreateMeetingAsync(MeetingInvite invite, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(invite);
        if (!Enabled)
        {
            return Result<CreatedMeeting>.Failure(Error.Conflict("calendar.not_configured", "Calendar:Organizer is not set."));
        }

        try
        {
            using var response = await http.PostAsJsonAsync($"v1.0/users/{Uri.EscapeDataString(options.Value.Organizer!)}/events", ToRequest(invite), ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Graph rejected the meeting '{Subject}': {Status}", invite.Subject, (int)response.StatusCode);
                return Result<CreatedMeeting>.Failure(Error.ExternalRejected("calendar.graph_rejected", $"Microsoft Graph rejected '{invite.Subject}' ({(int)response.StatusCode}). Check Calendars.ReadWrite and the application access policy for the organizer mailbox."));
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return Parse(doc.RootElement, invite.Subject);
        }
        catch (Exception ex) when (ex is HttpRequestException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            logger.LogWarning(ex, "Could not create the meeting '{Subject}' through Graph", invite.Subject);
            return Result<CreatedMeeting>.Failure(Error.ExternalRejected("calendar.graph_unreachable", "Could not reach Microsoft Graph or get a token."));
        }
    }

    internal static object ToRequest(MeetingInvite invite) => new
    {
        subject = invite.Subject,
        start = new { dateTime = invite.StartsAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture), timeZone = "UTC" },
        end = new { dateTime = invite.EndsAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture), timeZone = "UTC" },
        attendees = invite.Attendees.Select(a => new { emailAddress = new { address = a }, type = "required" }),
        isOnlineMeeting = true,
        onlineMeetingProvider = "teamsForBusiness",
        transactionId = invite.TransactionId,
    };

    internal static Result<CreatedMeeting> Parse(JsonElement root, string subject)
    {
        var id = root.TryGetProperty("id", out var i) ? i.GetString() : null;
        var join = root.TryGetProperty("onlineMeeting", out var m) && m.ValueKind == JsonValueKind.Object && m.TryGetProperty("joinUrl", out var u) ? u.GetString() : null;
        return id is null || string.IsNullOrWhiteSpace(join)
            ? Result<CreatedMeeting>.Failure(Error.ExternalRejected("calendar.no_join_url", $"Graph created '{subject}' without a Teams join URL."))
            : Result<CreatedMeeting>.Success(new CreatedMeeting(id, join));
    }
}
