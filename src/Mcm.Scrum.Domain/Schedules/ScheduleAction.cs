namespace Mcm.Scrum.Domain.Schedules;

/// <summary>What the scheduler should do now.</summary>
public enum ScheduleAction
{
    /// <summary>Nothing is due.</summary>
    None,

    /// <summary>Prepare the standup (pre-brief and pre-brief card), ten minutes before the start.</summary>
    Prepare,

    /// <summary>Start the standup and join the meeting, one minute before the start.</summary>
    Start,
}