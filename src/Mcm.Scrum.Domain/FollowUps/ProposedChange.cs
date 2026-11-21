namespace Mcm.Scrum.Domain.FollowUps;

/// <summary>A change proposed to Azure DevOps. It is only data until its follow-up is approved.</summary>
/// <param name="WorkItemId">Work item (always from the sprint).</param>
/// <param name="Op">Kind of change.</param>
/// <param name="Value">Comment text, new state, or <c>+Tag</c> / <c>-Tag</c>.</param>
/// <param name="Rationale">Why it is proposed, shown on the card.</param>
public sealed record ProposedChange(int WorkItemId, ChangeOp Op, string Value, string Rationale);

/// <summary>
/// A change that may be written to Azure DevOps. It can only be obtained from <see cref="FollowUp.GetApprovedChanges"/>,
/// which refuses unless a person approved the follow-up, so the write port cannot be handed an unapproved change.
/// </summary>
public sealed record ApprovedChange
{
    internal ApprovedChange(ProposedChange change, string approvedBy, DateOnly standupDate)
    {
        WorkItemId = change.WorkItemId;
        Op = change.Op;
        Value = change.Value;
        ApprovedBy = approvedBy;
        StandupDate = standupDate;
    }

    /// <summary>Gets the work item.</summary>
    public int WorkItemId { get; }

    /// <summary>Gets the kind of change.</summary>
    public ChangeOp Op { get; }

    /// <summary>Gets the comment text, new state, or tag instruction.</summary>
    public string Value { get; }

    /// <summary>Gets who approved it.</summary>
    public string ApprovedBy { get; }

    /// <summary>Gets the date of the standup it came from.</summary>
    public DateOnly StandupDate { get; }

    /// <summary>Gets the attribution written with every change (SPEC 9.4).</summary>
    public string Attribution =>
        $"Approved by {ApprovedBy} in Teams, proposed by MCM Scrum Agent from the {StandupDate:yyyy-MM-dd} standup.";
}
