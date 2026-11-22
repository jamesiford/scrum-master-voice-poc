using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>Reconcile agent: compares a spoken update with a member's brief and returns structured output (SPEC 10.2).</summary>
public sealed class FoundryReconcileAgent(FoundryChat chat, IPromptStore prompts) : IReconcileAgent
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<ReconcileSuggestion?> ReconcileAsync(MemberBrief brief, string utterance, CancellationToken ct, IReadOnlyList<RecordedStatement>? recorded = null)
    {
        ArgumentNullException.ThrowIfNull(brief);
        if (!chat.IsConfigured || string.IsNullOrWhiteSpace(utterance))
        {
            return null;
        }

        var instructions = await prompts.GetAsync("reconcile", ct);
        if (instructions is null)
        {
            return null;
        }

        var user = JsonSerializer.Serialize(new { brief = new { brief.DisplayName, brief.Items }, utterance, recorded }, Json);
        var content = await chat.TryCompleteJsonAsync(instructions, user, ct);
        return content is null ? null : Parse(content);
    }

    private static ReconcileSuggestion? Parse(string content)
    {
        try
        {
            var r = JsonSerializer.Deserialize<ModelReply>(content, Json);
            if (r is null)
            {
                return null;
            }

            var events = (r.Events ?? [])
                .Where(e => !string.IsNullOrWhiteSpace(e.Text) && Enum.TryParse<StandupEventType>(e.Type, ignoreCase: true, out var t)
                    && t is StandupEventType.Commitment or StandupEventType.Blocker or StandupEventType.Decision)
                .Select(e => new SuggestedEvent(Enum.Parse<StandupEventType>(e.Type!, ignoreCase: true), e.Text!, e.WorkItemIds ?? []))
                .ToList();
            return new ReconcileSuggestion(r.Consistent ?? true, r.MentionedWorkItemIds ?? [], r.Risks ?? [], r.SuggestedQuestion, events, r.Retracts ?? []);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record ModelReply(bool? Consistent, List<int>? MentionedWorkItemIds, List<string>? Risks, string? SuggestedQuestion, List<ModelEvent>? Events, List<int>? Retracts);

    private sealed record ModelEvent(string? Type, string? Text, List<int>? WorkItemIds);
}
