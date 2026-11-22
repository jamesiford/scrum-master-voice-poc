namespace Zava.Scrum.Domain.FollowUps;

/// <summary>What a follow-up is for (SPEC 15).</summary>
public enum FollowUpKind
{
    /// <summary>What the agent heard from a member, with proposed Azure DevOps updates.</summary>
    Followup,

    /// <summary>Asks an absent member for their update.</summary>
    Absent,

    /// <summary>Tells the human Scrum Master a blocker needs help.</summary>
    Escalation,

    /// <summary>Asks a member to help unblock a teammate's item (M9-R2).</summary>
    Unblock,

    /// <summary>Sprint planning pack for the Scrum Master.</summary>
    PlanningPack,

    /// <summary>Retrospective actions for the Scrum Master.</summary>
    RetroActions,

    /// <summary>Sprint review pack for the Scrum Master.</summary>
    ReviewPack,
}

/// <summary>Approval lifecycle of a follow-up.</summary>
public enum FollowUpStatus
{
    /// <summary>Created, not yet sent.</summary>
    Pending,

    /// <summary>Card sent, waiting for a decision.</summary>
    Sent,

    /// <summary>A person approved it. Only now may proposed changes be written (customer requirement F7).</summary>
    Approved,

    /// <summary>A person rejected it. Never written.</summary>
    Rejected,

    /// <summary>Approved and written to Azure DevOps.</summary>
    Applied,
}

/// <summary>Kinds of Azure DevOps change a follow-up may propose (SPEC 9.4).</summary>
public enum ChangeOp
{
    /// <summary>Add a comment to the work item history.</summary>
    Comment,

    /// <summary>Change the state; the value is the new state.</summary>
    State,

    /// <summary>Add or remove a tag; the value is <c>+Name</c> or <c>-Name</c>.</summary>
    Tag,

    /// <summary>Move the item to another iteration; the value is the iteration path.</summary>
    SetIteration,

    /// <summary>Assign the item; the value is the assignee's display or unique name.</summary>
    Assign,

    /// <summary>Create a work item from <see cref="ProposedChange.New"/>.</summary>
    CreateWorkItem,
}
