using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Members;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Teams;
using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Application.Tests.TestDoubles;

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
