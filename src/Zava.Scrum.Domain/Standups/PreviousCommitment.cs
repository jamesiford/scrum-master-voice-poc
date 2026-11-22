namespace Mcm.Scrum.Domain.Standups;

/// <summary>Something a member said they would do in an earlier standup, and that is still open (M9-R3).</summary>
/// <param name="WorkItemId">The item (always from the current sprint).</param>
/// <param name="Text">What they committed to.</param>
/// <param name="StandupsAgo">How many standups ago they said it; 1 is the previous standup.</param>
public sealed record PreviousCommitment(int WorkItemId, string Text, int StandupsAgo)
{
    /// <summary>Gets a value indicating whether it was missed: still open two or more standups after it was made.</summary>
    public bool IsMissed => StandupsAgo >= CommitmentCarryOver.MissedAfterStandups;
}
