using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;
using Zava.Scrum.Domain.Work;
using Zava.Scrum.Infrastructure.Agents;
using Zava.Scrum.Infrastructure.Http;

namespace Zava.Scrum.Tools.GateRunner;

/// <summary>Repeatable, content-free reports of authored synthetic decisions through the real adapter and the same grounding/rules.</summary>
internal static class DecisionEvaluation
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Evaluates rules and, explicitly, the real Microsoft-Decision deployment. Never writes to Azure DevOps.</summary>
    public static async Task<int> RunAsync(string goldenPath, string output, bool useModel, bool releaseCheck, CancellationToken ct)
    {
        var bytes = await File.ReadAllBytesAsync(goldenPath, ct);
        var set = JsonSerializer.Deserialize<GoldenSet>(bytes, Json) ?? throw new InvalidDataException("The golden set is empty.");
        Validate(set);
        var endpoint = Environment.GetEnvironmentVariable("EVALUATION_PROJECT_ENDPOINT");
        var deployment = Environment.GetEnvironmentVariable("EVALUATION_DECISION_DEPLOYMENT");
        if (string.IsNullOrWhiteSpace(deployment)) { deployment = "zava-decision"; }
        if (useModel && !Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("--model requires EVALUATION_PROJECT_ENDPOINT. No synthetic model result is substituted.");
        }

        var items = set.Items.Select(item => new BriefItem(new WorkItemId(item.Id), item.Title, "Task", "Active", 1, [])).ToArray();
        var known = items.Select(item => item.Id.Value).ToHashSet();
        using var http = new HttpClient(new BearerTokenHandler(new DefaultAzureCredential(), "https://cognitiveservices.azure.com/.default")
        {
            InnerHandler = new HttpClientHandler(),
        });
        if (useModel) { http.BaseAddress = new Uri(new Uri(endpoint!).GetLeftPart(UriPartial.Authority) + "/"); }
        var model = new DecisionUtteranceAnalyser(http, Options.Create(new FoundryOptions { ProjectEndpoint = endpoint ?? string.Empty }),
            Options.Create(new UtteranceAnalysisOptions { DecisionDeployment = deployment }),
            NullLogger<DecisionUtteranceAnalyser>.Instance);
        var rulesRows = new List<CaseResult>();
        var modelRows = new List<CaseResult>();
        var probabilities = new List<ProbabilityResult>();
        string? stopReason = null;
        foreach (var example in set.Cases)
        {
            var own = items.Where(item => set.Items.Single(i => i.Id == item.Id.Value).OwnerId == example.HolderId).ToArray();
            var rules = UtteranceAnalyser.Analyse(example.Text, example.SpeakerId, example.HolderId, set.Roster, own, known, null);
            rulesRows.Add(Score(example, rules, null, 0, known));
            if (!useModel || stopReason is not null) { continue; }
            var context = new Zava.Scrum.Application.Abstractions.Ports.UtteranceContext(
                example.Text, example.SpeakerId, example.HolderId, set.Roster, items, example.Asked, example.FocusItemId);
            var started = Stopwatch.GetTimestamp();
            var evidence = await model.ReadWithEvidenceAsync(context, ct);
            var analysis = UtteranceAnalyser.Analyse(example.Text, example.SpeakerId, example.HolderId, set.Roster, own, known, evidence.Reading);
            modelRows.Add(Score(example, analysis, evidence, (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds, known));
            AddProbabilities(example, evidence, items, probabilities);
            Console.WriteLine($"{example.Id}: source={analysis.Source}, intent={analysis.Interpretation.Intent}, failure={evidence.Failure ?? "none"}");
            // An authentication/rate-limit failure is reported once, not hammered for all 72 cases.
            if (evidence.Failure is "http_401" or "http_403" or "http_429") { stopReason = evidence.Failure; }
            await Task.Delay(250, ct);
        }

        var rulesReport = Summarise(rulesRows);
        var modelReport = Summarise(modelRows);
        var binaryCalibration = Calibrate(probabilities.Where(p => p.Question != "intent").ToList(), 0.7);
        var intentCalibration = Calibrate(probabilities.Where(p => p.Question == "intent").ToList(), 0.6);
        var releasePassed = modelRows.Count == set.Cases.Count && modelRows.All(row => row.Failure is null)
            && modelReport.IntentAccuracy >= 0.9 && modelReport.ItemAccuracy >= 0.95 && modelReport.CommentAccuracy >= 0.85
            && modelReport.DateAccuracy >= 0.95 && modelReport.EscapedIds == 0
            && binaryCalibration.AcceptedPrecision >= 0.9 && intentCalibration.AcceptedPrecision >= 0.9
            && binaryCalibration.Missing == 0 && intentCalibration.Missing == 0;
        var report = new
        {
            schemaVersion = 1,
            generatedAt = DateTimeOffset.UtcNow,
            teamId = set.TeamId,
            set.Provenance,
            goldenSha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            goldenCount = set.Cases.Count,
            model = "Microsoft-Decision-1.1",
            deployment,
            thresholds = new { binary = 0.7, intent = 0.6 },
            releasePassed,
            rules = new { status = "measured", summary = rulesReport, categories = rulesRows.GroupBy(row => row.Category).Select(group => new { category = group.Key, summary = Summarise(group.ToList()) }), cases = rulesRows, calibration = "not_applicable: rules emit no probabilities or comment kinds" },
            decision = new { status = useModel ? "measured" : "not_run", stopReason, expectedSamples = set.Cases.Count, unrunSamples = set.Cases.Count - modelRows.Count, summary = modelReport, categories = modelRows.GroupBy(row => row.Category).Select(group => new { category = group.Key, summary = Summarise(group.ToList()) }), cases = modelRows },
            calibration = new { binary = binaryCalibration, intent = intentCalibration, observations = probabilities },
            knowledge = new { status = "not_run", samples = 0 },
            workIq = new { status = "not_run", samples = 0 },
            liveFoundryQuality = new { status = "not_run", samples = 0, reason = "Content capture is prohibited; metadata cannot supply quality/safety/groundedness inputs." },
        };
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "decision-evaluation.json"), JsonSerializer.Serialize(report, Json), ct);
        var markdown = $"""
            # Decision evaluation

            Authored synthetic cases: {set.Cases.Count}; team `{set.TeamId}`; SHA256 `{report.goldenSha256}`.
            Model: Microsoft-Decision-1.1 / `{deployment}`. Binary threshold 0.7; intent threshold 0.6.
            Accuracy is exact per utterance, not inflated by the many negative binary labels.

            | Path | n | Intent | Items | Comment kinds | Dates | Escaped IDs |
            | --- | ---: | ---: | ---: | ---: | ---: | ---: |
            | Rules | {rulesReport.Samples} | {rulesReport.IntentAccuracy:P1} | {rulesReport.ItemAccuracy:P1} | {rulesReport.CommentAccuracy:P1} | {rulesReport.DateAccuracy:P1} (n{rulesReport.DateSamples}) | {rulesReport.EscapedIds} |
            | Decision + grounding | {modelReport.Samples} | {modelReport.IntentAccuracy:P1} | {modelReport.ItemAccuracy:P1} | {modelReport.CommentAccuracy:P1} | {modelReport.DateAccuracy:P1} (n{modelReport.DateSamples}) | {modelReport.EscapedIds} |

            Model failures: {modelReport.Failures}; rules fallbacks: {modelReport.RuleFallbacks}; stop reason: {stopReason ?? "none"}.
            0.7 accepted precision: {binaryCalibration.AcceptedPrecision:P1}, n={binaryCalibration.Accepted};
            Brier {binaryCalibration.Brier:F4}, ECE {binaryCalibration.Ece:F4}, missing={binaryCalibration.Missing}.
            0.6 accepted intent precision: {intentCalibration.AcceptedPrecision:P1}, n={intentCalibration.Accepted};
            Brier {intentCalibration.Brier:F4}, ECE {intentCalibration.Ece:F4}, missing={intentCalibration.Missing}.
            Model candidate ids rejected: {modelReport.RejectedIds}/{modelReport.CandidateIds}.
            Explicit outside-sprint challenge mentions: {modelReport.OutsideIdChallenges}; escaped ids: {modelReport.EscapedIds}.

            Release check: {(releasePassed ? "PASS" : "FAIL")}. CI reporting is non-blocking; `--release-check` exits non-zero on failure or absent model evidence.
            Rules intentionally emit no comment kinds or probabilities. Their absent capability is scored, not filled in with a stand-in.
            Knowledge, Work IQ and live Foundry content evaluations: not_run, n=0.
            """;
        await File.WriteAllTextAsync(Path.Combine(output, "decision-evaluation.md"), markdown, ct);
        Console.WriteLine(markdown);
        return releaseCheck && !releasePassed ? 1 : 0;
    }

    private static void Validate(GoldenSet set)
    {
        if (set.Version != 1 || set.Cases.Count < 60 || set.Cases.Select(c => c.Id).Distinct().Count() != set.Cases.Count
            || string.IsNullOrWhiteSpace(set.TeamId) || !set.Provenance.StartsWith("Authored synthetic", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Expected at least 60 distinct, team-scoped authored synthetic goldens.");
        }

        var roster = set.Roster.Select(member => member.MemberId).ToHashSet();
        var ids = set.Items.Select(item => item.Id).ToHashSet();
        if (set.Cases.Any(c => !roster.Contains(c.SpeakerId) || !roster.Contains(c.HolderId)
            || c.ItemIds.Any(id => !ids.Contains(id)) || c.RejectedIds.Any(ids.Contains)))
        {
            throw new InvalidDataException("A golden references an unknown roster member or invalid expected item.");
        }
    }

    private static CaseResult Score(GoldenCase expected, UtteranceAnalysis actual, DecisionReadingEvidence? evidence, int latencyMs, HashSet<int> known)
    {
        var candidateIds = evidence?.Reading?.WorkItemIds ?? [];
        var escaped = actual.WorkItemIds.Concat(actual.Comments.SelectMany(c => c.WorkItemIds)).Count(id => !known.Contains(id));
        return new(expected.Id, expected.Category,
            actual.Interpretation.Intent == expected.Intent && actual.Interpretation.TargetMemberId == (expected.TargetMemberId ?? expected.SpeakerId)
                && actual.Interpretation.ByMemberId == expected.ByMemberId,
            actual.WorkItemIds.ToHashSet().SetEquals(expected.ItemIds),
            actual.Comments.Select(c => c.Kind).ToHashSet().SetEquals(expected.CommentKinds),
            expected.NewDate is null ? null : actual.Comments.Any(c => c.Kind == CommentKind.DateChange && c.NewDate == expected.NewDate),
            actual.Interpretation, actual.WorkItemIds, actual.Comments.Select(c => c.Kind).ToArray(), actual.Source,
            latencyMs, evidence?.Failure, candidateIds.Count, candidateIds.Count(id => !known.Contains(id)), expected.RejectedIds.Count, escaped);
    }

    private static void AddProbabilities(GoldenCase expected, DecisionReadingEvidence evidence, IReadOnlyList<BriefItem> items, List<ProbabilityResult> output)
    {
        Add("intent", evidence.Reading?.Intent == expected.Intent);
        foreach (var item in items) { Add($"item_{item.Id.Value}", expected.ItemIds.Contains(item.Id.Value)); }
        // Progress has no binary question in the real adapter; it is not a missing model answer.
        foreach (var kind in Enum.GetValues<CommentKind>().Where(kind => kind != CommentKind.Progress))
        {
            Add($"kind_{kind}", expected.CommentKinds.Contains(kind));
        }
        void Add(string question, bool correct)
        {
            output.Add(new(expected.Id, question, evidence.Probabilities.TryGetValue(question, out var probability) ? probability : null, correct));
        }
    }

    private static EvaluationSummary Summarise(IReadOnlyList<CaseResult> rows) => new(
        rows.Count, Rate(rows.Count(row => row.IntentCorrect), rows.Count), Rate(rows.Count(row => row.ItemsCorrect), rows.Count),
        Rate(rows.Count(row => row.CommentsCorrect), rows.Count), Rate(rows.Count(row => row.DateCorrect == true), rows.Count(row => row.DateCorrect is not null)),
        rows.Count(row => row.DateCorrect is not null),
        rows.Count(row => row.Failure is not null), rows.Count(row => row.Source == AnalysisSource.Rules), rows.Sum(row => row.CandidateIds), rows.Sum(row => row.RejectedIds),
        rows.Sum(row => row.OutsideIdChallenges), rows.Sum(row => row.EscapedIds));

    private static Calibration Calibrate(IReadOnlyList<ProbabilityResult> observations, double threshold)
    {
        var valid = observations.Where(p => p.Probability is not null).ToArray();
        var accepted = valid.Where(p => p.Probability >= threshold).ToArray();
        var bins = Enumerable.Range(0, 10).Select(index =>
        {
            var values = valid.Where(p => Math.Min(9, (int)(p.Probability!.Value * 10)) == index).ToArray();
            return new { lower = index / 10.0, samples = values.Length, probability = values.Length == 0 ? 0 : values.Average(p => p.Probability!.Value), accuracy = Rate(values.Count(p => p.Truth), values.Length) };
        }).ToArray();
        return new(threshold, valid.Length, observations.Count - valid.Length, accepted.Length, Rate(accepted.Count(p => p.Truth), accepted.Length),
            valid.Length == 0 ? 0 : valid.Average(p => Math.Pow(p.Probability!.Value - (p.Truth ? 1 : 0), 2)),
            bins.Sum(bin => Math.Abs(bin.probability - bin.accuracy) * Rate(bin.samples, valid.Length)));
    }

    private static double Rate(int numerator, int denominator) => denominator == 0 ? 0 : (double)numerator / denominator;

    private sealed record GoldenSet(int Version, string Provenance, string TeamId, IReadOnlyList<NameRef> Roster, IReadOnlyList<GoldenItem> Items, IReadOnlyList<GoldenCase> Cases);
    private sealed record GoldenItem(int Id, string Title, string OwnerId);
    private sealed record GoldenCase(string Id, string Category, string Text, UtteranceIntent Intent, IReadOnlyList<int> ItemIds, IReadOnlyList<CommentKind> CommentKinds,
        string SpeakerId = "daniel", string HolderId = "daniel", string? TargetMemberId = null, string? ByMemberId = null, string? NewDate = null,
        string? Asked = null, int? FocusItemId = null)
    {
        public IReadOnlyList<int> RejectedIds { get; init; } = [];
    }
    private sealed record CaseResult(string Id, string Category, bool IntentCorrect, bool ItemsCorrect, bool CommentsCorrect, bool? DateCorrect,
        Interpretation ActualIntent, IReadOnlyList<int> ActualIds, IReadOnlyList<CommentKind> ActualKinds, AnalysisSource Source,
        int LatencyMs, string? Failure, int CandidateIds, int RejectedIds, int OutsideIdChallenges, int EscapedIds);
    private sealed record ProbabilityResult(string Id, string Question, double? Probability, bool Truth);
    private sealed record EvaluationSummary(int Samples, double IntentAccuracy, double ItemAccuracy, double CommentAccuracy, double DateAccuracy, int DateSamples, int Failures, int RuleFallbacks,
        int CandidateIds, int RejectedIds, int OutsideIdChallenges, int EscapedIds);
    private sealed record Calibration(double Threshold, int Samples, int Missing, int Accepted, double AcceptedPrecision, double Brier, double Ece);
}
