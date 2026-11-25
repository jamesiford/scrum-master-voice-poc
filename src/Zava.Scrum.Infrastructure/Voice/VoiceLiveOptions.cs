using System.ComponentModel.DataAnnotations;

namespace Zava.Scrum.Infrastructure.Voice;

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

    /// <summary>
    /// Gets or sets the <c>invocations_ws</c> endpoint of the hosted agent that relays to Voice Live (ADR 0021). Empty opens Voice Live directly.
    /// Both are real adapters of the same protocol; the relay forwards frames unchanged.
    /// </summary>
    public string RelayEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <c>invocations_ws</c> endpoint of a second hosted agent that runs the candidate version (ADR 0021). The platform refuses a
    /// percentage split on one agent endpoint (a multi-arm selector is restricted to the Agents Optimization service), so the canary is two agents and
    /// the API chooses per session. Empty means no canary.
    /// </summary>
    public string CanaryEndpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the share of sessions, 0 to 100, that use <see cref="CanaryEndpoint"/>. Zero is the one-step rollback.</summary>
    public int CanaryPercent { get; set; }

    /// <summary>Gets or sets the teams that may use the canary: a list, or a comma-separated string in the environment. Empty means every team.</summary>
    public string[] CanaryTeams { get; set; } = [];

    /// <summary>Gets or sets the hosted agent session id the relay connection reattaches to. Empty starts a new session for every connection.</summary>
    public string RelaySessionId { get; set; } = string.Empty;

    /// <summary>Gets or sets the Foundry voice agent name. When set, the Facilitator runs in <c>FoundryAgent</c> mode instead of model-direct (SPEC M8).</summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>Gets or sets the Foundry project that hosts <see cref="AgentName"/>.</summary>
    public string AgentProjectName { get; set; } = string.Empty;
}
