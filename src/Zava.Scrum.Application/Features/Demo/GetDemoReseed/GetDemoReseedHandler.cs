using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Demo.ReseedDemo;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Application.Features.Demo.GetDemoReseed;

/// <summary>Checks the app role before exposing the reset journal.</summary>
internal sealed class GetDemoReseedHandler(IDemoReseed reset) : IRequestHandler<GetDemoReseedQuery, Result<DemoReseedDto>>
{
    /// <inheritdoc />
    public Task<Result<DemoReseedDto>> Handle(GetDemoReseedQuery request, CancellationToken ct) =>
        request.Decision.Roles.Contains(AppRoles.Admin, StringComparer.Ordinal)
            ? reset.GetAsync(request.TeamId, request.OperationId, request.Decision.Actor, ct)
            : Task.FromResult(Result<DemoReseedDto>.Failure(Error.Forbidden("demo.admin_required", "Demo re-seeding requires the Admin app role.")));
}
