using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Tests.TestDoubles;

internal sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly Dictionary<Guid, StandupSession> _items = [];

    public Task<StandupSession?> GetAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<StandupSession>> ListAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<StandupSession>>(_items.Values.OrderByDescending(s => s.CreatedAt).ToList());

    public Task SaveAsync(StandupSession session, CancellationToken ct)
    {
        _items[session.Id] = session;
        return Task.CompletedTask;
    }
}
