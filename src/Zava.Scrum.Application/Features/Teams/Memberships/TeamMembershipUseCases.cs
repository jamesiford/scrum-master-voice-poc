using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.Memberships;

/// <summary>Lists a team's memberships.</summary>
/// <param name="TeamId">The team.</param>
public sealed record ListMembershipsQuery(string TeamId) : IQuery<IReadOnlyList<TeamMembership>>;

/// <summary>Adds a person to a team or changes their role. Managed by an Admin or the team's lead.</summary>
/// <param name="TeamId">The team.</param>
/// <param name="ObjectId">The person's Entra object id.</param>
/// <param name="Role">The role.</param>
public sealed record SetMembershipCommand(string TeamId, Guid ObjectId, TeamRole Role) : ICommand;

/// <summary>Removes a person from a team.</summary>
/// <param name="TeamId">The team.</param>
/// <param name="ObjectId">The person's Entra object id.</param>
public sealed record RemoveMembershipCommand(string TeamId, Guid ObjectId) : ICommand;

/// <summary>Adds the members of the team's Entra group as <see cref="TeamRole.Member"/>. Existing roles are kept and nobody is removed.</summary>
/// <param name="TeamId">The team.</param>
public sealed record SyncMembershipsFromGroupCommand(string TeamId) : ICommand<int>;

internal sealed class ListMembershipsHandler(ITeamMembershipRepository memberships) : IRequestHandler<ListMembershipsQuery, Result<IReadOnlyList<TeamMembership>>>
{
    public async Task<Result<IReadOnlyList<TeamMembership>>> Handle(ListMembershipsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<TeamMembership>>.Success(await memberships.ListByTeamAsync(request.TeamId, ct));
}

internal sealed class SetMembershipHandler(ITeamRepository teams, ITeamMembershipRepository memberships) : IRequestHandler<SetMembershipCommand, Result>
{
    public async Task<Result> Handle(SetMembershipCommand request, CancellationToken ct)
    {
        if (request.ObjectId == Guid.Empty)
        {
            return Result.Failure(Zava.Scrum.Domain.Identity.IdentityErrors.ObjectIdRequired);
        }

        if (await teams.GetAsync(request.TeamId, ct) is not { IsOpen: true })
        {
            return Result.Failure(TeamErrors.NotFound(request.TeamId));
        }

        await memberships.UpsertAsync(new TeamMembership(request.TeamId, request.ObjectId, request.Role), ct);
        return Result.Success();
    }
}

internal sealed class RemoveMembershipHandler(ITeamMembershipRepository memberships) : IRequestHandler<RemoveMembershipCommand, Result>
{
    public async Task<Result> Handle(RemoveMembershipCommand request, CancellationToken ct)
    {
        await memberships.RemoveAsync(request.TeamId, request.ObjectId, ct);
        return Result.Success();
    }
}

internal sealed class SyncMembershipsFromGroupHandler(ITeamRepository teams, ITeamMembershipRepository memberships, IEntraGroupReader groups) : IRequestHandler<SyncMembershipsFromGroupCommand, Result<int>>
{
    public async Task<Result<int>> Handle(SyncMembershipsFromGroupCommand request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result<int>.Failure(TeamErrors.NotFound(request.TeamId));
        }

        if (!Guid.TryParse(team.EntraGroupId, out var group))
        {
            return Result<int>.Failure(Error.Invalid("team.no_group", "This team has no Entra group to sync from."));
        }

        var members = await groups.ListMembersAsync(group, ct);
        if (members is null)
        {
            return Result<int>.Failure(Error.ExternalRejected("team.group_unreadable", "The Entra group could not be read. Check the id and that the API's identity may read groups."));
        }

        var existing = (await memberships.ListByTeamAsync(team.Id, ct)).Select(m => m.ObjectId).ToHashSet();
        var added = 0;
        foreach (var id in members.Where(id => !existing.Contains(id)))
        {
            await memberships.UpsertAsync(new TeamMembership(team.Id, id, TeamRole.Member), ct);
            added++;
        }

        return Result<int>.Success(added);
    }
}
