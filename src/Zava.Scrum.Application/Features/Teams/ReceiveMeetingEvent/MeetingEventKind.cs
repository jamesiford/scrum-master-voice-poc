namespace Zava.Scrum.Application.Features.Teams.ReceiveMeetingEvent;

/// <summary>The two Teams lifecycle events supported by the Azure-managed bot (ADR 0015 S2).</summary>
public enum MeetingEventKind
{
    /// <summary>The scheduled meeting started. This does not start a ceremony.</summary>
    Started,

    /// <summary>The scheduled meeting ended. A matching active ceremony ends through its normal domain lifecycle.</summary>
    Ended,
}
