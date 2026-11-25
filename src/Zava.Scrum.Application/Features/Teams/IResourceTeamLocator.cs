using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Teams;

/// <summary>
/// Finds the team a stored thing belongs to from its id alone, so a route that carries only a session, follow-up, plan, calendar or
/// correlation id is still authorised against the right team (ADR 0018). An id that matches nothing is reported as not found.
/// </summary>
public interface IResourceTeamLocator
{
    /// <summary>Gets the team of a session.</summary>
    /// <param name="sessionId">The session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The team id, or not found.</returns>
    Task<Result<string>> ForSessionAsync(Guid sessionId, CancellationToken ct);

    /// <summary>Gets the team of a follow-up.</summary>
    /// <param name="followUpId">The follow-up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The team id, or not found.</returns>
    Task<Result<string>> ForFollowUpAsync(Guid followUpId, CancellationToken ct);

    /// <summary>Gets the team of a planning pack.</summary>
    /// <param name="packId">The pack.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The team id, or not found.</returns>
    Task<Result<string>> ForPlanningPackAsync(Guid packId, CancellationToken ct);

    /// <summary>Gets the team of a sprint calendar proposal.</summary>
    /// <param name="calendarId">The proposal.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The team id, or not found.</returns>
    Task<Result<string>> ForCalendarAsync(Guid calendarId, CancellationToken ct);

    /// <summary>Gets the team of an audit chain from its correlation id or <c>zsa-</c> tag.</summary>
    /// <param name="reference">The correlation reference.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The team id, or not found.</returns>
    Task<Result<string>> ForCorrelationAsync(string reference, CancellationToken ct);
}

internal sealed class ResourceTeamLocator(
    ISessionRepository sessions,
    IFollowUpRepository followUps,
    IPlanningPackRepository packs,
    ISprintCalendarRepository calendars,
    IApprovalRecordStore records) : IResourceTeamLocator
{
    public async Task<Result<string>> ForSessionAsync(Guid sessionId, CancellationToken ct) =>
        Found((await sessions.GetAsync(sessionId, ct))?.TeamId);

    public async Task<Result<string>> ForFollowUpAsync(Guid followUpId, CancellationToken ct) =>
        Found((await followUps.GetAsync(followUpId, ct))?.TeamId);

    public async Task<Result<string>> ForPlanningPackAsync(Guid packId, CancellationToken ct) =>
        Found((await packs.GetAsync(packId, ct))?.Data.TeamId);

    public async Task<Result<string>> ForCalendarAsync(Guid calendarId, CancellationToken ct) =>
        Found((await calendars.GetAsync(calendarId, ct))?.Data.TeamId);

    public async Task<Result<string>> ForCorrelationAsync(string reference, CancellationToken ct)
    {
        var prefix = Correlation.ToPrefix(reference);
        if (prefix is null)
        {
            return Result<string>.Failure(Error.Invalid("audit.invalid_reference", "Give the 32-character correlation id, or the zsa- tag (at least 12 hex characters)."));
        }

        var trail = await records.ListByCorrelationAsync(prefix, ct);
        if (trail.Count > 0)
        {
            return Found(trail[0].TeamId);
        }

        var session = (await sessions.ListAsync(ct)).FirstOrDefault(s => s.CorrelationId.StartsWith(prefix, StringComparison.Ordinal));
        return Found(session?.TeamId);
    }

    private static Result<string> Found(string? teamId) =>
        teamId is null ? Result<string>.Failure(Error.NotFound("team.resource_not_found", "That item does not exist.")) : Result<string>.Success(teamId);
}
