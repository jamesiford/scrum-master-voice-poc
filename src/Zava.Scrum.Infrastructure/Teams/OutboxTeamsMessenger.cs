using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace Mcm.Scrum.Infrastructure.Teams;

/// <summary>
/// Renders cards and stores them in a JSON outbox the portal can show. It is the offline stand-in for Teams delivery.
/// </summary>
/// <remarks>PROD: a Microsoft 365 Agents SDK adapter implements <see cref="ITeamsMessenger"/> with proactive messaging; the port is unchanged.</remarks>
public sealed class OutboxTeamsMessenger : ITeamsMessenger, ICardOutbox, IDisposable
{
    private readonly string _path;
    private readonly ICollectionStore _store;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="OutboxTeamsMessenger"/> class.</summary>
    /// <param name="store">The collection store.</param>
    /// <param name="clock">Clock.</param>
    public OutboxTeamsMessenger(ICollectionStore store, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _path = "cards.json";
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task SendAsync(TeamsMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _gate.WaitAsync(ct);
        try
        {
            var cards = await _store.ReadAsync<List<StoredCard>>(_path, () => [], ct);
            if (cards.Any(c => c.Message.DedupeKey == message.DedupeKey))
            {
                return;
            }

            cards.Add(new StoredCard(Guid.NewGuid(), _clock.UtcNow, message, CardRenderer.Render(message)));
            await _store.WriteAsync(_path, cards, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TeamsMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _gate.WaitAsync(ct);
        try
        {
            var cards = await _store.ReadAsync<List<StoredCard>>(_path, () => [], ct);
            var index = cards.FindIndex(c => c.Message.DedupeKey == message.DedupeKey);
            if (index < 0)
            {
                return;
            }

            cards[index] = cards[index] with { Message = message, AdaptiveCardJson = CardRenderer.Render(message) };
            await _store.WriteAsync(_path, cards, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SentCard>> ListAsync(Guid? sessionId, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var cards = await _store.ReadAsync<List<StoredCard>>(_path, () => [], ct);
            return cards
                .Where(c => sessionId is null || c.Message.SessionId == sessionId)
                .OrderByDescending(c => c.SentAt)
                .Select(c => new SentCard(c.Id, c.SentAt, c.Message, c.AdaptiveCardJson))
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    private sealed record StoredCard(Guid Id, DateTimeOffset SentAt, TeamsMessage Message, string AdaptiveCardJson);
}
