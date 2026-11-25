namespace Zava.Scrum.Domain.Teams;

/// <summary>Where a team is in its life. Only <see cref="Active"/> teams are scheduled; an archived team is never hard deleted.</summary>
public enum TeamStatus
{
    /// <summary>Being set up. Not scheduled, not reachable by the bot.</summary>
    Draft,

    /// <summary>Running its ceremonies.</summary>
    Active,

    /// <summary>Temporarily not scheduled. Everything is still readable.</summary>
    Paused,

    /// <summary>Retired. Read-only history stays.</summary>
    Archived,
}
