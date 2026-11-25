using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Teams.ListTeams;

/// <summary>Asks the resolver, so the list and every other entry point agree on what a caller may see.</summary>
internal sealed class ListTeamsHandler(ITeamResolver resolver) : IRequestHandler<ListTeamsQuery, Result<IReadOnlyList<Team>>>
{
    public async Task<Result<IReadOnlyList<Team>>> Handle(ListTeamsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<Team>>.Success(await resolver.ListForCallerAsync(request.Caller, request.IncludeArchived, ct));
}
