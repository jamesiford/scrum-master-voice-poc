using Mcm.Scrum.Domain.Kpis;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Finished sprints for KPI reporting, with a flag for whether the data is real or fixture-backed.</summary>
/// <param name="Sprints">Finished sprints, any order.</param>
/// <param name="Illustrative"><see langword="true"/> when read from the fixture; the portal then labels the data "illustrative" (SPEC 9.5).</param>
public sealed record KpiHistory(IReadOnlyList<SprintHistory> Sprints, bool Illustrative);

/// <summary>Reads sprint history (SPEC 9.5).</summary>
public interface IKpiHistorySource
{
    /// <summary>Gets the history.</summary>
    /// <param name="teamId">Team id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The history, possibly empty.</returns>
    Task<KpiHistory> GetAsync(string teamId, CancellationToken ct);
}
