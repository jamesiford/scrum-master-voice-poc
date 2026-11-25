using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Tests.TestDoubles;

internal sealed class InMemoryTeamRepository : ITeamRepository
{
    private readonly Dictionary<string, Team> _items = [];

    public Task<Team?> GetAsync(string id, CancellationToken ct) => Task.FromResult(_items.GetValueOrDefault(id));

    public Task<IReadOnlyList<Team>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Team>>([.. _items.Values]);

    public Task SaveAsync(Team team, CancellationToken ct)
    {
        _items[team.Id] = team;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryMemberRepository : IMemberRepository
{
    private readonly Dictionary<string, IReadOnlyList<Member>> _items = [];

    public Task<IReadOnlyList<Member>> ListAsync(string teamId, CancellationToken ct) =>
        Task.FromResult(_items.GetValueOrDefault(teamId) ?? []);

    public Task ReplaceAsync(string teamId, IReadOnlyList<Member> members, CancellationToken ct)
    {
        _items[teamId] = members;
        return Task.CompletedTask;
    }
}
