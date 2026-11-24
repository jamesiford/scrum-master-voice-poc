using System.Text.Json;
using System.Text.Json.Nodes;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Agents;

/// <summary>
/// The decision model for live speech, on a Microsoft-Decision-1.1 deployment (ADR 0014). One request carries typed questions about the
/// utterance: a <c>choice</c> for what the speaker is doing, a <c>choice</c> for the teammate named, a yes/no <c>noul</c> per open work item
/// and a yes/no per comment kind. The model returns probabilities, never prose, so the comment text and any date are taken from the
/// transcript in code. Returns <see langword="null"/> on any fault, slow answer or rate limit, and the caller then uses the rules.
/// </summary>
/// <remarks>
/// VERIFY: https://learn.microsoft.com/azure/foundry/foundry-models/how-to/use-foundry-models-microsoft-decision (route and answer shape).
/// </remarks>
public sealed class DecisionUtteranceAnalyser(
    HttpClient http,
    IOptions<FoundryOptions> foundry,
    IOptions<UtteranceAnalysisOptions> options,
    ILogger<DecisionUtteranceAnalyser> logger) : IUtteranceAnalyser
{
    private const string Route = "providers/microsoft/v1/systemone";
    private const string None = "none";
    private const int MaxItems = 16;
    private const int MaxTextLength = 240;

    private static readonly (string Key, string Description)[] Intents =
    [
        ("update", "The speaker reports on their own work or answers a question put to them"),
        ("ask_item_info", "The speaker asks a factual question about a work item, such as its state, owner or age"),
        ("ask_what_to_report", "The speaker asks what they are supposed to report on"),
        ("step_away", "Someone says they or a teammate stepped away or needs a moment"),
        ("proxy_update", "One person gives, or offers to give, another person's update"),
        ("correction", "The speaker takes back or fixes something they said earlier"),
        ("back_now", "A person who stepped away says they are back"),
        ("out_of_turn_update", "Someone other than the person whose turn it is reports on their own tasks: what they finished, will do or are blocked on (\"yesterday I...\", \"today I will...\", \"I'm blocked on...\")"),
        ("chatter", "Anything else, including a side discussion: advice, analysis, guesses or suggestions about a problem someone else raised or about someone else's work, greetings, jokes and acknowledgements"),
    ];

    private static readonly (CommentKind Kind, string Question)[] Kinds =
    [
        (CommentKind.Blocker, "Does the speaker say something is blocking or stopping progress (waiting on someone or something, unable to proceed)? Saying there are no blockers is false."),
        (CommentKind.DateChange, "Does the speaker say that a finish or target date moves, slips or is pulled in?"),
        (CommentKind.Commitment, "Does the speaker say what they will do next, or promise to do something?"),
        (CommentKind.Decision, "Does the speaker say something was decided or agreed?"),
        (CommentKind.Estimate, "Does the speaker say the remaining work or effort changed?"),
        (CommentKind.Done, "Does the speaker say a work item is finished?"),
    ];

    /// <inheritdoc />
    public async Task<UtteranceReading?> ReadAsync(UtteranceContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        var o = options.Value;
        if (!o.UseModel || !o.UseDecisionEngine || string.IsNullOrWhiteSpace(o.DecisionDeployment)
            || string.IsNullOrWhiteSpace(foundry.Value.ProjectEndpoint) || string.IsNullOrWhiteSpace(context.Text))
        {
            return null;
        }

        var items = context.Items.Take(MaxItems).ToList();
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(o.TimeoutMs);
        try
        {
            // A string body, not PostAsJsonAsync: the route rejects chunked requests and needs a Content-Length header.
            using var body = new StringContent(BuildRequest(o.DecisionDeployment, context, items).ToJsonString(), System.Text.Encoding.UTF8, "application/json");
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            using var response = await http.PostAsync(Route, body, budget.Token);
            logger.LogInformation("Decision model status={Status} httpMs={HttpMs} questions={Questions}", (int)response.StatusCode, (int)System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds, items.Count + Kinds.Length + 3);
            if (!response.IsSuccessStatusCode)
            {
                // 429 is the usual one: the deployment's rate limit. The rules carry the standup meanwhile.
                var detail = await response.Content.ReadAsStringAsync(budget.Token);
                logger.LogWarning("Decision model returned {Status}: {Detail}; using rules", (int)response.StatusCode, detail.Length > 300 ? detail[..300] : detail);
                return null;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(budget.Token), cancellationToken: budget.Token);
            return Read(doc.RootElement, context, items, o.Threshold);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Decision model call failed; using rules");
            return null;
        }
    }

    private static JsonObject BuildRequest(string deployment, UtteranceContext c, List<BriefItem> items)
    {
        string NameOf(string? id) => c.Roster.FirstOrDefault(r => r.MemberId == id)?.DisplayName ?? string.Empty;
        var named = new JsonObject { [None] = "Nobody else is named" };
        foreach (var r in c.Roster.Where(r => r.MemberId != c.SpeakerId))
        {
            named[r.MemberId] = r.DisplayName;
        }

        var intents = new JsonObject();
        foreach (var (key, description) in Intents)
        {
            intents[key] = description;
        }

        var questions = new JsonObject
        {
            ["intent"] = Choice("What is the speaker doing with this utterance? The person whose turn it is gives updates; anyone else is chiming in.", intents),
            ["named"] = Choice("Which teammate, other than the speaker, does the utterance name or refer to?", named),
            ["hands_over"] = Noul("Does the speaker hand their own update to the teammate they name (for example \"Maya will give my update\")?"),
        };
        foreach (var i in items)
        {
            questions[$"item_{i.Id.Value}"] = Noul($"Is the utterance about work item {i.Id.Value}, \"{i.Title}\" ({i.Type})?");
        }

        foreach (var (kind, question) in Kinds)
        {
            questions[$"kind_{kind}"] = Noul(question);
        }

        return new JsonObject
        {
            ["model"] = deployment,
            ["state"] = new JsonObject
            {
                ["utterance"] = c.Text,
                ["speaker"] = NameOf(c.SpeakerId),
                ["person_whose_turn_it_is"] = c.HolderId is null ? None : NameOf(c.HolderId),
                ["question_the_agent_just_asked"] = c.Asked,
                ["work_item_that_question_was_about"] = c.FocusItemId,
            },
            ["questions"] = questions,
        };
    }

    private static JsonObject Choice(string instructions, JsonObject criteria) =>
        new() { ["type"] = "choice", ["instructions"] = instructions, ["criteria"] = criteria };

    private static JsonObject Noul(string instructions) =>
        new() { ["type"] = "noul", ["instructions"] = instructions };

    private static UtteranceReading? Read(JsonElement root, UtteranceContext c, List<BriefItem> items, double threshold)
    {
        if (!root.TryGetProperty("answers", out var answers) || answers.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!TryChoice(answers, "intent", out var intentKey, out var confidence) || !SnakeEnum.TryParse<UtteranceIntent>(intentKey, out var intent))
        {
            return null;
        }

        var namedId = TryChoice(answers, "named", out var n, out _) && n != None ? n : null;
        var (target, by) = Members(intent, namedId, Yes(answers, "hands_over", threshold), c.SpeakerId);
        var ids = items.Where(i => Yes(answers, $"item_{i.Id.Value}", threshold)).Select(i => i.Id.Value).ToList();
        return new UtteranceReading(intent, target, by, ids, Comments(answers, intent, ids, c.Text, threshold), confidence);
    }

    // Who a step-away or proxy concerns follows from the named teammate and who is speaking; code decides, the model only answers questions.
    private static (string? Target, string? By) Members(UtteranceIntent intent, string? namedId, bool handsOver, string speakerId) => intent switch
    {
        UtteranceIntent.StepAway => (namedId ?? speakerId, null),
        UtteranceIntent.ProxyUpdate when handsOver => (speakerId, namedId),
        UtteranceIntent.ProxyUpdate => (namedId, speakerId),
        _ => (null, null),
    };

    private static List<ItemComment> Comments(JsonElement answers, UtteranceIntent intent, List<int> ids, string text, double threshold)
    {
        if (intent is not (UtteranceIntent.Update or UtteranceIntent.OutOfTurnUpdate or UtteranceIntent.Correction or UtteranceIntent.ProxyUpdate))
        {
            return [];
        }

        var said = text.Length > MaxTextLength ? text[..MaxTextLength].TrimEnd() : text.Trim();
        return Kinds
            .Where(k => Yes(answers, $"kind_{k.Kind}", threshold))
            .Select(k => new ItemComment(k.Kind, ids.Count == 1 ? [.. ids] : [], said, k.Kind == CommentKind.DateChange ? SpokenDates.Find(text) : null))
            .ToList();
    }

    private static bool TryChoice(JsonElement answers, string name, out string key, out double confidence)
    {
        key = string.Empty;
        confidence = 0;
        if (!answers.TryGetProperty(name, out var a) || !a.TryGetProperty("choice", out var c) || c.GetString() is not { Length: > 0 } picked)
        {
            return false;
        }

        key = picked;
        confidence = a.TryGetProperty("confidence", out var p) && p.TryGetDouble(out var d) ? d : 1;
        return true;
    }

    private static bool Yes(JsonElement answers, string name, double threshold) =>
        answers.TryGetProperty(name, out var a) && a.TryGetProperty("noul", out var p) && p.TryGetDouble(out var d) && d >= threshold;
}
