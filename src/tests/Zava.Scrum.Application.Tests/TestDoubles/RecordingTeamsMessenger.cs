using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Application.Tests.TestDoubles;

internal sealed class RecordingTeamsMessenger : ITeamsMessenger
{
    public List<TeamsMessage> Sent { get; } = [];

    public List<TeamsMessage> Updated { get; } = [];

    public Task UpdateAsync(TeamsMessage message, CancellationToken ct)
    {
        Updated.Add(message);
        return Task.CompletedTask;
    }

    public Task SendAsync(TeamsMessage message, CancellationToken ct)
    {
        if (!Sent.Any(m => m.DedupeKey == message.DedupeKey))
        {
            Sent.Add(message);
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakeMeetingBridge : IMeetingBridge
{
    public List<Guid> Left { get; } = [];

    public Task<Zava.Scrum.Domain.Common.Result> JoinAsync(MeetingJoinRequest request, CancellationToken ct) => Task.FromResult(Zava.Scrum.Domain.Common.Result.Success());

    public Task LeaveAsync(Guid sessionId, CancellationToken ct)
    {
        Left.Add(sessionId);
        return Task.CompletedTask;
    }
}
