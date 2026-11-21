using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
namespace Mcm.Scrum.Application.Features.Standups.ControlStandup;

/// <summary>Forwards the action to the running voice gateway.</summary>
internal sealed class ControlStandupHandler(ILiveSessions live) : IRequestHandler<ControlStandupCommand, Result>
{
    public Task<Result> Handle(ControlStandupCommand request, CancellationToken ct) =>
        Task.FromResult(live.TrySend(request.SessionId, request.Action) ? Result.Success() : Result.Failure(StandupErrors.NotLive(request.SessionId)));
}
