using System.Text.Json;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;
using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>A single publication document, shared by collection and session adapters. Legacy data is never physically deleted.</summary>
internal sealed class DemoState
{
    /// <summary>Gets or sets the reset epoch used to refuse stale pre-reset collection writes.</summary>
    public long Epoch { get; set; }

    /// <summary>Gets the item schema version; migration runs once before request handling.</summary>
    public int RecordStorageVersion { get; set; }

    /// <summary>Gets the visible per-team record generation, switched atomically on reset.</summary>
    public Dictionary<string, long> RecordGenerations { get; init; } = [];

    /// <summary>Gets reset cutoffs that refuse stale unsaved records.</summary>
    public Dictionary<string, DateTimeOffset> RecordCutoffs { get; init; } = [];

    /// <summary>Gets the canonical collection overrides.</summary>
    public Dictionary<string, JsonElement> Collections { get; init; } = [];

    /// <summary>Gets the authoritative session sets for teams that have completed a reset.</summary>
    public Dictionary<string, List<StandupSessionSnapshot>> Sessions { get; init; } = [];

    /// <summary>Gets persistent session-id tombstones, so an old in-flight start/save cannot resurrect a previous take after publication.</summary>
    public Dictionary<string, HashSet<Guid>> SessionTombstones { get; init; } = [];

    /// <summary>Gets the preparation cutoff for each reset team, blocking a pre-reset unsaved preparation after publication.</summary>
    public Dictionary<string, DateTimeOffset> SessionCutoffs { get; init; } = [];

    /// <summary>Gets the durable operation journal.</summary>
    public Dictionary<Guid, DemoReseedOperation> Operations { get; init; } = [];
}

/// <summary>Staged reset inputs and an approval snapshot retained after publication for recovery and audit.</summary>
internal sealed record DemoReseedOperation(
    Guid Id, Guid TenantId, Team Target, IReadOnlyList<Team> Scope, IReadOnlyList<Member> Roster,
    FollowUpSnapshot Approval, bool Completed = false, bool Aborted = false, IReadOnlyList<Guid>? SessionIds = null)
{
    /// <summary>Returns the public recovery contract without staged data or identities.</summary>
    public DemoReseedDto ToDto() => new(Id, Target.Id, Approval.Id, Aborted ? "Aborted" : Completed ? "Completed" : "AwaitingApprovalOrApply",
        Scope.Select(t => t.Id).ToList(),
        "One local/Cosmos publication document atomically switches roster, team registry, sessions and follow-ups. Azure DevOps applies first through approved follow-ups; retry that phase, then commit. No distributed rollback or comment deletion.");
}
