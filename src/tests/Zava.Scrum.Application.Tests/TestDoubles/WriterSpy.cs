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

    public bool Fail { get; set; }

    public Task<Result> ApplyAsync(ApprovedChange change, CancellationToken ct)
    {
        if (Fail)
        {
            return Task.FromResult(Result.Failure(Error.ExternalRejected("ado.write_rejected", "rejected")));
        }

        Written.Add(change);
        return Task.FromResult(Result.Success());
    }
}

/// <summary>Returns a fixed next iteration path.</summary>
internal sealed class FakeIterationReader(string? next, IterationInfo? info = null) : IIterationReader
{
    public Task<string?> GetNextIterationPathAsync(Team team, CancellationToken ct) => Task.FromResult(next);

    public Task<IterationInfo?> FindIterationAsync(Team team, string? name, CancellationToken ct) => Task.FromResult(info);
}
