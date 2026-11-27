using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Application.Features.Demo.AbortDemoReseed;

/// <summary>Requires the app Admin role before releasing an undecided reset's guard.</summary>
internal sealed class AbortDemoReseedHandler(IDemoReseed reset) : IRequestHandler<AbortDemoReseedCommand, Result<Guid>>
{
    /// <inheritdoc />
    public Task<Result<Guid>> Handle(AbortDemoReseedCommand request, CancellationToken ct) =>
        request.Decision.Roles.Contains(AppRoles.Admin, StringComparer.Ordinal)
            ? reset.AbortAsync(request.TeamId, request.OperationId, request.Decision.Actor, ct)
            : Task.FromResult(Result<Guid>.Failure(Error.Forbidden("demo.admin_required", "Demo re-seeding requires the Admin app role.")));
}
