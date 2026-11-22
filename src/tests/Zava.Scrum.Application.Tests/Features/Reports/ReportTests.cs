using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Reports.AnswerTeamQuestion;
using Zava.Scrum.Application.Features.Reports.GetSprintReport;
using Zava.Scrum.Application.Tests.TestDoubles;
using Zava.Scrum.Domain.Kpis;
using Microsoft.Extensions.DependencyInjection;

namespace Zava.Scrum.Application.Tests.Features.Reports;

public sealed class ReportTests
{
    private static SprintHistory Sprint(int n, double committed, double completed) =>
        new($"Sprint {n}", new DateOnly(2026, 1, n), new DateOnly(2026, 1, n + 1), committed, completed, 1, 2, 3, 90, 80, 1, 12);

    private sealed class Source(bool illustrative) : IKpiHistorySource
    {
        public Task<KpiHistory> GetAsync(string teamId, CancellationToken ct) =>
            Task.FromResult(new KpiHistory([Sprint(1, 40, 30), Sprint(2, 40, 40), Sprint(3, 20, 20)], illustrative));
    }

    private sealed class SilentAgent : IReportingAgent
    {
        public Task<string?> NarrateAsync(string kind, string factsJson, CancellationToken ct) => Task.FromResult<string?>(null);

        public Task<string?> AnswerAsync(string question, string factsJson, CancellationToken ct) => Task.FromResult<string?>(null);
    }

    [Fact]
    public async Task SprintReport_ModelUnavailable_UsesCodeComputedNumbersAndIllustrativeLabel()
    {
        await using var h = await StandupHarness.CreateAsync(configure: s =>
        {
            s.AddSingleton<IKpiHistorySource>(new Source(true));
            s.AddSingleton<IReportingAgent>(new SilentAgent());
        });

        var report = await h.Sender.Send(new GetSprintReportQuery("t1"));

        Assert.False(report.Value.NarrativeFromModel);
        Assert.True(report.Value.Illustrative);
        Assert.Contains("90%", report.Value.Markdown, StringComparison.Ordinal);
        Assert.Contains("illustrative", report.Value.Markdown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ask_ModelUnavailable_ReturnsKpiTableInsteadOfInventingAnAnswer()
    {
        await using var h = await StandupHarness.CreateAsync(configure: s =>
        {
            s.AddSingleton<IKpiHistorySource>(new Source(false));
            s.AddSingleton<IReportingAgent>(new SilentAgent());
        });

        var answer = await h.Sender.Send(new AnswerTeamQuestionQuery("t1", "How are we doing?"));

        Assert.Contains("Say/do ratio", answer.Value, StringComparison.Ordinal);
    }
}
