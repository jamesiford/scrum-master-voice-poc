namespace Zava.Scrum.Domain.Voice;

/// <summary>Identity attribution and measured amplitude of one unmixed audio source; contains no audio or approval authority.</summary>
/// <param name="SourceId">Transport participant identifier.</param>
/// <param name="MemberId">Verified team roster member, or null for a guest.</param>
/// <param name="Amplitude">Measured source amplitude.</param>
public sealed record FloorAudioSource(string SourceId, string? MemberId, int Amplitude);
