using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
namespace Zava.Scrum.Application.Features.Standups.GetLiveState;

/// <summary>Reads the live state.</summary>
internal sealed class GetLiveStateHandler(ILiveSessions live) : IRequestHandler<GetLiveStateQuery, Result<string>>
{
    public Task<Result<string>> Handle(GetLiveStateQuery request, CancellationToken ct) =>
        Task.FromResult(live.GetState(request.SessionId) is { } state ? Result<string>.Success(state) : Result<string>.Failure(StandupErrors.NotLive(request.SessionId)));
}
