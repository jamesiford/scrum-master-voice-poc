using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Voice;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>
/// The decision model for live speech: one call reads an utterance in three steps (intent, work items, comment kind). It returns
/// <see langword="null"/> when the model is off, unconfigured, slower than the latency budget or unreadable, and the caller then uses the rules.
/// </summary>
public sealed class FoundryUtteranceAnalyser(FoundryChat chat, IPromptStore prompts, IOptions<UtteranceAnalysisOptions> options) : IUtteranceAnalyser
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<UtteranceReading?> ReadAsync(UtteranceContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        var o = options.Value;
        if (!o.UseModel || !chat.IsConfigured || string.IsNullOrWhiteSpace(context.Text))
        {
            return null;
        }

        var instructions = await prompts.GetAsync("utterance-analyst", ct);
        if (instructions is null)
        {
            return null;
        }

        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(o.TimeoutMs);
        try
        {
            var content = await chat.TryCompleteJsonAsync(instructions, Serialise(context), budget.Token, string.IsNullOrWhiteSpace(o.Deployment) ? null : o.Deployment);
            return content is null ? null : Parse(content);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Over budget: the rules decide, so a slow model never delays the agent's reply.
            return null;
        }
    }

    private static string Serialise(UtteranceContext c)
    {
        string NameOf(string? id) => c.Roster.FirstOrDefault(r => r.MemberId == id)?.DisplayName ?? string.Empty;
        return JsonSerializer.Serialize(
            new
            {
                utterance = c.Text,
                speaker = new { id = c.SpeakerId, name = NameOf(c.SpeakerId) },
                holder = c.HolderId is null ? null : new { id = c.HolderId, name = NameOf(c.HolderId) },
                roster = c.Roster.Select(r => new { id = r.MemberId, name = r.DisplayName }),
                items = c.Items.Select(i => new { id = i.Id.Value, type = i.Type, title = i.Title, state = i.State, flags = i.Flags.Select(f => f.ToString()) }),
                asked = c.Asked,
                focusItemId = c.FocusItemId,
            },
            Json);
    }

    private static UtteranceReading? Parse(string content)
    {
        try
        {
            var r = JsonSerializer.Deserialize<ModelReply>(content, Json);
            if (r is null)
            {
                return null;
            }

            var comments = (r.Comments ?? [])
                .Select(c => (Comment: c, Kind: KindOf(c.Kind)))
                .Where(x => x.Kind is not null && !string.IsNullOrWhiteSpace(x.Comment.Text))
                .Select(x => new ItemComment(x.Kind!.Value, x.Comment.WorkItemIds ?? [], x.Comment.Text!.Trim(), string.IsNullOrWhiteSpace(x.Comment.NewDate) ? null : x.Comment.NewDate.Trim()))
                .ToList();
            var intent = SnakeEnum.TryParse<UtteranceIntent>(r.Intent, out var i) ? i : (UtteranceIntent?)null;
            return new UtteranceReading(intent, Blank(r.TargetMemberId), Blank(r.ByMemberId), r.WorkItemIds ?? [], comments, Math.Clamp(r.Confidence ?? 0, 0, 1));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static CommentKind? KindOf(string? word) => SnakeEnum.TryParse<CommentKind>(word, out var kind) ? kind : null;

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private sealed record ModelReply(string? Intent, string? TargetMemberId, string? ByMemberId, double? Confidence, List<int>? WorkItemIds, List<ModelComment>? Comments);

    private sealed record ModelComment(string? Kind, string? Text, List<int>? WorkItemIds, string? NewDate);
}
