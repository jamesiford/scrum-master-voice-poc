namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Request-local Work IQ bearer access. Never persist or log this value.</summary>
/// <param name="Token">The delegated bearer token.</param>
public sealed record DelegatedWorkSignalAccess(string Token)
{
    /// <summary>Prevents accidental record formatting from disclosing the credential.</summary>
    /// <returns>A redacted description.</returns>
    public override string ToString() => nameof(DelegatedWorkSignalAccess);
}
