namespace Mcm.Scrum.Domain.Voice;

/// <summary>Where the standup conversation is (SPEC 11.3).</summary>
public enum FloorState
{
    /// <summary>Not started.</summary>
    Idle,

    /// <summary>The agent is greeting the team.</summary>
    Opening,

    /// <summary>Waiting for the application to pick the next speaker.</summary>
    Advancing,

    /// <summary>The agent is inviting (or skipping, when absent) a member.</summary>
    CallingOn,

    /// <summary>A member holds the floor and the agent stays silent.</summary>
    Listening,

    /// <summary>The member finished; the update is being checked against the brief.</summary>
    Reconciling,

    /// <summary>The agent is asking its one clarifying question.</summary>
    Clarifying,

    /// <summary>The agent briefly interjects (parking lot, time warning, or when addressed).</summary>
    Interjecting,

    /// <summary>The agent is giving the closing recap.</summary>
    Recap,

    /// <summary>The standup is over.</summary>
    Closed,

    /// <summary>A human took over; the agent only listens.</summary>
    HumanOverride,

    /// <summary>Muted from the portal.</summary>
    Paused,

    /// <summary>The agent is speaking to the whole team, not to a member (for example reading back retrospective themes).</summary>
    Announcing,

    /// <summary>The agent is saying it will skip the current member and come back, after which the next member is called.</summary>
    Deferring,
}
