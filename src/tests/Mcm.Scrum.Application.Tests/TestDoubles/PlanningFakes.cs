using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Kpis;
using Mcm.Scrum.Domain.Planning;
using Mcm.Scrum.Domain.Teams;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Application.Tests.TestDoubles;

internal sealed class InMemoryPlanningPackRepository : IPlanningPackRepository
{
    private readonly Dictionary<Guid, PlanningPackData> _items = [];

    public Task<PlanningPack?> GetAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_items.TryGetValue(id, out var d) ? PlanningPack.Restore(d) : null);

    public Task<PlanningPack?> GetLatestAsync(string teamId, CancellationToken ct) =>
        Task.FromResult(_items.Values.Where(d => d.TeamId == teamId).OrderByDescending(d => d.CreatedAt).Select(PlanningPack.Restore).FirstOrDefault());

    public Task SaveAsync(PlanningPack pack, CancellationToken ct)
    {
        _items[pack.Id] = pack.Data;
        return Task.CompletedTask;
    }
}

internal sealed class FakeBacklogReader(IReadOnlyList<WorkItem> items) : IBacklogReader
{
    public Task<Result<IReadOnlyList<WorkItem>>> GetBacklogAsync(Team team, CancellationToken ct) =>
        Task.FromResult(Result<IReadOnlyList<WorkItem>>.Success(items));
}

internal sealed class FakeKpiHistorySource(params double[] completed) : IKpiHistorySource
{
    public Task<KpiHistory> GetAsync(string teamId, CancellationToken ct)
    {
        var start = new DateOnly(2025, 1, 1);
        IReadOnlyList<SprintHistory> sprints = [.. completed.Select((c, i) => new SprintHistory($"S{i}", start.AddDays(i * 14), start.AddDays((i * 14) + 13), c, c, 0, 0, 0, 0, 0, 0, 0))];
        return Task.FromResult(new KpiHistory(sprints, Illustrative: true));
    }
}
