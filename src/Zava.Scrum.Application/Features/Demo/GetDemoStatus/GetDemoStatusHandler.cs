using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
namespace Zava.Scrum.Application.Features.Demo.GetDemoStatus;

/// <summary>Counts demo records.</summary>
internal sealed class GetDemoStatusHandler(ISessionRepository sessions, IFollowUpRepository followUps, ICardOutbox cards) : IRequestHandler<GetDemoStatusQuery, Result<DemoStatus>>
{
    public async Task<Result<DemoStatus>> Handle(GetDemoStatusQuery request, CancellationToken ct) =>
        Result<DemoStatus>.Success(new DemoStatus(
            (await sessions.ListAsync(ct)).Count(s => s.TeamId == request.TeamId),
            (await followUps.ListAsync(request.TeamId, null, ct)).Count,
            (await cards.ListAsync(null, ct)).Count(c => c.Message.TeamId == request.TeamId)));
}
