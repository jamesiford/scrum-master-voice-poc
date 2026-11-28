using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Application.Features.Demo.ReseedDemo;

/// <summary>Requires the Admin app role even when invoked without an HTTP endpoint.</summary>
internal sealed class ReseedDemoHandler(IDemoReseed reset) : IRequestHandler<ReseedDemoCommand, Result<Guid>>
{
    /// <inheritdoc />
    public Task<Result<Guid>> Handle(ReseedDemoCommand request, CancellationToken ct) =>
        !request.Decision.Roles.Contains(AppRoles.Admin, StringComparer.Ordinal)
            ? Task.FromResult(Result<Guid>.Failure(Error.Forbidden("demo.admin_required", "Demo re-seeding requires the Admin app role.")))
            : request.OperationId is { } id
                ? reset.CommitAsync(request.TeamId, id, request.Decision.Actor, ct)
                : reset.StageAsync(request.TeamId, request.Decision.Actor, ct);
}
