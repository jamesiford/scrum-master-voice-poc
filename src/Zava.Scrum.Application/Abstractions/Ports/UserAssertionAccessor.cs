namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Plain scoped holder for the request's <see cref="UserAssertion"/>.</summary>
internal sealed class UserAssertionAccessor : IUserAssertionAccessor
{
    /// <inheritdoc />
    public UserAssertion? Current { get; set; }
}
