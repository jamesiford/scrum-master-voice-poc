using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Planning;

namespace Zava.Scrum.Application.Features.Planning.GetPlanningPack;

/// <summary>Loads the requested or latest pack.</summary>
internal sealed class GetPlanningPackHandler(IPlanningPackRepository packs) : IRequestHandler<GetPlanningPackQuery, Result<PlanningPackData>>
{
    public async Task<Result<PlanningPackData>> Handle(GetPlanningPackQuery request, CancellationToken ct)
    {
        var pack = request.PackId is { } id ? await packs.GetAsync(id, ct) : await packs.GetLatestAsync(request.TeamId, ct);
        if (pack is null)
        {
            return Result<PlanningPackData>.Failure(request.PackId is { } missing ? PlanningErrors.NotFound(missing) : PlanningErrors.NoPack);
        }

        return Result<PlanningPackData>.Success(pack.Data);
    }
}
