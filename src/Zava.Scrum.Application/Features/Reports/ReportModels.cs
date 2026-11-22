using Zava.Scrum.Domain.Kpis;

namespace Zava.Scrum.Application.Features.Reports;

/// <summary>KPIs over a window, with the sprints behind them.</summary>
/// <param name="Summary">The KPIs.</param>
/// <param name="Sprints">The sprints in the window, oldest first.</param>
/// <param name="Illustrative">True when fixture-backed.</param>
public sealed record KpiReport(KpiSummary Summary, IReadOnlyList<SprintHistory> Sprints, bool Illustrative);

/// <summary>A written report.</summary>
/// <param name="Title">Report title.</param>
/// <param name="Markdown">Markdown with numbers from code and a narrative that the model phrased.</param>
/// <param name="Narrative">The narrative alone.</param>
/// <param name="NarrativeFromModel">False when the model was unavailable and the deterministic text was used.</param>
/// <param name="Illustrative">True when fixture-backed.</param>
public sealed record ReportDocument(string Title, string Markdown, string Narrative, bool NarrativeFromModel, bool Illustrative);
