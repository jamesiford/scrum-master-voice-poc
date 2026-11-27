namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Retention contract for short team evidence references; deliberately no implementation before the spike.</summary>
public interface IWorkSignalEvidenceStore
{
    /// <summary>Saves only policy-checked short references from an explicit state-changing command.</summary>
    /// <param name="record">The team, owner, correlation and deletion deadline.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task AppendAsync(WorkSignalEvidenceRecord record, CancellationToken ct);

    /// <summary>Deletes references for this team's revoked connection, without touching other teams.</summary>
    /// <param name="teamId">The application team.</param>
    /// <param name="ownerObjectId">The revoked owner.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task DeleteForOwnerAsync(string teamId, Guid ownerObjectId, CancellationToken ct);

    /// <summary>Deletes expired references for one team.</summary>
    /// <param name="teamId">The application team.</param>
    /// <param name="now">The clock timestamp.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task DeleteExpiredAsync(string teamId, DateTimeOffset now, CancellationToken ct);
}
