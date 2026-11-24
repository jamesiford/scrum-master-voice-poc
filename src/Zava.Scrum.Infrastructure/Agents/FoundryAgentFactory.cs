using Azure.AI.Projects;
using Azure.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>
/// Builds code-defined Responses agents over the Foundry project (Azure.AI.Projects 2.x). No server-side agent resource is created:
/// the prompt is read from <c>agents/instructions</c> at call time, so it can be tuned during a demo without a rebuild (rule 5).
/// Each agent is wrapped with OpenTelemetry so model calls appear in Application Insights.
/// </summary>
/// <remarks>
/// VERIFY: https://learn.microsoft.com/agent-framework/integrations/by-component/model-providers/microsoft-foundry (<c>AIProjectClient.AsAIAgent</c>).
/// PROD: Foundry hosted agents with their own identity and versions.
/// </remarks>
public sealed class FoundryAgentFactory : IFoundryAgentFactory
{
    /// <summary>The OpenTelemetry source name of agent calls; subscribe to it in the telemetry setup.</summary>
    public const string TelemetrySource = "Zava.Scrum.Agents";

    private readonly Lazy<AIProjectClient> _client;
    private readonly string _model;

    /// <summary>Initializes a new instance of the <see cref="FoundryAgentFactory"/> class.</summary>
    /// <param name="options">Foundry options (project endpoint and reasoning deployment).</param>
    /// <param name="credential">The Entra credential (no keys).</param>
    public FoundryAgentFactory(IOptions<FoundryOptions> options, TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(credential);
        var o = options.Value;
        _model = o.ReasoningDeployment;
        _client = new Lazy<AIProjectClient>(() => new AIProjectClient(new Uri(o.ProjectEndpoint), credential));
    }

    /// <inheritdoc />
    public AIAgent Create(string name, string instructions) => Create(name, instructions, null);

    /// <inheritdoc />
    public AIAgent Create(string name, string instructions, string? model) =>
        _client.Value
            .AsAIAgent(model: string.IsNullOrWhiteSpace(model) ? _model : model, instructions: instructions, name: name)
            .AsBuilder()
            .UseOpenTelemetry(TelemetrySource)
            .Build();
}
