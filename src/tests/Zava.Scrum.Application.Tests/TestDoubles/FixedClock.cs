using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Application.Tests.TestDoubles;

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
