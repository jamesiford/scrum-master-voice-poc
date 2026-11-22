using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
namespace Zava.Scrum.Application.Features.Standups.ControlStandup;

/// <summary>Forwards the action to the running voice gateway.</summary>
internal sealed class ControlStandupHandler(ILiveSessions live) : IRequestHandler<ControlStandupCommand, Result>
{
    public Task<Result> Handle(ControlStandupCommand request, CancellationToken ct) =>
        Task.FromResult(live.TrySend(request.SessionId, request.Action) ? Result.Success() : Result.Failure(StandupErrors.NotLive(request.SessionId)));
}
