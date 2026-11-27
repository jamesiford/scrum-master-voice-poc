using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Application.Features.Demo.OptInDemoScope;

/// <summary>Requires the Admin app role before applying an explicit demo scope manifest.</summary>
internal sealed class OptInDemoScopeHandler(IDemoReseed reset) : IRequestHandler<OptInDemoScopeCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(OptInDemoScopeCommand request, CancellationToken ct) =>
        request.Decision.Roles.Contains(AppRoles.Admin, StringComparer.Ordinal)
            ? reset.OptInAsync(request.Manifest, request.Decision.Actor, ct)
            : Task.FromResult(Result.Failure(Error.Forbidden("demo.admin_required", "Demo scope opt-in requires the Admin app role.")));
}
