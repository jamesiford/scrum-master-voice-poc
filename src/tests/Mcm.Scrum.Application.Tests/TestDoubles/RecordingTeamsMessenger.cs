using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Application.Tests.TestDoubles;

internal sealed class RecordingTeamsMessenger : ITeamsMessenger
{
    public List<TeamsMessage> Sent { get; } = [];

    public Task SendAsync(TeamsMessage message, CancellationToken ct)
    {
        if (!Sent.Any(m => m.DedupeKey == message.DedupeKey))
        {
            Sent.Add(message);
        }

        return Task.CompletedTask;
    }
}
