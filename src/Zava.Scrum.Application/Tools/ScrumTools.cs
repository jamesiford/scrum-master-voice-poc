using System.Text.Json;
using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Features.Standups.EndStandup;
using Mcm.Scrum.Application.Features.Standups.GetMemberBrief;
using Mcm.Scrum.Application.Features.Standups.GetNextSpeaker;
using Mcm.Scrum.Application.Features.Standups.GetTimeRemaining;
using Mcm.Scrum.Application.Features.Standups.GetWorkItemDetails;
using Mcm.Scrum.Application.Features.Standups.MarkAbsent;
using Mcm.Scrum.Application.Features.Standups.ParkTopic;
using Mcm.Scrum.Application.Features.Standups.ReconcileUpdate;
using Mcm.Scrum.Application.Features.Standups.RecordStandupEvent;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Tools;

/// <summary>Maps tool names one-to-one onto commands and queries (SPEC 6.1.4).</summary>
internal sealed class ScrumTools(ISender sender) : IScrumTools
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private static readonly ToolDefinition[] Tools =
    [
        new("get_next_speaker", "Gets who speaks next and whether they are absent.", """{"type":"object","properties":{}}"""),
        new("get_member_brief", "Gets a member's open work items and flags.", """{"type":"object","properties":{"memberId":{"type":"string"}},"required":["memberId"]}"""),
        new("reconcile_update", "Checks what a member said against their brief; returns a grounded suggestedQuestion and events.", """{"type":"object","properties":{"memberId":{"type":"string"},"utterance":{"type":"string"}},"required":["memberId","utterance"]}"""),
        new("record_event", "Records a commitment, blocker or decision.", """{"type":"object","properties":{"type":{"type":"string","enum":["Commitment","Blocker","Decision"]},"memberId":{"type":"string"},"text":{"type":"string"},"workItemIds":{"type":"array","items":{"type":"integer"}}},"required":["type","text"]}"""),
        new("park_topic", "Parks a topic for offline discussion.", """{"type":"object","properties":{"topic":{"type":"string"}},"required":["topic"]}"""),
        new("mark_absent", "Marks a member absent today.", """{"type":"object","properties":{"memberId":{"type":"string"}},"required":["memberId"]}"""),
        new("get_time_remaining", "Gets seconds left in the standup.", """{"type":"object","properties":{}}"""),
        new("get_work_item_details", "Gets current facts about one sprint work item (state, owner, due date, whether overdue, points, tags). Use it to answer a question about an item.", """{"type":"object","properties":{"workItemId":{"type":"integer"}},"required":["workItemId"]}"""),
        new("end_standup", "Ends the standup after the recap.", """{"type":"object","properties":{}}"""),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ToolDefinition> Definitions => Tools;

    /// <inheritdoc />
    public IReadOnlyList<ToolDefinition> FacilitatorDefinitions { get; } =
        [.. Tools.Where(t => t.Name is "get_member_brief" or "get_time_remaining" or "get_work_item_details")];

    /// <inheritdoc />
    public async Task<string> InvokeAsync(Guid sessionId, string name, string argumentsJson, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(name);
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
        var args = doc.RootElement;
        return name switch
        {
            "get_next_speaker" => Out(await sender.Send(new GetNextSpeakerQuery(sessionId), ct)),
            "get_member_brief" => Out(await sender.Send(new GetMemberBriefQuery(sessionId, Str(args, "memberId")), ct)),
            "reconcile_update" => Out(await sender.Send(new ReconcileUpdateQuery(sessionId, Str(args, "memberId"), Str(args, "utterance")), ct)),
            "record_event" => Out(await sender.Send(RecordEvent(sessionId, args), ct)),
            "park_topic" => Out(await sender.Send(new ParkTopicCommand(sessionId, Str(args, "topic")), ct)),
            "mark_absent" => Out(await sender.Send(new MarkAbsentCommand(sessionId, Str(args, "memberId")), ct)),
            "get_time_remaining" => Out(await sender.Send(new GetTimeRemainingQuery(sessionId), ct)),
            "get_work_item_details" => Out(await sender.Send(new GetWorkItemDetailsQuery(sessionId, args.TryGetProperty("workItemId", out var wid) && wid.ValueKind == JsonValueKind.Number ? wid.GetInt32() : 0), ct)),
            "end_standup" => Out(await sender.Send(new EndStandupCommand(sessionId), ct)),
            _ => JsonSerializer.Serialize(new { error = $"Unknown tool '{name}'." }, Json),
        };
    }

    private static RecordStandupEventCommand RecordEvent(Guid sessionId, JsonElement args)
    {
        var type = Enum.TryParse<StandupEventType>(Str(args, "type"), ignoreCase: true, out var t) ? t : StandupEventType.Decision;
        var ids = args.TryGetProperty("workItemIds", out var arr) && arr.ValueKind == JsonValueKind.Array
            ? arr.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Number).Select(e => e.GetInt32()).ToList()
            : [];
        var member = Str(args, "memberId");
        return new RecordStandupEventCommand(sessionId, type, member.Length == 0 ? null : member, Str(args, "text"), ids);
    }

    private static string Str(JsonElement args, string name) =>
        args.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : string.Empty;

    private static string Out(Result result) =>
        result.Error is { } e
            ? JsonSerializer.Serialize(new { error = e.Message }, Json)
            : result.GetType().IsGenericType
                ? JsonSerializer.Serialize(result.GetType().GetProperty("Value")!.GetValue(result), Json)
                : """{"ok":true}""";
}
