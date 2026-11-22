namespace Zava.Scrum.Domain.Planning;

/// <summary>An open item that is not ready to be planned or committed, with the reasons (Definition of Ready).</summary>
/// <param name="WorkItemId">The item.</param>
/// <param name="Title">Title.</param>
/// <param name="Reasons">What is missing.</param>
public sealed record ReadinessGap(int WorkItemId, string Title, IReadOnlyList<string> Reasons);

/// <summary>An open item that would carry over if the sprint ended now.</summary>
/// <param name="WorkItemId">The item.</param>
/// <param name="Title">Title.</param>
/// <param name="State">Current state.</param>
/// <param name="Points">Story points, if any.</param>
/// <param name="OwnerName">Owner, or <see langword="null"/>.</param>
public sealed record CarryOverItem(int WorkItemId, string Title, string State, double? Points, string? OwnerName);

/// <summary>One member's open load.</summary>
/// <param name="MemberId">Member id.</param>
/// <param name="DisplayName">Name.</param>
/// <param name="OpenItems">Open items owned.</param>
/// <param name="OpenPoints">Story points still open.</param>
/// <param name="RemainingHours">Remaining task hours.</param>
public sealed record MemberLoad(string MemberId, string DisplayName, int OpenItems, double OpenPoints, double RemainingHours);

/// <summary>A suggested owner for an unassigned item. A suggestion only: nothing is assigned without an approved follow-up.</summary>
/// <param name="WorkItemId">The item.</param>
/// <param name="Title">Title.</param>
/// <param name="MemberId">Suggested member.</param>
/// <param name="DisplayName">Their name.</param>
/// <param name="Reason">Why, in plain words.</param>
public sealed record AssignmentSuggestion(int WorkItemId, string Title, string MemberId, string DisplayName, string Reason);

/// <summary>Everything the Scrum Master needs to prepare the next planning session, computed in code (rule 3).</summary>
/// <param name="Loads">Open load per member.</param>
/// <param name="CarryOver">Open items that would carry over.</param>
/// <param name="NotReady">Open items failing the Definition of Ready.</param>
/// <param name="Suggestions">Owner suggestions for unassigned items.</param>
public sealed record PlanningPrep(
    IReadOnlyList<MemberLoad> Loads,
    IReadOnlyList<CarryOverItem> CarryOver,
    IReadOnlyList<ReadinessGap> NotReady,
    IReadOnlyList<AssignmentSuggestion> Suggestions);
