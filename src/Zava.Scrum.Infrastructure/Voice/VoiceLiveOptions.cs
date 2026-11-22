using System.ComponentModel.DataAnnotations;

namespace Mcm.Scrum.Infrastructure.Voice;

/// <summary>Binds <c>VoiceLive__*</c> configuration (SPEC 17).</summary>
public sealed class VoiceLiveOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "VoiceLive";

    /// <summary>Gets or sets the websocket endpoint. Empty derives it from <c>Foundry:ProjectEndpoint</c>.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the Voice Live API version.</summary>
    [Required]
    public string ApiVersion { get; set; } = "2026-04-10";

    /// <summary>Gets or sets the realtime model deployment.</summary>
    [Required]
    public string Model { get; set; } = "gpt-realtime-2.1";

    /// <summary>Gets or sets the agent voice.</summary>
    [Required]
    public string Voice { get; set; } = "en-US-Ava:DragonHDLatestNeural";

    /// <summary>Gets or sets the transcription language.</summary>
    [Required]
    public string Language { get; set; } = "en-US";

    /// <summary>Gets or sets the Foundry voice agent name. When set, the Facilitator runs in <c>FoundryAgent</c> mode instead of model-direct (SPEC M8).</summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>Gets or sets the Foundry project that hosts <see cref="AgentName"/>.</summary>
    public string AgentProjectName { get; set; } = string.Empty;
}
