namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>Selects the existing ACS canvas or the native media bot's screen-sharing adapter.</summary>
public sealed class BoardOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Board";

    /// <summary>Gets or sets Share (ACS canvas, default) or MediaShare (native VBSS).</summary>
    public string Mode { get; set; } = "Share";
}
