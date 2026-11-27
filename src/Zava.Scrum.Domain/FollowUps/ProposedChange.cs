using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Domain.FollowUps;

/// <summary>The fields of a work item that an approved <see cref="ChangeOp.CreateWorkItem"/> creates.</summary>
/// <param name="Type">Work item type, for example <c>Task</c>.</param>
/// <param name="Title">Title.</param>
/// <param name="Description">Description.</param>
/// <param name="Tags">Tags to set.</param>
/// <param name="IterationPath">Iteration the item belongs to.</param>
/// <param name="AssignedTo">Display name or unique name of the assignee, if any.</param>
public sealed record NewWorkItem(string Type, string Title, string Description, IReadOnlyList<string> Tags, string IterationPath, string? AssignedTo);

/// <summary>A change proposed to Azure DevOps. It is only data until its follow-up is approved.</summary>
/// <param name="WorkItemId">Work item (always from the sprint). Zero for <see cref="ChangeOp.CreateWorkItem"/>, which has no id yet.</param>
/// <param name="Op">Kind of change.</param>
/// <param name="Value">Comment text, new state, <c>+Tag</c> / <c>-Tag</c>, iteration path, assignee, or the new item's title.</param>
/// <param name="Rationale">Why it is proposed, shown on the card.</param>
/// <param name="New">The item to create; set only for <see cref="ChangeOp.CreateWorkItem"/>.</param>
/// <param name="OwnerKey">For <see cref="ChangeOp.Assign"/>, the roster key of the new owner. DEMO: the sandbox has one real user, so the writer records ownership as a <c>demo-owner-{key}</c> tag.</param>
/// <param name="DemoReset">The exact approved baseline for an existing demo item; never used by ordinary ceremonies.</param>
public sealed record ProposedChange(int WorkItemId, ChangeOp Op, string Value, string Rationale, NewWorkItem? New = null, string? OwnerKey = null, DemoWorkItemReset? DemoReset = null)
{
    /// <summary>Proposes moving an item to another iteration.</summary>
    /// <param name="workItemId">The item.</param>
    /// <param name="iterationPath">Target iteration path.</param>
    /// <param name="rationale">Why.</param>
    /// <returns>The change.</returns>
    public static ProposedChange SetIteration(int workItemId, string iterationPath, string rationale) =>
        new(workItemId, ChangeOp.SetIteration, iterationPath, rationale);

    /// <summary>Proposes assigning an item.</summary>
    /// <param name="workItemId">The item.</param>
    /// <param name="user">Display name or unique name.</param>
    /// <param name="rationale">Why.</param>
    /// <param name="ownerKey">Roster key of the new owner, if known.</param>
    /// <returns>The change.</returns>
    public static ProposedChange Assign(int workItemId, string user, string rationale, string? ownerKey = null) =>
        new(workItemId, ChangeOp.Assign, user, rationale, null, ownerKey);

    /// <summary>Proposes creating a work item.</summary>
    /// <param name="item">What to create.</param>
    /// <param name="rationale">Why.</param>
    /// <param name="ownerKey">Roster key of the assignee, if known (DEMO: the writer records it as a <c>demo-owner-{key}</c> tag).</param>
    /// <returns>The change.</returns>
    public static ProposedChange Create(NewWorkItem item, string rationale, string? ownerKey = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new(0, ChangeOp.CreateWorkItem, item.Title, rationale, item, ownerKey);
    }
}

/// <summary>
/// A change that may be written to Azure DevOps. It can only be obtained from <see cref="FollowUp.GetApprovedChanges"/>,
/// which refuses unless a person approved the follow-up, so the write port cannot be handed an unapproved change.
/// </summary>
public sealed record ApprovedChange
{
    internal ApprovedChange(ProposedChange change, string approvedBy, DateOnly standupDate, CeremonyKind ceremony, ApprovalChannel channel, string correlationId)
    {
        WorkItemId = change.WorkItemId;
        Op = change.Op;
        Value = change.Value;
        New = change.New;
        OwnerKey = change.OwnerKey;
        DemoReset = change.DemoReset;
        ApprovedBy = approvedBy;
        StandupDate = standupDate;
        Ceremony = ceremony;
        Channel = channel;
        CorrelationId = correlationId;
    }

    /// <summary>Gets the work item. Zero when the change creates one.</summary>
    public int WorkItemId { get; }

    /// <summary>Gets the kind of change.</summary>
    public ChangeOp Op { get; }

    /// <summary>Gets the comment text, new state, tag instruction, iteration path, assignee or new title.</summary>
    public string Value { get; }

    /// <summary>Gets the item to create, for <see cref="ChangeOp.CreateWorkItem"/>.</summary>
    public NewWorkItem? New { get; }

    /// <summary>Gets the roster key of the new owner, for <see cref="ChangeOp.Assign"/>.</summary>
    public string? OwnerKey { get; }

    /// <summary>Gets the approved demo baseline, if this is a re-seed operation.</summary>
    public DemoWorkItemReset? DemoReset { get; }

    /// <summary>Gets who approved it.</summary>
    public string ApprovedBy { get; }

    /// <summary>Gets the date of the ceremony it came from.</summary>
    public DateOnly StandupDate { get; }

    /// <summary>Gets the ceremony it came from.</summary>
    public CeremonyKind Ceremony { get; }

    /// <summary>Gets where it was approved.</summary>
    public ApprovalChannel Channel { get; }

    /// <summary>Gets the correlation id (W3C trace id) of the session this change came from.</summary>
    public string CorrelationId { get; }

    /// <summary>Gets the attribution written with every change (SPEC 9.4). It ends with the correlation tag so the comment can be traced to its audit chain.</summary>
    public string Attribution =>
        $"Approved by {ApprovedBy} in {(Channel == ApprovalChannel.Portal ? "the portal" : "Teams")}, proposed by Zava Scrum Agent from the {StandupDate:yyyy-MM-dd} {Ceremony.Phrase()}. Correlation: {Domain.Common.Correlation.Tag(CorrelationId)}";
}
