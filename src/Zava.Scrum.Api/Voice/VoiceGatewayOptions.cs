using System.ComponentModel.DataAnnotations;

namespace Zava.Scrum.Api.Voice;

/// <summary>Binds <c>Voice__*</c>: floor-control timings (SPEC 11.3).</summary>
public sealed class VoiceGatewayOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Voice";

    /// <summary>Gets or sets seconds of off-topic talk before the agent parks it. DEMO: shortened from the spec's 60.</summary>
    [Range(3, 300)]
    public int ParkAfterSeconds { get; set; } = 60;

    /// <summary>Gets or sets silence after an update, in milliseconds. Indian and Costa Rican teams may need longer.</summary>
    [Range(300, 5000)]
    public int SilenceMs { get; set; } = 1200;

    /// <summary>Gets or sets the silence, in milliseconds, after which the agent may interject (park, warn). Keep it below <see cref="SilenceMs"/>.</summary>
    [Range(200, 3000)]
    public int PauseMs { get; set; } = 800;

    /// <summary>Gets or sets how long, in milliseconds, someone must keep talking over the agent before it stops. Shorter than a word, so a cough does not cut it off, and short enough that it does not talk over the person.</summary>
    [Range(150, 2000)]
    public int BargeInMs { get; set; } = 350;

    /// <summary>Gets or sets how long, in milliseconds, a member whose turn it is not must keep talking over the agent before it yields to them too.</summary>
    [Range(500, 5000)]
    public int SustainedBargeInMs { get; set; } = 1500;

    /// <summary>Gets or sets seconds to wait for a member to start before moving on.</summary>
    [Range(5, 300)]
    public int NoResponseSeconds { get; set; } = 25;

    /// <summary>Gets or sets whether voice preparation reuses authoritative comments and overlaps independent prompt/team reads (ADR 0044).</summary>
    /// <remarks>Corrections, rules and model-failure paths retain the reconciliation model. Disabled until controlled latency evidence accepts the candidate.</remarks>
    public bool OptimiseTurnPreparation { get; set; }
}
