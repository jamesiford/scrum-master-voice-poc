using System.Globalization;
using System.Text;
using Mcm.Scrum.Domain.Kpis;

namespace Mcm.Scrum.Application.Features.Reports;

/// <summary>Builds report text. Numbers are formatted here from <see cref="KpiSummary"/>; the model only adds the narrative.</summary>
public static class ReportComposer
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary>Writes the narrative used when the model is unavailable.</summary>
    /// <param name="kpi">The KPIs.</param>
    /// <param name="latest">The most recent sprint, if any.</param>
    /// <returns>Two plain sentences.</returns>
    public static string FallbackNarrative(KpiSummary kpi, SprintHistory? latest)
    {
        ArgumentNullException.ThrowIfNull(kpi);
        if (kpi.Sprints == 0 || latest is null)
        {
            return "There is no sprint history to report on yet.";
        }

        return string.Create(Culture, $"Over the last {kpi.Sprints} sprints the team delivered {kpi.SayDoRatio:0%} of what it committed, at an average velocity of {kpi.Velocity:0.#} points. {latest.Name} delivered {KpiCalculator.SayDo(latest):0%} with {latest.CarryOverItems} item(s) carried over.");
    }

    /// <summary>Formats the KPIs as label and display-text pairs, so the model quotes figures exactly as shown.</summary>
    /// <param name="kpi">The KPIs.</param>
    /// <returns>Label to formatted value.</returns>
    public static IReadOnlyDictionary<string, string> KpiText(KpiSummary kpi)
    {
        ArgumentNullException.ThrowIfNull(kpi);
        return new Dictionary<string, string>
        {
            ["Say/do ratio"] = kpi.SayDoRatio.ToString("0%", Culture),
            ["Velocity (points per sprint)"] = kpi.Velocity.ToString("0.#", Culture),
            ["Carry-over items (total)"] = kpi.CarryOverItems.ToString(Culture),
            ["Average blocker age (days)"] = kpi.AvgBlockerAgeDays.ToString("0.0", Culture),
            ["Average story cycle time (days)"] = kpi.AvgCycleTimeDays.ToString("0.0", Culture),
            ["Standup attendance"] = (kpi.AvgAttendancePct / 100).ToString("0%", Culture),
            ["Follow-ups closed within 24 hours"] = (kpi.AvgFollowUpClosurePct / 100).ToString("0%", Culture),
            ["Agent-run standups (total)"] = kpi.AgentRunStandups.ToString(Culture),
            ["Average standup duration (min)"] = kpi.AvgStandupMinutes.ToString("0.#", Culture),
        };
    }

    /// <summary>Formats the KPI table.</summary>
    /// <param name="kpi">The KPIs.</param>
    /// <returns>A Markdown table.</returns>
    public static string KpiTable(KpiSummary kpi)
    {
        ArgumentNullException.ThrowIfNull(kpi);
        var sb = new StringBuilder();
        sb.AppendLine("| KPI | Value |").AppendLine("|---|---|");
        Row(sb, "Say/do ratio", kpi.SayDoRatio.ToString("0%", Culture));
        Row(sb, "Velocity (points per sprint)", kpi.Velocity.ToString("0.#", Culture));
        Row(sb, "Carry-over items", kpi.CarryOverItems.ToString(Culture));
        Row(sb, "Average blocker age (days)", kpi.AvgBlockerAgeDays.ToString("0.0", Culture));
        Row(sb, "Average story cycle time (days)", kpi.AvgCycleTimeDays.ToString("0.0", Culture));
        Row(sb, "Standup attendance", (kpi.AvgAttendancePct / 100).ToString("0%", Culture));
        Row(sb, "Follow-ups closed within 24 hours", (kpi.AvgFollowUpClosurePct / 100).ToString("0%", Culture));
        Row(sb, "Agent-run standups", kpi.AgentRunStandups.ToString(Culture));
        Row(sb, "Average standup duration (min)", kpi.AvgStandupMinutes.ToString("0.#", Culture));
        return sb.ToString();
    }

    /// <summary>Formats a per-sprint table.</summary>
    /// <param name="sprints">The sprints.</param>
    /// <returns>A Markdown table.</returns>
    public static string SprintTable(IReadOnlyList<SprintHistory> sprints)
    {
        ArgumentNullException.ThrowIfNull(sprints);
        var sb = new StringBuilder();
        sb.AppendLine("| Sprint | Committed | Completed | Say/do | Carry-over |").AppendLine("|---|---|---|---|---|");
        foreach (var s in sprints)
        {
            sb.AppendLine(string.Create(Culture, $"| {s.Name} | {s.CommittedPoints:0.#} | {s.CompletedPoints:0.#} | {KpiCalculator.SayDo(s):0%} | {s.CarryOverItems} |"));
        }

        return sb.ToString();
    }

    private static void Row(StringBuilder sb, string label, string value) => sb.Append("| ").Append(label).Append(" | ").Append(value).AppendLine(" |");
}