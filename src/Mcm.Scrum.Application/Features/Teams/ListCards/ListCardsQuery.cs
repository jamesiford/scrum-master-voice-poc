using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Application.Features.Teams.ListCards;

/// <summary>Lists cards that were sent, with their Adaptive Card JSON.</summary>
/// <param name="SessionId">Only this standup, or all when <see langword="null"/>.</param>
public sealed record ListCardsQuery(Guid? SessionId = null) : IQuery<IReadOnlyList<SentCard>>;
