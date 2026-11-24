using System.ComponentModel.DataAnnotations;

namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>Binds <c>UtteranceAnalysis__*</c> configuration: whether a decision model reads each utterance in the live standup.</summary>
public sealed class UtteranceAnalysisOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "UtteranceAnalysis";

    /// <summary>Gets or sets the mode: <c>Model</c> asks the decision model first, <c>Rules</c> uses only the plain rules (the pre-M10 behaviour).</summary>
    public string Mode { get; set; } = "Model";

    /// <summary>Gets or sets the engine: <c>Decision</c> uses a Microsoft-Decision deployment (typed questions, probabilities, about a second); <c>Chat</c> uses the reasoning chat model (about three seconds).</summary>
    public string Engine { get; set; } = "Decision";

    /// <summary>Gets or sets the Microsoft-Decision-1.1 deployment name used when <see cref="Engine"/> is <c>Decision</c>.</summary>
    public string DecisionDeployment { get; set; } = string.Empty;

    /// <summary>Gets or sets the probability at or above which a yes/no decision (an item discussed, a comment kind) counts as true.</summary>
    [Range(0.5, 0.99)]
    public double Threshold { get; set; } = 0.7;

    /// <summary>Gets a value indicating whether the decision model is used instead of the chat model.</summary>
    public bool UseDecisionEngine => string.Equals(Engine, "Decision", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets or sets the latency budget in milliseconds; a slower answer is dropped and the rules decide, so the agent never waits longer than this.</summary>
    [Range(200, 10000)]
    public int TimeoutMs { get; set; } = 2500;

    /// <summary>Gets or sets an optional faster chat deployment for the <c>Chat</c> engine; empty uses <see cref="FoundryOptions.ReasoningDeployment"/>.</summary>
    public string Deployment { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether the model is asked at all.</summary>
    public bool UseModel => string.Equals(Mode, "Model", StringComparison.OrdinalIgnoreCase);
}
