namespace Zava.Scrum.Domain.Standups;

/// <summary>Which column a retrospective note belongs to. The format only changes the column titles (see <see cref="RetroFormats"/>).</summary>
public enum RetroCategory
{
    /// <summary>Something the team should start, or is missing.</summary>
    Start,

    /// <summary>Something the team should stop, or do better.</summary>
    Stop,

    /// <summary>Something that went well and should continue.</summary>
    Continue,
}
