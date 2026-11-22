using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Standups;

namespace Mcm.Scrum.Application.Tests.TestDoubles;

internal sealed class FakeRetroSettings : IRetroSettings
{
    public int Minutes => 15;

    public RetroFormat Format => RetroFormat.WentWell;
}
