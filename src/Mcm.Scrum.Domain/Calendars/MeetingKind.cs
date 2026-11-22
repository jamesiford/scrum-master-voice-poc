namespace Mcm.Scrum.Domain.Calendars;

/// <summary>Which ceremony a calendar meeting is.</summary>
public enum MeetingKind
{
    /// <summary>Sprint planning, on the first working day.</summary>
    Planning,

    /// <summary>The daily standup.</summary>
    Standup,

    /// <summary>Sprint review, on the last working day (only when the team has one).</summary>
    Review,

    /// <summary>Sprint retrospective, on the last working day.</summary>
    Retrospective,
}
