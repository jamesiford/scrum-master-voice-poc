using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Health.GetHealth;

/// <summary>Answers <see cref="GetHealthQuery"/> by reading the session store, proving the store port is wired.</summary>
internal sealed class GetHealthHandler(ISessionRepository sessions, IClock clock)
    : IRequestHandler<GetHealthQuery, Result<HealthDto>>
{
    public async Task<Result<HealthDto>> Handle(GetHealthQuery request, CancellationToken ct)
    {
        var all = await sessions.ListAsync(ct);
        return Result<HealthDto>.Success(new HealthDto("healthy", clock.UtcNow, all.Count));
    }
}
