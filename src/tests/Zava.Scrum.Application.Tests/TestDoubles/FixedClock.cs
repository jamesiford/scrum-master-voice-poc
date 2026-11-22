using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Application.Tests.TestDoubles;

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
