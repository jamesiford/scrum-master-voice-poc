using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Calendars.ApproveSprintCalendar;
using Zava.Scrum.Application.Features.Calendars.GetSprintCalendar;
using Zava.Scrum.Application.Features.Calendars.ListSprintCalendars;
using Zava.Scrum.Application.Features.Calendars.ProposeSprintCalendar;
using Zava.Scrum.Application.Features.Calendars.RejectSprintCalendar;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Sprint calendar endpoints (M9d). Each route sends one command or query.</summary>
public static class CalendarEndpoints
{
    /// <summary>Maps <c>/api/calendar</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/calendar");

        group.MapGet("/", async (string teamId, string? iteration, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetSprintCalendarQuery(teamId, iteration), ct)).ToHttpResult());

        // The fallback when Graph is not configured: import the file into Outlook by hand.
        group.MapGet("/{iteration}.ics", async (string iteration, string teamId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSprintCalendarQuery(teamId, iteration), ct);
            return result.IsSuccess ? Results.Text(result.Value.Ics, "text/calendar") : result.ToHttpResult();
        });

        group.MapGet("/proposals", async (string teamId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListSprintCalendarsQuery(teamId), ct)).ToHttpResult());

        group.MapPost("/propose", async (ProposeBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ProposeSprintCalendarCommand(body.TeamId, body.Iteration), ct)).ToHttpResult());

        group.MapPost("/proposals/{id:guid}/approve", async (Guid id, DecisionBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ApproveSprintCalendarCommand(id, body.Approver), ct)).ToHttpResult());

        group.MapPost("/proposals/{id:guid}/reject", async (Guid id, DecisionBody body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RejectSprintCalendarCommand(id, body.Approver), ct)).ToHttpResult());
        return app;
    }

    private sealed record ProposeBody(string TeamId, string? Iteration);

    private sealed record DecisionBody(string Approver);
}
