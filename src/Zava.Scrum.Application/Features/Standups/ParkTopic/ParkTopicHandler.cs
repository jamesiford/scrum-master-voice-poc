using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Features.Standups.ParkTopic;

/// <summary>Records a parking-lot event.</summary>
internal sealed class ParkTopicHandler(ISessionRepository sessions, IClock clock) : IRequestHandler<ParkTopicCommand, Result>
{
    public async Task<Result> Handle(ParkTopicCommand request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var topic = string.IsNullOrWhiteSpace(request.Topic) ? "Side discussion" : request.Topic.Trim();
        session.RecordEvent(StandupEventType.ParkingLot, null, [], topic, clock.UtcNow);
        await sessions.SaveAsync(session, ct);
        return Result.Success();
    }
}
