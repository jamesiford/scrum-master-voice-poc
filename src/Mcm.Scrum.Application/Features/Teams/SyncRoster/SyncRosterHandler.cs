using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Teams.SyncRoster;

/// <summary>Merges the source roster into the stored one by member key.</summary>
internal sealed class SyncRosterHandler(ITeamRepository teams, IMemberRepository members, IRosterSource source)
    : IRequestHandler<SyncRosterCommand, Result>
{
    public async Task<Result> Handle(SyncRosterCommand request, CancellationToken ct)
    {
        var team = await teams.GetAsync(request.TeamId, ct);
        if (team is null)
        {
            return Result.Failure(TeamErrors.NotFound(request.TeamId));
        }

        var existing = (await members.ListAsync(team.Id, ct)).ToDictionary(m => m.Key, StringComparer.OrdinalIgnoreCase);
        var merged = (await source.GetRosterAsync(team, ct))
            .Select(m => existing.TryGetValue(m.Key, out var old)
                ? m with { Active = old.Active, Pronunciation = old.Pronunciation, SpeakingOrder = old.SpeakingOrder, AvailabilityPct = old.AvailabilityPct }
                : m)
            .ToList();

        await members.ReplaceAsync(team.Id, merged, ct);
        return Result.Success();
    }
}
