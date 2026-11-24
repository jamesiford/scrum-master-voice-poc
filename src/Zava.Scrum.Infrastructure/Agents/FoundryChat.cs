using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>
/// Runs one prompt through a Microsoft Agent Framework chat-client agent on the Foundry project and returns the model's JSON text.
/// Any failure (no model configured, timeout, service error, no credential) yields <see langword="null"/> so callers fall back to
/// deterministic text; a model outage never fails a use case.
/// </summary>
/// <remarks>
/// ADR 0009b replaced the earlier hand-written <c>HttpClient</c> call (ADR 0002) with this; the ports and every caller are unchanged.
/// </remarks>
public sealed class FoundryChat(IFoundryAgentFactory agents, IOptions<FoundryOptions> options, ILogger<FoundryChat> logger)
{
    private const int MaxOutputTokens = 4000;
    private const string JsonReminder = "\n\nReply with a single JSON object.";

    /// <summary>Gets a value indicating whether a model deployment is configured.</summary>
    public bool IsConfigured => options.Value.IsConfigured;

    /// <summary>Asks the reasoning model for a JSON object.</summary>
    /// <param name="system">System instructions.</param>
    /// <param name="user">User content, usually JSON facts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="deployment">A model deployment to use instead of the default reasoning deployment, or <see langword="null"/>.</param>
    /// <returns>The JSON text, or <see langword="null"/> when unconfigured or the call failed.</returns>
    public async Task<string?> TryCompleteJsonAsync(string system, string user, CancellationToken ct, string? deployment = null)
    {
        if (!IsConfigured)
        {
            return null;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        try
        {
            var agent = agents.Create("zava-scrum-agent", system, deployment);
            var run = new ChatClientAgentRunOptions(new ChatOptions { ResponseFormat = ChatResponseFormat.Json, MaxOutputTokens = MaxOutputTokens });
            // The Responses API rejects JSON mode unless the input (not only the instructions) mentions JSON.
            var response = await agent.RunAsync(user + JsonReminder, session: null, run, timeout.Token);
            return string.IsNullOrWhiteSpace(response.Text) ? null : response.Text;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Model faults are expected (timeout, 429, no credential); the caller has a code fallback, so log and carry on.
            logger.LogWarning(ex, "Foundry call failed; using deterministic text");
            return null;
        }
    }
}
