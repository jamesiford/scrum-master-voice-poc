namespace Mcm.Scrum.Domain.Standups;

/// <summary>One member's slice of the pre-brief.</summary>
/// <param name="MemberId">Member id.</param>
/// <param name="DisplayName">Name the agent says.</param>
/// <param name="Items">Open items this member owns.</param>
/// <param name="SuggestedQuestion">A grounded question for the standup.</param>
public sealed record MemberBrief(
    string MemberId,
    string DisplayName,
    IReadOnlyList<BriefItem> Items,
    string SuggestedQuestion);
