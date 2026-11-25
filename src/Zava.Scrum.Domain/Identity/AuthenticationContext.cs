namespace Zava.Scrum.Domain.Identity;

/// <summary>
/// The claims that proved a sign-in, kept with an approval so it can be audited later. It never holds a token.
/// </summary>
/// <param name="Method">How the caller authenticated.</param>
/// <param name="Claims">Selected claims, for example <c>oid</c>, <c>tid</c>, <c>azp</c>, <c>scp</c>, <c>roles</c> and <c>iat</c>.</param>
public sealed record AuthenticationContext(string Method, IReadOnlyDictionary<string, string> Claims)
{
    /// <summary>Gets an empty context for callers with no claims to record.</summary>
    /// <param name="method">How the caller authenticated.</param>
    /// <returns>A context with no claims.</returns>
    public static AuthenticationContext Of(string method) => new(method, new Dictionary<string, string>());
}
