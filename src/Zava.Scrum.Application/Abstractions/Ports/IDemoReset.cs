namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Clears demo state so the storyboard can be re-run (SPEC 2). Never touches Azure DevOps.</summary>
public interface IDemoReset
{
    /// <summary>Deletes sessions, follow-ups and sent cards.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task.</returns>
    Task ResetAsync(CancellationToken ct);
}
