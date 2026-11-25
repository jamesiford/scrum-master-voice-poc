using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.BindConversation;

/// <summary>
/// Binds the Teams conversation the onboarding card came from to a team. The caller must be an Admin or that team's lead (checked by Entra
/// object id, from the card action's activity).
/// </summary>
/// <param name="Caller">Who pressed Bind.</param>
/// <param name="TeamId">The team to bind it to.</param>
/// <param name="TenantId">The tenant on the activity.</param>
/// <param name="TeamsTeamId">The Teams team (group) id, for a channel.</param>
/// <param name="ConversationId">The conversation id.</param>
/// <param name="IsChannel">True for a channel, false for a chat.</param>
/// <param name="Purpose">What the conversation is for.</param>
public sealed record BindTeamsConversationCommand(CallerContext Caller, string TeamId, string TenantId, string? TeamsTeamId, string ConversationId, bool IsChannel, BindingPurpose Purpose) : ICommand;

internal sealed class BindTeamsConversationHandler(ITeamResolver resolver, ITeamRepository teams, IIdentityPolicy identity) : IRequestHandler<BindTeamsConversationCommand, Result>
{
    public async Task<Result> Handle(BindTeamsConversationCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(request.TenantId, out var tenant) || tenant != identity.TenantId)
        {
            return Result.Failure(Zava.Scrum.Domain.Identity.IdentityErrors.WrongTenant);
        }

        var team = await resolver.ForCallerAsync(request.Caller, request.TeamId, TeamPermission.Operate, ct);
        if (!team.IsSuccess)
        {
            return Result.Failure(team.Error!);
        }

        // A conversation belongs to one team: unbind it from any other first.
        foreach (var other in (await teams.ListAsync(ct)).Where(t => t.Id != team.Value.Id && t.Bindings.Any(b => b.Matches(request.ConversationId))))
        {
            await teams.SaveAsync(other with { TeamsBindings = other.Bindings.Where(b => !b.Matches(request.ConversationId)).ToList() }, ct);
        }

        var binding = new TeamsBinding(request.TenantId, request.IsChannel ? request.TeamsTeamId : null, request.IsChannel ? request.ConversationId : null, request.IsChannel ? null : request.ConversationId, request.Purpose);
        var bindings = team.Value.Bindings.Where(b => !(b.Matches(request.ConversationId) && b.Purpose == request.Purpose)).Append(binding).ToList();
        await teams.SaveAsync(team.Value with { TeamsBindings = bindings }, ct);
        return Result.Success();
    }
}
