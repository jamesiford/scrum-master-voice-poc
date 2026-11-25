using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Application.Features.Teams.ListCards;

/// <summary>Lists cards that were sent, with their Adaptive Card JSON.</summary>
/// <param name="TeamId">Only this team's cards. The API always supplies it, so a caller never sees another team's outbox.</param>
/// <param name="SessionId">Only this standup, or all when <see langword="null"/>.</param>
public sealed record ListCardsQuery(Guid? SessionId = null, string? TeamId = null) : IQuery<IReadOnlyList<SentCard>>;
