using Zava.Scrum.Domain.FollowUps;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Persistence for <see cref="FollowUp"/>. Every implementation must pass the shared contract test (SOLID: L).</summary>
public interface IFollowUpRepository
{
    /// <summary>Gets a follow-up.</summary>
    /// <param name="id">Follow-up id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The follow-up, or <see langword="null"/>.</returns>
    Task<FollowUp?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Lists follow-ups, newest first.</summary>
    /// <param name="teamId">Team id.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The follow-ups.</returns>
    Task<IReadOnlyList<FollowUp>> ListAsync(string teamId, FollowUpStatus? status, CancellationToken ct);

    /// <summary>Lists the follow-ups of one standup.</summary>
    /// <param name="sessionId">Session id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The follow-ups.</returns>
    Task<IReadOnlyList<FollowUp>> ListBySessionAsync(Guid sessionId, CancellationToken ct);

    /// <summary>Inserts or replaces a follow-up.</summary>
    /// <param name="followUp">The follow-up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SaveAsync(FollowUp followUp, CancellationToken ct);
}
