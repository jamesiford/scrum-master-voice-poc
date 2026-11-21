namespace Mcm.Scrum.Domain.Common;

/// <summary>Lets pipeline behaviours build a failed result of the closed response type without reflection.</summary>
/// <typeparam name="TSelf">The result type itself.</typeparam>
public interface IResultFactory<out TSelf>
{
    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The failure.</param>
    /// <returns>A failed result.</returns>
    static abstract TSelf Failure(Error error);
}
