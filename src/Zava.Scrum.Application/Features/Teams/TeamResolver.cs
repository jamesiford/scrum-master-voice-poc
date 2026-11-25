using Microsoft.Extensions.Logging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams;

/// <summary>The resolver. A refusal is logged with the caller and the team asked for, so a probing caller leaves a trace.</summary>
internal sealed class TeamResolver(
    ITeamRepository teams,
    ITeamMembershipRepository memberships,
    ISessionRepository sessions,
    IIdentityPolicy identity,
    ILogger<TeamResolver> logger) : ITeamResolver
{
    public async Task<Result<Team>> ForCallerAsync(CallerContext caller, string? teamId, TeamPermission permission, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(caller);
        if (string.IsNullOrWhiteSpace(teamId))
        {
            return Refuse<Team>(caller, "(none)", TeamErrors.NoTeam);
        }

        var team = await teams.GetAsync(teamId, ct);
        var membership = (await memberships.ListByMemberAsync(caller.ObjectId, ct)).FirstOrDefault(m => m.TeamId == teamId);
        var allowed = TeamAccess.Authorise(teamId, caller.IsAdmin, membership, permission);
        if (team is null || !allowed.IsSuccess)
        {
            return Refuse<Team>(caller, teamId, allowed.IsSuccess ? TeamAccess.NotFound(teamId) : allowed.Error!);
        }

        if (!team.IsOpen && permission != TeamPermission.Read)
        {
            return Refuse<Team>(caller, teamId, TeamErrors.Archived(teamId));
        }

        return Result<Team>.Success(team);
    }

    public async Task<IReadOnlyList<Team>> ListForCallerAsync(CallerContext caller, bool includeArchived, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(caller);
        var all = await teams.ListAsync(ct);
        IEnumerable<Team> visible;
        if (caller.IsAdmin)
        {
            visible = all;
        }
        else
        {
            var mine = (await memberships.ListByMemberAsync(caller.ObjectId, ct)).Select(m => m.TeamId).ToHashSet(StringComparer.Ordinal);
            visible = all.Where(t => mine.Contains(t.Id) && t.Status != TeamStatus.Draft);
        }

        return visible.Where(t => includeArchived && caller.IsAdmin || t.IsOpen).OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<Result<Team>> ForConversationAsync(string? tenantId, string? conversationId, CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var tenant) || tenant != identity.TenantId || string.IsNullOrWhiteSpace(conversationId))
        {
            logger.LogWarning("Refused conversation {Conversation}: tenant {Tenant} is not this deployment's", conversationId, tenantId);
            return Result<Team>.Failure(TeamErrors.NoTeam);
        }

        var found = (await teams.ListAsync(ct)).Where(t => t.Status == TeamStatus.Active && t.Bindings.Any(b => b.Matches(conversationId))).ToList();
        if (found.Count == 1)
        {
            return Result<Team>.Success(found[0]);
        }

        logger.LogWarning("Conversation {Conversation} resolves to {Count} teams; nothing happens until an Admin or team lead binds it", conversationId, found.Count);
        return Result<Team>.Failure(found.Count == 0 ? TeamErrors.Unbound : TeamErrors.AmbiguousBinding);
    }

    public async Task<Result<Team>> ForUserAsync(Guid objectId, string? chosenTeamId, CancellationToken ct)
    {
        var mine = await memberships.ListByMemberAsync(objectId, ct);
        var open = new List<Team>();
        foreach (var m in mine)
        {
            if (await teams.GetAsync(m.TeamId, ct) is { IsOpen: true, Status: not TeamStatus.Draft } t)
            {
                open.Add(t);
            }
        }

        if (!string.IsNullOrWhiteSpace(chosenTeamId))
        {
            return open.FirstOrDefault(t => t.Id == chosenTeamId) is { } chosen
                ? Result<Team>.Success(chosen)
                : Refuse<Team>(new CallerContext(objectId, false), chosenTeamId, TeamAccess.NotFound(chosenTeamId));
        }

        return open.Count switch
        {
            0 => Refuse<Team>(new CallerContext(objectId, false), "(none)", TeamErrors.NoTeam),
            1 => Result<Team>.Success(open[0]),
            _ => Result<Team>.Failure(TeamErrors.Ambiguous(open.Select(t => t.Name))),
        };
    }

    public async Task<Result<Team>> ForSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var session = await sessions.GetAsync(sessionId, ct);
        var team = session is null ? null : await teams.GetAsync(session.TeamId, ct);
        return team is null
            ? Result<Team>.Failure(Error.NotFound("team.session_has_no_team", $"Session '{sessionId}' does not resolve to a team."))
            : Result<Team>.Success(team);
    }

    private Result<T> Refuse<T>(CallerContext caller, string teamId, Error error)
    {
        logger.LogWarning("Refused {Code} for caller {Caller} on team {TeamId}", error.Code, caller.ObjectId, teamId);
        return Result<T>.Failure(error);
    }
}
