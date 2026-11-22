namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>An email to send.</summary>
/// <param name="To">Recipient address.</param>
/// <param name="Subject">Subject line.</param>
/// <param name="Body">Plain-text body.</param>
/// <param name="FollowUpId">The follow-up the email carries, for traceability.</param>
public sealed record EmailMessage(string To, string Subject, string Body, Guid? FollowUpId);

/// <summary>Sends follow-ups by email (F3, stretch). Behind <c>Features:EmailFollowUps</c>.</summary>
public interface IEmailSender
{
    /// <summary>Gets a value indicating whether email follow-ups are switched on.</summary>
    bool Enabled { get; }

    /// <summary>Sends an email.</summary>
    /// <param name="message">The message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SendAsync(EmailMessage message, CancellationToken ct);

    /// <summary>Lists emails sent so far, newest first.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sent emails.</returns>
    Task<IReadOnlyList<EmailMessage>> ListAsync(CancellationToken ct);
}
