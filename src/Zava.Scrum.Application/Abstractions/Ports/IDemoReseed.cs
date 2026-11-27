using Zava.Scrum.Application.Features.Demo.ReseedDemo;
using Zava.Scrum.Application.Features.Demo.OptInDemoScope;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Stages and atomically publishes demo state; Azure DevOps remains a separately approved recoverable phase.</summary>
public interface IDemoReseed
{
    /// <summary>Stages a guarded operation and its pending Azure DevOps reset follow-up.</summary>
    Task<Result<Guid>> StageAsync(string teamId, ActorIdentity actor, CancellationToken ct);

    /// <summary>Publishes the local snapshot only once its approved Azure DevOps follow-up was applied.</summary>
    Task<Result<Guid>> CommitAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct);

    /// <summary>Reads an operation scoped to the requested team and tenant.</summary>
    Task<Result<DemoReseedDto>> GetAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct);

    /// <summary>Releases an undecided/rejected operation's guard. Approved or partially applied ADO work must be resumed, never silently rolled back.</summary>
    Task<Result<Guid>> AbortAsync(string teamId, Guid operationId, ActorIdentity actor, CancellationToken ct);

    /// <summary>Atomically marks only the verified seed and explicitly named debris records in the approved manifest.</summary>
    Task<Result> OptInAsync(DemoResetScopeManifest manifest, ActorIdentity actor, CancellationToken ct);

    /// <summary>Clears only allowlisted, demo-marked records in the configured tenant; never globally deletes collections or sessions.</summary>
    Task<Result> ClearAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Resolves the journal-owned team before any caller-supplied team override is considered by authorisation.</summary>
    Task<Result<string>> LocateTeamAsync(Guid operationId, CancellationToken ct);

}
