using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;
namespace Zava.Scrum.Application.Features.Demo.ResetDemo;

/// <summary>Preserves the legacy endpoint with safe tenant-scoped demo-only publication instead of a global wipe.</summary>
internal sealed class ResetDemoHandler(IDemoReseed reset) : IRequestHandler<ResetDemoCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ResetDemoCommand request, CancellationToken ct) =>
        request.Decision is null || !request.Decision.Roles.Contains(AppRoles.Admin, StringComparer.Ordinal)
            ? Task.FromResult(Result.Failure(Error.Forbidden("demo.admin_required", "Demo reset requires a validated Admin caller.")))
            : reset.ClearAsync(request.Decision.Actor.TenantId, ct);
}
