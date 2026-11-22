namespace Mcm.Scrum.Application.Tools;

/// <summary>The facilitator's tools (SPEC 7.2). Each tool sends one command or query and holds no logic of its own.</summary>
public interface IScrumTools
{
    /// <summary>Gets the tools to offer the model.</summary>
    IReadOnlyList<ToolDefinition> Definitions { get; }

    /// <summary>
    /// Gets the read-only subset offered to the facilitator model. The write tools stay dispatchable but are not offered:
    /// the gateway already records events, parks topics and ends the standup from floor control, and a model that could
    /// also do so duplicated events and ended the standup before its recap.
    /// </summary>
    IReadOnlyList<ToolDefinition> FacilitatorDefinitions { get; }

    /// <summary>Runs a tool.</summary>
    /// <param name="sessionId">The standup session.</param>
    /// <param name="name">Tool name.</param>
    /// <param name="argumentsJson">Arguments as JSON.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result as JSON, or <c>{"error": "..."}</c> for an expected failure.</returns>
    Task<string> InvokeAsync(Guid sessionId, string name, string argumentsJson, CancellationToken ct);
}
