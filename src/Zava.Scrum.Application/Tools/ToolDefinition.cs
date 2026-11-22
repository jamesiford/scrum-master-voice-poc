namespace Zava.Scrum.Application.Tools;

/// <summary>A tool the facilitator model may call (SPEC 7.2).</summary>
/// <param name="Name">Tool name, for example <c>reconcile_update</c>.</param>
/// <param name="Description">What it does, shown to the model.</param>
/// <param name="ParametersJsonSchema">JSON schema of the arguments.</param>
public sealed record ToolDefinition(string Name, string Description, string ParametersJsonSchema);
