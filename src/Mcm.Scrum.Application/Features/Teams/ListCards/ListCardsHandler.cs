using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Teams.ListCards;

/// <summary>Reads the card outbox.</summary>
internal sealed class ListCardsHandler(ICardOutbox outbox) : IRequestHandler<ListCardsQuery, Result<IReadOnlyList<SentCard>>>
{
    public async Task<Result<IReadOnlyList<SentCard>>> Handle(ListCardsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<SentCard>>.Success(await outbox.ListAsync(request.SessionId, ct));
}
