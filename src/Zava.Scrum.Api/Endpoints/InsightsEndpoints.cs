using Zava.Scrum.Api.Auth;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.FollowUps.ListEmails;
using Zava.Scrum.Application.Features.Models.GetModelComparison;
using Zava.Scrum.Application.Features.Retrospectives.GetRetrospective;
using Zava.Scrum.Application.Features.Retrospectives.PrepareRetrospective;
using Zava.Scrum.Application.Features.Stories.DraftUserStory;
using Zava.Scrum.Application.Features.Stories.ListParkedTopics;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Stretch endpoints (SPEC M8): retrospective, Story Writer, model comparison and the email outbox. Each route sends one query.</summary>
public static class InsightsEndpoints
{
    /// <summary>Maps <c>/api/retrospective</c>, <c>/api/stories</c>, <c>/api/models</c> and <c>/api/emails</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapInsightsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/retrospective", async (string teamId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetRetrospectiveQuery(teamId), ct)).ToHttpResult());
        app.MapPost("/api/retrospective/prepare", async (PrepareRetroBody b, ISender sender, CancellationToken ct) =>
            (await sender.Send(new PrepareRetrospectiveCommand(b.TeamId), ct)).ToHttpResult());
        app.MapGet("/api/stories/topics", async (string teamId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListParkedTopicsQuery(teamId), ct)).ToHttpResult());
        app.MapPost("/api/stories/draft", async (DraftBody b, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DraftUserStoryQuery(b.TeamId, b.Topic), ct)).ToHttpResult());
        app.MapGet("/api/models", async (string teamId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetModelComparisonQuery(teamId), ct)).ToHttpResult());
        app.MapGet("/api/emails", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListEmailsQuery(), ct)).ToHttpResult()).RequireRegistry();
        return app;
    }

    /// <summary>Request body for <c>POST /api/retrospective/prepare</c>.</summary>
    /// <param name="TeamId">Team id.</param>
    public sealed record PrepareRetroBody(string TeamId);

    /// <summary>Request body for <c>POST /api/stories/draft</c>.</summary>
    /// <param name="TeamId">Team id.</param>
    /// <param name="Topic">The topic or parked decision.</param>
    public sealed record DraftBody(string TeamId, string Topic);
}
