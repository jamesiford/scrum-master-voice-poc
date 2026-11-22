namespace Zava.Scrum.Domain.Voice;

/// <summary>Why the agent is speaking. Each reason has its own turn instruction.</summary>
public enum SpeakReason
{
    /// <summary>Greeting and sprint goal.</summary>
    Open,

    /// <summary>Inviting the next member.</summary>
    CallOn,

    /// <summary>Announcing an absent member.</summary>
    Absent,

    /// <summary>Asking the one clarifying question.</summary>
    Clarify,

    /// <summary>"Let's take that offline."</summary>
    Park,

    /// <summary>Three minutes (80 percent of the timebox) have passed.</summary>
    TimeWarning,

    /// <summary>Someone addressed the agent by name.</summary>
    Addressed,

    /// <summary>Closing recap.</summary>
    Recap,

    /// <summary>Acknowledging that a member stepped away; the agent will circle back.</summary>
    StepAway,

    /// <summary>Noting that a present member did not respond.</summary>
    NoResponse,

    /// <summary>Returning to a member who was skipped earlier.</summary>
    CircleBack,

    /// <summary>Reading the grouped retrospective notes back to the team.</summary>
    RetroThemes,

    /// <summary>Asking a member who just raised a blocker who can help unblock it (M9-R2).</summary>
    Unblocker,
}
