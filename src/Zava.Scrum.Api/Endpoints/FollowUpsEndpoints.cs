using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.FollowUps.ApplyApprovedChanges;
using Zava.Scrum.Application.Features.FollowUps.ApproveFollowUp;
using Zava.Scrum.Application.Features.FollowUps.GetFollowUp;
using Zava.Scrum.Application.Features.FollowUps.ListFollowUps;
using Zava.Scrum.Application.Features.FollowUps.ProposeAdoChange;
using Zava.Scrum.Application.Features.FollowUps.RejectFollowUp;
using Zava.Scrum.Application.Features.FollowUps.SendFollowUps;
using Zava.Scrum.Application.Features.Teams.ListCards;
using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Api.Endpoints;

/// <summary>Follow-up and card endpoints (SPEC 14). Each route sends exactly one command or query.</summary>
public static class FollowUpsEndpoints
{
    /// <summary>Maps <c>/api/followups</c>, <c>/api/cards</c> and <c>/api/sessions/{id}/followups</c>.</summary>
    /// <param name="app">The route builder.</param>
    /// <returns>The same builder.</returns>
    public static IEndpointRouteBuilder MapFollowUpsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/followups");

        group.MapGet("/", async (string teamId, FollowUpStatus? status, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListFollowUpsQuery(teamId, status), ct)).ToHttpResult());

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetFollowUpQuery(id), ct)).ToHttpResult());

        group.MapPost("/propose", async (ProposeBody b, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ProposeAdoChangeCommand(b.SessionId, b.MemberId, b.WorkItemId, b.Op, b.Value, b.Rationale ?? string.Empty), ct)).ToHttpResult());

        group.MapPost("/{id:guid}/approve", async (Guid id, DecisionBody b, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ApproveFollowUpCommand(id, b.Approver, ApprovalChannel.Portal), ct)).ToHttpResult());

        group.MapPost("/{id:guid}/reject", async (Guid id, DecisionBody b, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RejectFollowUpCommand(id, b.Approver), ct)).ToHttpResult());

        group.MapPost("/{id:guid}/apply", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ApplyApprovedChangesCommand(id), ct)).ToHttpResult());

        app.MapPost("/api/sessions/{id:guid}/followups", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SendFollowUpsCommand(id), ct)).ToHttpResult());

        app.MapGet("/api/cards", async (Guid? sessionId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListCardsQuery(sessionId), ct)).ToHttpResult());

        return app;
    }

    private sealed record DecisionBody(string Approver);

    private sealed record ProposeBody(Guid SessionId, string MemberId, int WorkItemId, ChangeOp Op, string Value, string? Rationale);
}
