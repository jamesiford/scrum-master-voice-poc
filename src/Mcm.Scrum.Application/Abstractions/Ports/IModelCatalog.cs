using Mcm.Scrum.Domain.Voice;

namespace Mcm.Scrum.Application.Abstractions.Ports;

/// <summary>Lists the realtime models worth comparing and which one is deployed.</summary>
public interface IModelCatalog
{
    /// <summary>Gets the name of the model the Facilitator uses now.</summary>
    string CurrentModel { get; }

    /// <summary>Lists the candidate models.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The candidates with list prices.</returns>
    Task<IReadOnlyList<ModelPrice>> ListAsync(CancellationToken ct);
}
