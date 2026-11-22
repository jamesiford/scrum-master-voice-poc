using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Stories.ListParkedTopics;

/// <summary>Collects distinct parking-lot events from the team's sessions.</summary>
internal sealed class ListParkedTopicsHandler(ISessionRepository sessions) : IRequestHandler<ListParkedTopicsQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(ListParkedTopicsQuery request, CancellationToken ct)
    {
        IReadOnlyList<string> topics = (await sessions.ListAsync(ct))
            .Where(s => s.TeamId == request.TeamId)
            .SelectMany(s => s.Events)
            .Where(e => e.Type == StandupEventType.ParkingLot)
            .Select(e => e.Text.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        return Result<IReadOnlyList<string>>.Success(topics);
    }
}
