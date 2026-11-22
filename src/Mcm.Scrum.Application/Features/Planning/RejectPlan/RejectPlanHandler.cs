using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Planning;

namespace Mcm.Scrum.Application.Features.Planning.RejectPlan;

/// <summary>Rejects the pack and replaces its card with the outcome. No follow-up is created, so nothing can be written.</summary>
internal sealed class RejectPlanHandler(IPlanningPackRepository packs, ITeamsMessenger messenger, IClock clock) : IRequestHandler<RejectPlanCommand, Result>
{
    public async Task<Result> Handle(RejectPlanCommand request, CancellationToken ct)
    {
        var pack = await packs.GetAsync(request.PackId, ct);
        if (pack is null)
        {
            return Result.Failure(PlanningErrors.NotFound(request.PackId));
        }

        var rejected = pack.Reject(request.Rejecter, clock.UtcNow);
        if (!rejected.IsSuccess)
        {
            return rejected;
        }

        await packs.SaveAsync(pack, ct);
        await messenger.UpdateAsync(PlanningCards.Result(pack.Data, $"Rejected by {pack.Data.DecidedBy}. Nothing was written."), ct);
        return Result.Success();
    }
}
