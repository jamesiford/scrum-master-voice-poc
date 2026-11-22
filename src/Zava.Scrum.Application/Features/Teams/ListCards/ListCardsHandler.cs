using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Teams.ListCards;

/// <summary>Reads the card outbox.</summary>
internal sealed class ListCardsHandler(ICardOutbox outbox) : IRequestHandler<ListCardsQuery, Result<IReadOnlyList<SentCard>>>
{
    public async Task<Result<IReadOnlyList<SentCard>>> Handle(ListCardsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<SentCard>>.Success(await outbox.ListAsync(request.SessionId, ct));
}
