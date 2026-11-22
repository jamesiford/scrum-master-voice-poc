using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Application.Features.Teams;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Standups.GetWorkItemDetails;

/// <summary>Looks the item up in the live sprint so due dates and states are current, not as of the pre-brief.</summary>
internal sealed class GetWorkItemDetailsHandler(ISessionRepository sessions, ITeamRepository teams, IWorkItemReader reader, IClock clock)
    : IRequestHandler<GetWorkItemDetailsQuery, Result<WorkItemDetails>>
{
    public async Task<Result<WorkItemDetails>> Handle(GetWorkItemDetailsQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<WorkItemDetails>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var team = await teams.GetAsync(session.TeamId, ct);
        if (team is null)
        {
            return Result<WorkItemDetails>.Failure(TeamErrors.NotFound(session.TeamId));
        }

        var sprint = await reader.GetCurrentSprintAsync(team, ct);
        if (!sprint.IsSuccess)
        {
            return Result<WorkItemDetails>.Failure(sprint.Error!);
        }

        var item = sprint.Value.Items.FirstOrDefault(i => i.Id.Value == request.WorkItemId);
        if (item is null)
        {
            return Result<WorkItemDetails>.Failure(Error.NotFound("workitem.not_in_sprint", $"Work item {request.WorkItemId} is not in the current sprint."));
        }

        var now = clock.UtcNow;
        var overdue = !item.IsDone && item.DueDate is { } due && due < now;
        return Result<WorkItemDetails>.Success(new WorkItemDetails(
            item.Id.Value,
            item.Title,
            item.Type,
            item.State,
            item.AssignedTo,
            Math.Max(0, (int)(now - item.StateChangedAt).TotalDays),
            item.DueDate is { } d ? d.ToString("dddd d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture) : "none set",
            overdue,
            overdue ? (int)(now - item.DueDate!.Value).TotalDays : 0,
            item.StoryPoints,
            item.RemainingWork,
            item.Tags));
    }
}
