namespace Mcm.Scrum.Domain.Flags;

/// <summary>A condition worth asking about in the standup (SPEC 9.3).</summary>
public enum Flag
{
    /// <summary>In progress too long in its current state.</summary>
    Stale,

    /// <summary>Tagged <c>Blocked</c>.</summary>
    Blocked,

    /// <summary>Past its due date and not done.</summary>
    Overdue,

    /// <summary>Nobody owns it.</summary>
    Unassigned,

    /// <summary>In progress but untouched since yesterday.</summary>
    NoRecentUpdate,
}
