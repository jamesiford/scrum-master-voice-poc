using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Features.Prompts.GetPrompt;
using Mcm.Scrum.Application.Features.Prompts.ListPrompts;
using Mcm.Scrum.Application.Features.Prompts.UpdatePrompt;
using Mcm.Scrum.Application.Features.Schedules.GetSchedule;
using Mcm.Scrum.Application.Features.Schedules.UpdateSchedule;

namespace Mcm.Scrum.Api.Endpoints;

/// <summary>Schedule and prompt endpoints (SPEC 14). Each route sends exactly one command or query.</summary>
public static class ScheduleEndpoints
{
    /// <summary>Maps <c>/api/teams/{id}/schedule</c> and <c>/api/teams/{id}/prompts</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams/{teamId}");

        group.MapGet("/schedule", async (string teamId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetScheduleQuery(teamId), ct)).ToHttpResult());

        group.MapPut("/schedule", async (string teamId, ScheduleBody b, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateScheduleCommand(teamId, b.Enabled, b.Days, b.StartTime, b.Mode, b.JoinUrl), ct)).ToHttpResult());

        group.MapGet("/prompts", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListPromptsQuery(), ct)).ToHttpResult());

        group.MapGet("/prompts/{name}", async (string name, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPromptQuery(name), ct)).ToHttpResult());

        group.MapPut("/prompts/{name}", async (string name, PromptBody b, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdatePromptCommand(name, b.Text), ct)).ToHttpResult());

        return app;
    }

    private sealed record ScheduleBody(bool Enabled, IReadOnlyList<string> Days, string StartTime, string Mode, string? JoinUrl);

    private sealed record PromptBody(string Text);
}