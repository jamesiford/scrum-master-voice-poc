using System.Text.Json;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Features.Reports;

/// <summary>Builds the facts JSON given to the Reporting agent. Everything in it was computed in code (rule 3).</summary>
public static class ReportFacts
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Serialises KPIs, sprints and the current pre-brief.</summary>
    /// <param name="kpi">The KPIs.</param>
    /// <param name="sprints">The sprints behind them.</param>
    /// <param name="current">The most recent standup session, if any.</param>
    /// <param name="followUps">Follow-ups of that standup, as short lines.</param>
    /// <returns>JSON.</returns>
    public static string Build(KpiSummary kpi, IReadOnlyList<SprintHistory> sprints, StandupSession? current, IReadOnlyList<string> followUps)
    {
        ArgumentNullException.ThrowIfNull(kpi);
        var brief = current?.PreBrief;
        return JsonSerializer.Serialize(
            new
            {
                window = $"the last {kpi.Sprints} sprints",
                kpi = ReportComposer.KpiText(kpi),
                sprints = sprints.Select(s => new { s.Name, committedPoints = s.CommittedPoints, completedPoints = s.CompletedPoints, sayDo = KpiCalculator.SayDo(s).ToString("0%", System.Globalization.CultureInfo.InvariantCulture), carryOverItems = s.CarryOverItems }),
                currentSprint = brief is null
                    ? null
                    : new
                    {
                        brief.IterationPath,
                        brief.SprintGoal,
                        brief.DaysLeft,
                        brief.Burndown,
                        brief.TeamRisks,
                        members = brief.Members.Select(m => new
                        {
                            m.DisplayName,
                            items = m.Items.Select(i => new { id = i.Id.Value, i.Title, i.Type, i.State, flags = i.Flags.Select(f => f.ToString()) }),
                        }),
                    },
                lastStandup = current is null
                    ? null
                    : new
                    {
                        status = current.Status.ToString(),
                        events = current.ActiveEvents.Where(e => e.Type is StandupEventType.Commitment or StandupEventType.Blocker or StandupEventType.Decision or StandupEventType.ParkingLot).Select(e => new { type = e.Type.ToString(), e.Text }),
                        followUps,
                    },
            },
            Json);
    }

    /// <summary>Loads the facts for a team.</summary>
    /// <param name="teamId">Team id.</param>
    /// <param name="sprints">Window size.</param>
    /// <param name="history">History source.</param>
    /// <param name="sessions">Sessions.</param>
    /// <param name="followUps">Follow-ups.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The KPIs, window, whether illustrative, and facts JSON.</returns>
    public static async Task<(KpiSummary Kpi, IReadOnlyList<SprintHistory> Window, bool Illustrative, string Json)> LoadAsync(
        string teamId,
        int sprints,
        IKpiHistorySource history,
        ISessionRepository sessions,
        IFollowUpRepository followUps,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(followUps);
        var h = await history.GetAsync(teamId, ct);
        var window = KpiCalculator.Window(h.Sprints, sprints);
        var kpi = KpiCalculator.Summarize(window);
        var current = (await sessions.ListAsync(ct)).FirstOrDefault(s => s.TeamId == teamId);
        var lines = current is null
            ? []
            : (await followUps.ListBySessionAsync(current.Id, ct)).Select(f => $"{f.Kind} for {f.MemberId ?? "team"}: {f.Status}").ToList();
        return (kpi, window, h.Illustrative, Build(kpi, window, current, lines));
    }
}
