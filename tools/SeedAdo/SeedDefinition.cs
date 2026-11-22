using System.Text.Json.Serialization;

namespace Mcm.Scrum.Tools.SeedAdo;

/// <summary>The demo backlog definition in <c>demo/seed-ado.json</c>.</summary>
internal sealed record SeedDefinition(
    string Project,
    string Team,
    string AreaPath,
    string Iteration,
    int SprintLengthDays,
    int DaysElapsed,
    string SprintGoal,
    string? NextIteration,
    List<SeedMember> Members,
    List<SeedWorkItem> WorkItems);

internal sealed record SeedMember(string Key, string DisplayName);

internal sealed record SeedWorkItem(
    string Key,
    string Type,
    string Title,
    string State,
    double? Points,
    double? Remaining,
    string? Owner,
    int? FinishInDays,
    List<string> Tags,
    double? OriginalEstimate = null,
    bool Backlog = false,
    string? AcceptanceCriteria = null)
{
    [JsonIgnore]
    public string KeyTag => "demo-key-" + Key;
}
