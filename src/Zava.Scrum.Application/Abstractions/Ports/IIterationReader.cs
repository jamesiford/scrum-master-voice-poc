using Zava.Scrum.Domain.Teams;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Reads iteration paths from the work tracker. Kept apart from <see cref="IWorkItemReader"/> because only ceremony packs need it (SOLID: I).</summary>
public interface IIterationReader
{
    /// <summary>Gets the path of the team's next iteration.</summary>
    /// <param name="team">The team.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The iteration path, or <see langword="null"/> when there is no future iteration or it could not be read.</returns>
    Task<string?> GetNextIterationPathAsync(Team team, CancellationToken ct);

    /// <summary>Finds an iteration with its dates (M9d: the sprint calendar).</summary>
    /// <param name="team">The team.</param>
    /// <param name="name">The iteration's short name or path; <see langword="null"/> for the next iteration, or the current one when there is no future iteration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The iteration, or <see langword="null"/> when none matches or it has no dates.</returns>
    Task<IterationInfo?> FindIterationAsync(Team team, string? name, CancellationToken ct);
}
