namespace Zava.Scrum.Domain.Voice;

/// <summary>A deterministic calendar interpretation for a note, not an authorised work-item change.</summary>
/// <param name="Date">The resolved date, or null when the expression is ambiguous or context is absent.</param>
/// <param name="Status">The explicit resolution or abstention reason.</param>
public sealed record SpokenDateResolution(DateOnly? Date, string Status);
