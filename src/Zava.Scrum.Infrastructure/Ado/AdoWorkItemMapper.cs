using System.Text.Json;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Infrastructure.Ado;

/// <summary>Maps Azure DevOps work item JSON to <see cref="WorkItem"/>. Pure, so it is unit tested without HTTP.</summary>
internal static class AdoWorkItemMapper
{
    private const string OwnerTagPrefix = "demo-owner-";
    private const string AgeTagPrefix = "demo-age-";
    private const string QuietTagPrefix = "demo-quiet-";

    /// <summary>Maps one item from the <c>workitemsbatch</c> response.</summary>
    /// <param name="item">An element of <c>value</c> with <c>id</c> and <c>fields</c>.</param>
    /// <param name="options">Demo shortcuts.</param>
    /// <returns>The work item.</returns>
    public static WorkItem Map(JsonElement item, DemoOptions options)
    {
        var fields = item.GetProperty("fields");
        var tags = ReadTags(fields);
        var (assignedTo, ownerKey) = (ReadAssignee(fields), options.HonorOwnerTags ? TagValue(tags, OwnerTagPrefix) : null);

        // DEMO: Azure DevOps cannot backdate StateChangeDate, so demo-age-N shifts it back N days; PROD: no tag shifts.
        var stateChanged = ReadDate(fields, AdoFields.StateChangeDate) ?? ReadDate(fields, AdoFields.ChangedDate) ?? DateTimeOffset.UnixEpoch;
        var changed = ReadDate(fields, AdoFields.ChangedDate) ?? stateChanged;
        if (options.HonorAgeTags)
        {
            stateChanged = stateChanged.AddDays(-TagNumber(tags, AgeTagPrefix));
            changed = changed.AddDays(-TagNumber(tags, QuietTagPrefix));
        }

        return new WorkItem(
            new WorkItemId(item.GetProperty("id").GetInt32()),
            ReadString(fields, AdoFields.Title) ?? string.Empty,
            ReadString(fields, AdoFields.WorkItemType) ?? string.Empty,
            ReadString(fields, AdoFields.State) ?? string.Empty,
            assignedTo,
            ownerKey,
            stateChanged,
            changed,
            ReadNumber(fields, AdoFields.StoryPoints),
            ReadNumber(fields, AdoFields.RemainingWork),
            ReadDate(fields, AdoFields.FinishDate) ?? ReadDate(fields, AdoFields.TargetDate),
            tags,
            ReadNumber(fields, AdoFields.OriginalEstimate),
            ReadString(fields, AdoFields.AcceptanceCriteria));
    }

    private static List<string> ReadTags(JsonElement fields) =>
        (ReadString(fields, AdoFields.Tags) ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static string? TagValue(List<string> tags, string prefix) =>
        tags.FirstOrDefault(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..];

    private static int TagNumber(List<string> tags, string prefix) =>
        int.TryParse(TagValue(tags, prefix), System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : 0;

    private static string? ReadAssignee(JsonElement fields)
    {
        if (!fields.TryGetProperty(AdoFields.AssignedTo, out var who))
        {
            return null;
        }

        return who.ValueKind == JsonValueKind.Object
            ? (who.TryGetProperty("uniqueName", out var u) ? u.GetString() : null) ?? (who.TryGetProperty("displayName", out var d) ? d.GetString() : null)
            : who.GetString();
    }

    private static string? ReadString(JsonElement fields, string name) =>
        fields.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static double? ReadNumber(JsonElement fields, string name) =>
        fields.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

    private static DateTimeOffset? ReadDate(JsonElement fields, string name) =>
        ReadString(fields, name) is { } s && DateTimeOffset.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var d)
            ? d
            : null;
}
