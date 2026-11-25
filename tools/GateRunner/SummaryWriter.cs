using System.Text;

namespace Zava.Scrum.Tools.GateRunner;

/// <summary>Turns saved run results into the Markdown summary pasted into the gate table.</summary>
internal static class SummaryWriter
{
    private static readonly (string Name, Func<TimingTurn, int> Pick)[] Hops =
    [
        ("Total (speech end to first audio)", t => t.TotalMs),
        ("Speech end to transcript", t => t.SpeechEndToTranscriptMs),
        ("Analyser (rules and grounding)", t => t.AnalyserMs),
        ("Decision model call", t => t.DecisionModelMs),
        ("Orchestration (floor, silence, reconcile)", t => t.OrchestrationMs),
        ("Voice Live first audio", t => t.VoiceFirstAudioMs),
    ];

    /// <summary>Builds the Markdown for a set of runs, one section per label.</summary>
    /// <param name="runs">All the runs.</param>
    /// <returns>Markdown text.</returns>
    public static string Write(IReadOnlyList<RunResult> runs)
    {
        var sb = new StringBuilder();
        sb.Append("# Gate runner summary").AppendLine().AppendLine();
        foreach (var group in runs.GroupBy(r => (r.Label, r.Build)).OrderBy(g => g.Key.Label))
        {
            var turns = group.SelectMany(r => r.Turns).ToList();
            sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"### {group.Key.Label} (build {group.Key.Build}, {group.Count()} runs)"));
            sb.AppendLine();
            sb.AppendLine("| Hop | p50 ms | p95 ms | n |");
            sb.AppendLine("| --- | ---: | ---: | ---: |");
            foreach (var (name, pick) in Hops)
            {
                var v = turns.Select(pick).ToList();
                sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"| {name} | {Stats.Percentile(v, 50)} | {Stats.Percentile(v, 95)} | {v.Count} |"));
            }

            var questions = turns.Where(t => t.Reason is "Clarify" or "Unblocker").Select(t => t.TotalMs).ToList();
            sb.AppendLine();
            sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Questions only (Clarify and Unblocker turns): p50 {Stats.Percentile(questions, 50)} ms, p95 {Stats.Percentile(questions, 95)} ms, n {questions.Count}."));
            var modelRead = turns.Count(t => t.Source == "Model");
            sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Read by the decision model: {modelRead} of {turns.Count} turns; the rest used the rules."));
            sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"API-reported session p50 per run: {string.Join(", ", group.OrderBy(r => r.StartedAt).Select(r => r.ReportedP50Ms))} ms."));
            sb.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Agent spoke over a person (talk-overs): {group.Sum(r => r.TalkOvers)} in {group.Count()} runs. People's transcript pieces: {string.Join(", ", group.OrderBy(r => r.StartedAt).Select(r => r.HumanSegments))} per run."));
            sb.AppendLine();
        }

        sb.AppendLine("Percentiles are nearest-rank over every timed turn in every run of the section; no values are interpolated.");
        return sb.ToString();
    }
}
