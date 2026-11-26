namespace Zava.Scrum.Application.Abstractions.Messaging;

/// <summary>Supplies resolved team metadata to the pipeline when a request selects its team from memberships.</summary>
/// <typeparam name="TResponse">The typed response containing the authorised team.</typeparam>
public interface IResolvedTeamRequest<TResponse>
{
    /// <summary>Gets the authorised team after execution without adding business logic to the logging behaviour.</summary>
    /// <param name="response">The request's result.</param>
    /// <returns>The resolved team, an explicit requested team on failure, or null when no team was selected.</returns>
    string? GetResolvedTeamId(TResponse response);
}
