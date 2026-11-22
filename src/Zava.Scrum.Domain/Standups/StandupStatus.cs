namespace Zava.Scrum.Domain.Standups;

/// <summary>Lifecycle of a <see cref="StandupSession"/>.</summary>
public enum StandupStatus
{
    /// <summary>Pre-brief built, standup not started.</summary>
    Prepared,

    /// <summary>Standup is running.</summary>
    Live,

    /// <summary>Standup finished.</summary>
    Ended,
}
