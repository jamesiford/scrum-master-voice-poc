using System.Collections.Concurrent;
using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Api.Voice;

/// <summary>Tracks the standups running on this instance so the portal can watch and steer them (SPEC M5).</summary>
/// <remarks>PROD: with more than one API replica, route control through Service Bus or sticky routing to the owning instance.</remarks>
public sealed class LiveSessionRegistry : ILiveSessions
{
    private sealed record Entry(Action<string> Send, string State);

    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();

    /// <summary>Registers a running standup.</summary>
    /// <param name="sessionId">The standup.</param>
    /// <param name="send">Delivers a control action to its gateway.</param>
    public void Register(Guid sessionId, Action<string> send) => _entries[sessionId] = new Entry(send, "{}");

    /// <summary>Stores the latest state pushed by the gateway.</summary>
    /// <param name="sessionId">The standup.</param>
    /// <param name="stateJson">The state as JSON.</param>
    public void Update(Guid sessionId, string stateJson)
    {
        if (_entries.TryGetValue(sessionId, out var e))
        {
            _entries[sessionId] = e with { State = stateJson };
        }
    }

    /// <summary>Removes a standup that has finished.</summary>
    /// <param name="sessionId">The standup.</param>
    public void Unregister(Guid sessionId) => _entries.TryRemove(sessionId, out _);

    /// <inheritdoc />
    public bool TrySend(Guid sessionId, string action)
    {
        if (!_entries.TryGetValue(sessionId, out var e))
        {
            return false;
        }

        e.Send(action);
        return true;
    }

    /// <inheritdoc />
    public string? GetState(Guid sessionId) => _entries.TryGetValue(sessionId, out var e) ? e.State : null;
}