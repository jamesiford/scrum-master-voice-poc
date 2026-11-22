using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Infrastructure;

/// <summary>Wall-clock <see cref="IClock"/> backed by <see cref="TimeProvider"/>.</summary>
public sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
