using Zava.Scrum.Domain.WorkSignals;

namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Team-keyed owner consent metadata only; no credentials or Microsoft 365 content.</summary>
public interface IWorkSignalConsentRepository
{
    /// <summary>Loads the latest connection, including revocation, for one team.</summary>
    /// <param name="teamId">The application team.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current connection, or null if never connected.</returns>
    Task<WorkSignalConsent?> GetAsync(string teamId, CancellationToken ct);

    /// <summary>Saves only after successful delegated consent or authorised revocation.</summary>
    /// <param name="consent">The team-keyed owner metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task SaveAsync(WorkSignalConsent consent, CancellationToken ct);
}
