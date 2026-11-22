using System.ComponentModel.DataAnnotations;

namespace Mcm.Scrum.Infrastructure.Agents;

/// <summary>Binds <c>Foundry__*</c> configuration (SPEC 17). Leave <see cref="ProjectEndpoint"/> empty to run on deterministic questions only.</summary>
public sealed class FoundryOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Foundry";

    /// <summary>Gets or sets the Foundry project endpoint; its host also serves the OpenAI-compatible route.</summary>
    public string ProjectEndpoint { get; set; } = string.Empty;

    /// <summary>Gets or sets the reasoning model deployment name.</summary>
    public string ReasoningDeployment { get; set; } = string.Empty;

    /// <summary>Gets or sets the speech region (used from M2).</summary>
    public string SpeechRegion { get; set; } = string.Empty;

    /// <summary>Gets or sets the model call timeout in seconds.</summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 25;

    /// <summary>Gets a value indicating whether a model call can be attempted.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ProjectEndpoint) && !string.IsNullOrWhiteSpace(ReasoningDeployment);
}
