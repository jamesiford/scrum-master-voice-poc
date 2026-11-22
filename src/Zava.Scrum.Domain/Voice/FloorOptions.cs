namespace Zava.Scrum.Domain.Voice;

/// <summary>Tunable floor-control timings (SPEC 11.3).</summary>
/// <param name="StandupLength">Total timebox.</param>
/// <param name="SilenceMs">Silence after the last speech before the update counts as finished.</param>
/// <param name="ParkAfterOffTopicMs">Off-holder speech needed before the agent parks the topic.</param>
/// <param name="NoResponseMs">Wait this long for a member to start talking before moving on.</param>
/// <param name="WarnFraction">Fraction of the timebox at which to warn.</param>
/// <param name="MaxClarifications">How many clarifying questions one member can be asked, one per uncovered flagged item.</param>
/// <param name="PauseMs">Silence that counts as a pause in which the agent may interject.</param>
public sealed record FloorOptions(
    TimeSpan StandupLength,
    int SilenceMs = 1200,
    int ParkAfterOffTopicMs = 60_000,
    int NoResponseMs = 25_000,
    double WarnFraction = 0.8,
    int PauseMs = 800,
    int MaxClarifications = 3);
