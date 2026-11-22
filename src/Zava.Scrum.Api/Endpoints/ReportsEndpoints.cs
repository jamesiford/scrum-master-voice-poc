using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Reports.AnswerTeamQuestion;
using Zava.Scrum.Application.Features.Reports.GetKpis;
using Zava.Scrum.Application.Features.Reports.GetMonthlyReport;
using Zava.Scrum.Application.Features.Reports.GetSprintReport;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Report and Q&amp;A endpoints (SPEC 14). Each route sends exactly one query.</summary>
public static class ReportsEndpoints
{
    /// <summary>Maps <c>/api/reports</c> and <c>/api/ask</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports");
        group.MapGet("/kpis", async (string teamId, int? sprints, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetKpisQuery(teamId, sprints ?? 6), ct)).ToHttpResult());
        group.MapGet("/sprint", async (string teamId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetSprintReportQuery(teamId), ct)).ToHttpResult());
        group.MapGet("/monthly", async (string teamId, int? sprints, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMonthlyReportQuery(teamId, sprints ?? 6), ct)).ToHttpResult());
        app.MapPost("/api/ask", async (AskBody b, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AnswerTeamQuestionQuery(b.TeamId, b.Question), ct)).ToHttpResult());
        return app;
    }

    /// <summary>Request body for <c>POST /api/ask</c>.</summary>
    /// <param name="TeamId">Team id.</param>
    /// <param name="Question">The question.</param>
    public sealed record AskBody(string TeamId, string Question);
}
