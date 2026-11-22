namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>A card that was sent, with the Adaptive Card JSON a real Teams client would render.</summary>
/// <param name="Id">Outbox id.</param>
/// <param name="SentAt">When it was sent.</param>
/// <param name="Message">What was sent.</param>
/// <param name="AdaptiveCardJson">The rendered Adaptive Card.</param>
public sealed record SentCard(Guid Id, DateTimeOffset SentAt, TeamsMessage Message, string AdaptiveCardJson);

/// <summary>Read side of the messenger: what has been sent. Lets the portal show cards when no Teams tenant is connected.</summary>
public interface ICardOutbox
{
    /// <summary>Lists sent cards, newest first.</summary>
    /// <param name="sessionId">Only this standup, or all when <see langword="null"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cards.</returns>
    Task<IReadOnlyList<SentCard>> ListAsync(Guid? sessionId, CancellationToken ct);
}
