using Zava.Scrum.Application.Abstractions.Ports;
using Microsoft.Extensions.Options;
using Zava.Scrum.Infrastructure.Ado;

namespace Zava.Scrum.Infrastructure.Persistence;

/// <summary>Preserves the legacy reset port with explicit demo-only atomic publication; no collections or real-team sessions are globally deleted.</summary>
public sealed class JsonFileDemoReset(IDemoReseed reset, IOptions<DemoOptions> options) : IDemoReset
{
    /// <inheritdoc />
    public async Task ResetAsync(CancellationToken ct)
    {
        var result = await reset.ClearAsync(options.Value.ResetTenantId, ct);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Error!.Message);
        }
    }
}
