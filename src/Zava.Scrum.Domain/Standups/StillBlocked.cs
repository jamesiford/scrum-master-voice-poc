namespace Zava.Scrum.Domain.Standups;

/// <summary>An item that was blocked in an earlier standup, with the person named to unblock it.</summary>
/// <param name="WorkItemId">The item.</param>
/// <param name="UnblockerMemberId">The roster member named.</param>
public sealed record StillBlocked(int WorkItemId, string UnblockerMemberId);
