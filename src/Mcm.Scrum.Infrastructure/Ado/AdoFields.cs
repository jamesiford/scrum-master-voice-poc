namespace Mcm.Scrum.Infrastructure.Ado;

/// <summary>Azure DevOps field reference names. One source of truth (DRY); an Agile project is assumed, see SPEC 9.3 for the field map.</summary>
public static class AdoFields
{
    /// <summary>Work item id.</summary>
    public const string Id = "System.Id";

    /// <summary>Title.</summary>
    public const string Title = "System.Title";

    /// <summary>Work item type.</summary>
    public const string WorkItemType = "System.WorkItemType";

    /// <summary>State.</summary>
    public const string State = "System.State";

    /// <summary>Assignee identity.</summary>
    public const string AssignedTo = "System.AssignedTo";

    /// <summary>Tags, semicolon separated.</summary>
    public const string Tags = "System.Tags";

    /// <summary>Discussion history; writing it adds a comment.</summary>
    public const string History = "System.History";

    /// <summary>Last change.</summary>
    public const string ChangedDate = "System.ChangedDate";

    /// <summary>When the state last changed.</summary>
    public const string StateChangeDate = "Microsoft.VSTS.Common.StateChangeDate";

    /// <summary>Story points.</summary>
    public const string StoryPoints = "Microsoft.VSTS.Scheduling.StoryPoints";

    /// <summary>Remaining hours.</summary>
    public const string RemainingWork = "Microsoft.VSTS.Scheduling.RemainingWork";

    /// <summary>Finish date; Agile Tasks and Stories carry this, not TargetDate.</summary>
    public const string FinishDate = "Microsoft.VSTS.Scheduling.FinishDate";

    /// <summary>Target date; Agile Epics and Features carry this. Read as a fallback for other templates.</summary>
    public const string TargetDate = "Microsoft.VSTS.Scheduling.TargetDate";

    /// <summary>Iteration path.</summary>
    public const string IterationPath = "System.IterationPath";

    /// <summary>Fields requested for every sprint item.</summary>
    public static readonly IReadOnlyList<string> SprintItemFields =
    [
        Id, Title, WorkItemType, State, AssignedTo, Tags, ChangedDate, StateChangeDate, StoryPoints, RemainingWork, FinishDate, TargetDate,
    ];
}
