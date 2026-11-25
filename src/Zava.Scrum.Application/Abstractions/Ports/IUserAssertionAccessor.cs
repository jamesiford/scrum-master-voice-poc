namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Holds the <see cref="UserAssertion"/> for the current request scope. Registered scoped, so it never carries over between requests.</summary>
public interface IUserAssertionAccessor
{
    /// <summary>Gets or sets the assertion presented with the current request, or <see langword="null"/> when the caller has none.</summary>
    UserAssertion? Current { get; set; }
}
