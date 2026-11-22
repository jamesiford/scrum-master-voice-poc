using System.Net.Http.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>
/// Sends follow-up emails from the agent's mailbox with Microsoft Graph <c>sendMail</c> (M9-R1). Every email is also recorded in the
/// outbox, so the portal still lists it and a Graph failure falls back to the outbox copy instead of losing the message.
/// </summary>
/// <remarks>
/// VERIFY: https://learn.microsoft.com/graph/api/user-sendmail (application permission Mail.Send). Scope it to the agent's mailbox with an
/// application access policy: https://learn.microsoft.com/graph/auth-limit-mailbox-access.
/// PROD: a shared mailbox owned by the team, and a delivery log with retries.
/// </remarks>
public sealed class GraphEmailSender(HttpClient http, OutboxEmailSender outbox, IOptions<EmailOptions> options, ILogger<GraphEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    public bool Enabled => outbox.Enabled;

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        await outbox.SendAsync(message, ct);
        var sender = options.Value.Sender;
        if (string.IsNullOrWhiteSpace(sender))
        {
            logger.LogWarning("Email:Mode is Graph but Email:Sender is not set; the email was recorded in the outbox only");
            return;
        }

        try
        {
            using var response = await http.PostAsJsonAsync($"v1.0/users/{Uri.EscapeDataString(sender)}/sendMail", ToRequest(message), ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Graph sendMail rejected the email for follow-up {FollowUpId}: {Status}", message.FollowUpId, (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or Azure.Identity.CredentialUnavailableException or Azure.Identity.AuthenticationFailedException)
        {
            logger.LogWarning(ex, "Could not send the email for follow-up {FollowUpId} through Graph", message.FollowUpId);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EmailMessage>> ListAsync(CancellationToken ct) => outbox.ListAsync(ct);

    internal static object ToRequest(EmailMessage message) => new
    {
        message = new
        {
            subject = message.Subject,
            body = new { contentType = "Text", content = message.Body },
            toRecipients = new[] { new { emailAddress = new { address = message.To } } },
        },
        saveToSentItems = true,
    };
}
