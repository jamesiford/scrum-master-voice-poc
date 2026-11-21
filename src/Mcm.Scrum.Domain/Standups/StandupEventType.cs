namespace Mcm.Scrum.Domain.Standups;

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
}
