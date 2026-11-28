namespace Zava.Scrum.Domain.Voice;

/// <summary>The required circle-back produced no audio after two retries. Pause rather than silently skip the invitation.</summary>
/// <param name="MemberId">The verified member whose invitation could not be delivered.</param>
public sealed record RequiredSpeechFailed(string MemberId) : FloorAction;
