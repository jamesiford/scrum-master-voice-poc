using Zava.Scrum.Domain.Members;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Persistence for a team's roster.</summary>
public interface IMemberRepository
{
    /// <summary>Lists a team's members in speaking order.</summary>
    /// <param name="teamId">Team id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The members.</returns>
    Task<IReadOnlyList<Member>> ListAsync(string teamId, CancellationToken ct);

    /// <summary>Replaces a team's whole roster.</summary>
    /// <param name="teamId">Team id.</param>
    /// <param name="members">The new roster.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task ReplaceAsync(string teamId, IReadOnlyList<Member> members, CancellationToken ct);
}
