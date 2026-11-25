using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Tests.TestDoubles;

internal sealed class InMemoryFollowUpRepository : IFollowUpRepository
{
    private readonly Dictionary<Guid, FollowUpSnapshot> _items = [];

    public Task<FollowUp?> GetAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_items.TryGetValue(id, out var s) ? FollowUp.Restore(s) : null);

    public Task<IReadOnlyList<FollowUp>> ListAsync(string teamId, FollowUpStatus? status, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<FollowUp>>(_items.Values.Select(FollowUp.Restore).ToList());

    public Task<IReadOnlyList<FollowUp>> ListBySessionAsync(Guid sessionId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<FollowUp>>(_items.Values.Where(s => s.SessionId == sessionId).Select(FollowUp.Restore).ToList());

    public FollowUpSnapshot Snapshot(Guid id) => _items[id];

    public Task SaveAsync(FollowUp followUp, CancellationToken ct)
    {
        _items[followUp.Id] = followUp.ToSnapshot();
        return Task.CompletedTask;
    }
}
