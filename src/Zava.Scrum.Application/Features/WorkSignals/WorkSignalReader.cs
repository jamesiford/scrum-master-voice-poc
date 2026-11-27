using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Teams;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;
using Zava.Scrum.Domain.WorkSignals;

namespace Zava.Scrum.Application.Features.WorkSignals;

/// <summary>Contract-stage read boundary, deliberately unregistered until the real Work IQ spike passes (ADR 0027).</summary>
internal sealed class WorkSignalReader(
    ITeamResolver teams, ITeamMembershipRepository memberships, IWorkSignalConsentRepository consents,
    IUserAssertionAccessor assertions, IIdentityPolicy identity, IWorkSignalScopeReader scopes,
    IWorkSignals signals, IClock clock, WorkSignalEvidencePolicy policy)
{
    /// <summary>Reads source-labelled channel activity only for an item on the selected team's board.</summary>
    /// <param name="teamId">The owner's explicit team selection.</param>
    /// <param name="workItem">The board item.</param>
    /// <param name="correlationId">The request correlation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Checked evidence or a visible refusal.</returns>
    public async Task<Result<IReadOnlyList<WorkSignalEvidence>>> GetChannelActivityAsync(
        string teamId, WorkItemId workItem, Guid correlationId, CancellationToken ct)
    {
        var prepared = await PrepareAsync(teamId, correlationId, ct);
        if (!prepared.IsSuccess)
        {
            return Result<IReadOnlyList<WorkSignalEvidence>>.Failure(prepared.Error!);
        }

        var context = prepared.Value;
        if (!context.Scope.WorkItemIds.Contains(workItem))
        {
            return Result<IReadOnlyList<WorkSignalEvidence>>.Failure(Error.Invalid("signals.item_outside_team", "The work item is not on this team's current board."));
        }

        var result = await signals.GetChannelActivityAsync(context, workItem, ct);
        return await CheckAsync(result, context, WorkSignalSource.Channel, context.Scope.ChannelId, ct);
    }

    /// <summary>Returns at most three evidenced roster identities, never an assignment.</summary>
    /// <param name="teamId">The selected team.</param>
    /// <param name="workItem">The board item that owns the topic.</param>
    /// <param name="topic">The blocker topic.</param>
    /// <param name="correlationId">The request correlation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Bounded advisory helper evidence or a refusal.</returns>
    public async Task<Result<IReadOnlyList<WorkSignalEvidence>>> SuggestHelpersAsync(
        string teamId, WorkItemId workItem, string topic, Guid correlationId, CancellationToken ct)
    {
        var prepared = await PrepareAsync(teamId, correlationId, ct);
        if (!prepared.IsSuccess)
        {
            return Result<IReadOnlyList<WorkSignalEvidence>>.Failure(prepared.Error!);
        }

        var context = prepared.Value;
        if (!context.Scope.WorkItemIds.Contains(workItem) || string.IsNullOrWhiteSpace(topic))
        {
            return Result<IReadOnlyList<WorkSignalEvidence>>.Failure(Error.Invalid("signals.topic_outside_team", "A topic and a work item on this team's board are required."));
        }

        var checkedResult = await CheckAsync(await signals.SuggestHelpersAsync(context, topic, ct), context, WorkSignalSource.Channel, context.Scope.ChannelId, ct);
        return checkedResult.IsSuccess
            ? Result<IReadOnlyList<WorkSignalEvidence>>.Success(checkedResult.Value.Where(e => e.MemberObjectId is not null).DistinctBy(e => e.MemberObjectId).Take(3).ToList())
            : checkedResult;
    }

    /// <summary>Reads existing notes only for a meeting stored against this team.</summary>
    /// <param name="teamId">The selected team.</param>
    /// <param name="meetingId">The stored meeting.</param>
    /// <param name="correlationId">The request correlation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Cited notes only, never a proposed or applied Azure DevOps change.</returns>
    public async Task<Result<IReadOnlyList<WorkSignalEvidence>>> GetMeetingNotesAsync(
        string teamId, string meetingId, Guid correlationId, CancellationToken ct)
    {
        var prepared = await PrepareAsync(teamId, correlationId, ct);
        if (!prepared.IsSuccess)
        {
            return Result<IReadOnlyList<WorkSignalEvidence>>.Failure(prepared.Error!);
        }

        var context = prepared.Value;
        if (!context.Scope.MeetingIds.Contains(meetingId))
        {
            return Result<IReadOnlyList<WorkSignalEvidence>>.Failure(Error.Invalid("signals.meeting_outside_team", "The meeting is not stored against this team."));
        }

        return await CheckAsync(await signals.GetMeetingNotesAsync(context, meetingId, ct), context, WorkSignalSource.Meeting, meetingId, ct);
    }

    private async Task<Result<WorkSignalContext>> PrepareAsync(string teamId, Guid correlationId, CancellationToken ct)
    {
        var assertion = assertions.Current;
        if (assertion is null || string.IsNullOrWhiteSpace(assertion.Token) || correlationId == Guid.Empty)
        {
            return Result<WorkSignalContext>.Failure(WorkSignalAccess.Unavailable);
        }

        var selected = await teams.ForUserAsync(assertion.ObjectId, teamId, ct);
        if (!selected.IsSuccess)
        {
            return Result<WorkSignalContext>.Failure(selected.Error!);
        }

        var team = selected.Value;
        if (!team.WorkSignalsEnabled)
        {
            return Result<WorkSignalContext>.Failure(WorkSignalAccess.Unavailable);
        }

        var consent = await consents.GetAsync(team.Id, ct);
        var owner = (await memberships.ListByTeamAsync(team.Id, ct)).SingleOrDefault(m => m.ObjectId == consent?.OwnerObjectId);
        var allowed = WorkSignalAccess.Authorise(team, consent, owner, assertion.ObjectId, assertion.TenantId, identity.TenantId);
        if (!allowed.IsSuccess)
        {
            return Result<WorkSignalContext>.Failure(allowed.Error!);
        }

        var scope = await scopes.GetAsync(team, assertion, ct);
        if (!scope.IsSuccess)
        {
            return Result<WorkSignalContext>.Failure(scope.Error!);
        }

        if (scope.Value.TeamId != team.Id || string.IsNullOrWhiteSpace(scope.Value.TeamsGroupId) || string.IsNullOrWhiteSpace(scope.Value.ChannelId))
        {
            return Result<WorkSignalContext>.Failure(Error.Conflict("signals.scope_invalid", "No signals available. An exact team channel binding is required."));
        }

        return Result<WorkSignalContext>.Success(new(team, assertion, scope.Value, correlationId));
    }

    private async Task<Result<IReadOnlyList<WorkSignalEvidence>>> CheckAsync(Result<IReadOnlyList<WorkSignalEvidence>> result,
        WorkSignalContext context, WorkSignalSource source, string resourceId, CancellationToken ct)
    {
        if (!result.IsSuccess)
        {
            return result;
        }

        var selected = await teams.ForUserAsync(context.OwnerAssertion.ObjectId, context.Team.Id, ct);
        if (!selected.IsSuccess)
        {
            return Result<IReadOnlyList<WorkSignalEvidence>>.Failure(selected.Error!);
        }

        var consent = await consents.GetAsync(context.Team.Id, ct);
        var owner = (await memberships.ListByTeamAsync(context.Team.Id, ct)).SingleOrDefault(m => m.ObjectId == consent?.OwnerObjectId);
        var allowed = WorkSignalAccess.Authorise(selected.Value, consent, owner,
            context.OwnerAssertion.ObjectId, context.OwnerAssertion.TenantId, identity.TenantId);
        if (!allowed.IsSuccess)
        {
            return Result<IReadOnlyList<WorkSignalEvidence>>.Failure(allowed.Error!);
        }

        var check = policy.Validate(result.Value, context.Scope, source, resourceId, clock.UtcNow);
        return check.IsSuccess ? result : Result<IReadOnlyList<WorkSignalEvidence>>.Failure(check.Error!);
    }
}
