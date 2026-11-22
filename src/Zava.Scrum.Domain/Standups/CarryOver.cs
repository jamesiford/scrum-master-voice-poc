namespace Zava.Scrum.Domain.Standups;

/// <summary>What earlier standups leave behind for the next pre-brief.</summary>
/// <param name="Commitments">Open commitments per member id.</param>
/// <param name="Blocked">Blocked items with their named unblocker.</param>
public sealed record CarryOver(IReadOnlyDictionary<string, IReadOnlyList<PreviousCommitment>> Commitments, IReadOnlyList<StillBlocked> Blocked)
{
    /// <summary>Gets an empty carry-over.</summary>
    public static CarryOver None { get; } = new(new Dictionary<string, IReadOnlyList<PreviousCommitment>>(), []);
}
