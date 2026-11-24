namespace Zava.Scrum.Domain.Standups;

/// <summary>Kinds of session events (SPEC 15, <c>events</c> container).</summary>
public enum StandupEventType
{
    /// <summary>Something said, by a member or the agent.</summary>
    Transcript,

    /// <summary>Something a member committed to.</summary>
    Commitment,

    /// <summary>A blocker.</summary>
    Blocker,

    /// <summary>A decision.</summary>
    Decision,

    /// <summary>A topic taken offline.</summary>
    ParkingLot,

    /// <summary>A member is absent.</summary>
    Absent,

    /// <summary>A tool call made by the agent.</summary>
    ToolCall,

    /// <summary>A grounded clarifying question the agent asked a member.</summary>
    Question,

    /// <summary>A call or bridge status change, such as waiting in the Teams lobby.</summary>
    Bridge,

    /// <summary>A member was skipped for now (stepped away, or silent) and will be asked again at the end.</summary>
    Deferred,

    /// <summary>The agent came back to a deferred member.</summary>
    CircledBack,

    /// <summary>A member was present but did not respond when called. Different from absent.</summary>
    NoResponse,

    /// <summary>One member is giving another's update on their behalf.</summary>
    Proxy,

    /// <summary>A member corrected an earlier statement.</summary>
    Correction,

    /// <summary>A retrospective note: what to start, stop or keep (<see cref="StandupEvent.Category"/>; null until classified).</summary>
    RetroItem,

    /// <summary>A retrospective action item (<see cref="StandupEvent.OwnerMemberId"/> is who owns it).</summary>
    ActionItem,

    /// <summary>A named group of retrospective notes (<see cref="StandupEvent.Text"/> is the name; <c>WorkItemIds</c> are unused).</summary>
    Theme,

    /// <summary>The ceremony moved to a new phase (retrospective: themes read-back, or an action turn).</summary>
    Phase,

    /// <summary>A member said an item's finish or target date moves. Recorded for the Scrum Master; nothing in Azure DevOps changes.</summary>
    DateChange,
}
