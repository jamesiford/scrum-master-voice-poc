using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Identity;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Planning;

namespace Zava.Scrum.Application.Features.Planning.RejectPlan;

/// <summary>Rejects the pack and replaces its card with the outcome. No follow-up is created, so nothing can be written.</summary>
internal sealed class RejectPlanHandler(IPlanningPackRepository packs, ApprovalGate gate, ITeamsMessenger messenger, IClock clock) : IRequestHandler<RejectPlanCommand, Result>
{
    public async Task<Result> Handle(RejectPlanCommand request, CancellationToken ct)
    {
        var pack = await packs.GetAsync(request.PackId, ct);
        if (pack is null)
        {
            return Result.Failure(PlanningErrors.NotFound(request.PackId));
        }

        var allowed = await gate.AuthoriseAsync(request.Decision, pack.Data.TeamId, null, forScrumMaster: true, ct);
        if (!allowed.IsSuccess)
        {
            return allowed;
        }

        var rejected = pack.Reject(request.Decision.Actor.DisplayName, clock.UtcNow);
        if (!rejected.IsSuccess)
        {
            return rejected;
        }

        await packs.SaveAsync(pack, ct);
        await messenger.UpdateAsync(PlanningCards.Result(pack.Data, $"Rejected by {pack.Data.DecidedBy}. Nothing was written."), ct);
        return Result.Success();
    }
}
