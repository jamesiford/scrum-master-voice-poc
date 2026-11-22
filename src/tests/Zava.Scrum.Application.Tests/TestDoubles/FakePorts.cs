using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Tests.TestDoubles;

internal sealed class FakeWorkItemReader(Result<SprintSnapshot> result) : IWorkItemReader
{
    public Task<Result<SprintSnapshot>> GetCurrentSprintAsync(Team team, CancellationToken ct) => Task.FromResult(result);
}

internal sealed class FakeBacklogRiskAgent(BacklogRiskSuggestion? suggestion) : IBacklogRiskAgent
{
    public Task<BacklogRiskSuggestion?> SuggestAsync(PreBrief brief, CancellationToken ct) => Task.FromResult(suggestion);
}

internal sealed class FakeRosterSource(IReadOnlyList<Member> members) : IRosterSource
{
    public Task<IReadOnlyList<Member>> GetRosterAsync(Team team, CancellationToken ct) => Task.FromResult(members);
}
