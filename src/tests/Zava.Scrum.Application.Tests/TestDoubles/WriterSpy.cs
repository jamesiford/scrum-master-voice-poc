using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Tests.TestDoubles;

/// <summary>Records every approved change handed to the writer; <see cref="Fail"/> makes it reject them.</summary>
internal sealed class WriterSpy : IWorkItemWriter
{
    public List<ApprovedChange> Written { get; } = [];

    /// <summary>The delegated token each write carried.</summary>
    public List<string> Tokens { get; } = [];

    public bool Fail { get; set; }

    public Task<Result<WorkItemRevision>> ApplyAsync(ApprovedChange change, AdoSettings target, DelegatedAdoAccess access, CancellationToken ct)
    {
        if (Fail)
        {
            return Task.FromResult(Result<WorkItemRevision>.Failure(Error.ExternalRejected("ado.write_rejected", "rejected")));
        }

        Written.Add(change);
        Tokens.Add(access.AccessToken);
        return Task.FromResult(Result<WorkItemRevision>.Success(new WorkItemRevision(change.WorkItemId, Written.Count + 1)));
    }
}

/// <summary>Returns a fixed next iteration path.</summary>
internal sealed class FakeIterationReader(string? next, IterationInfo? info = null) : IIterationReader
{
    public Task<string?> GetNextIterationPathAsync(Team team, CancellationToken ct) => Task.FromResult(next);

    public Task<IterationInfo?> FindIterationAsync(Team team, string? name, CancellationToken ct) => Task.FromResult(info);
}
