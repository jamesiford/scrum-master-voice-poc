using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Standups;

namespace Zava.Scrum.Application.Tests.TestDoubles;

internal sealed class FakeRetroSettings : IRetroSettings
{
    public int Minutes => 15;

    public RetroFormat Format => RetroFormat.WentWell;
}
