namespace Zava.Scrum.Domain.Standups;

/// <summary>One member's slice of the pre-brief.</summary>
/// <param name="MemberId">Member id.</param>
/// <param name="DisplayName">Name the agent says.</param>
/// <param name="Items">Open items this member owns.</param>
/// <param name="SuggestedQuestion">A grounded question for the standup.</param>
/// <param name="PreviousCommitments">What they said they would do last time and that is still open (M9-R3).</param>
/// <param name="HelpRequests">Blocked items of teammates that this member was named to help with (M9-R2).</param>
public sealed record MemberBrief(
    string MemberId,
    string DisplayName,
    IReadOnlyList<BriefItem> Items,
    string SuggestedQuestion,
    IReadOnlyList<PreviousCommitment>? PreviousCommitments = null,
    IReadOnlyList<HelpRequest>? HelpRequests = null);
