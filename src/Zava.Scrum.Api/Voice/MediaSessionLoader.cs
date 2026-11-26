using Microsoft.Extensions.Options;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Application.Features.Standups.GetSession;
using Zava.Scrum.Application.Features.Teams.GetRoster;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Infrastructure.Teams;

namespace Zava.Scrum.Api.Voice;

/// <summary>Loads an authorised media session through existing read use cases, leaving Application unchanged.</summary>
public sealed class MediaSessionLoader(ISender sender, IPromptStore prompts, ISessionRepository sessions, IOptions<BridgeOptions> options, IOptions<BoardOptions> board)
{
    /// <summary>Validates the explicit team against the session before disclosing any facts.</summary>
    /// <param name="sessionId">The token's session.</param>
    /// <param name="teamId">Explicit team id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Prepared read-only facts or a typed failure.</returns>
    public async Task<Result<MediaSessionContext>> LoadAsync(Guid sessionId, string teamId, CancellationToken ct)
    {
        var session = await sender.Send(new GetSessionQuery(sessionId), ct);
        if (!session.IsSuccess)
        {
            return Result<MediaSessionContext>.Failure(session.Error!);
        }

        if (options.Value.Mode != "GraphMedia" || session.Value.TeamId != teamId
            || session.Value.Status != StandupStatus.Live || session.Value.Mode != "acs"
            || session.Value.PreBrief?.TeamId != teamId)
        {
            return Result<MediaSessionContext>.Failure(Error.Forbidden("media.session_refused", "GraphMedia requires a live meeting session belonging to the explicit team."));
        }

        var roster = await sender.Send(new GetRosterQuery(teamId), ct);
        if (!roster.IsSuccess)
        {
            return Result<MediaSessionContext>.Failure(roster.Error!);
        }

        var prompt = await prompts.ResolveAsync(teamId, "media-facilitator", ct);
        var stored = await sessions.GetAsync(sessionId, ct);
        if (string.IsNullOrWhiteSpace(stored?.JoinUrl))
        {
            return Result<MediaSessionContext>.Failure(Error.Invalid("media.link_missing", "The live session has no stored calendar link."));
        }

        return prompt is null
            ? Result<MediaSessionContext>.Failure(Error.NotFound("media.prompt_missing", "The media-facilitator prompt is missing."))
            : Result<MediaSessionContext>.Success(new MediaSessionContext(session.Value, roster.Value, prompt.Text, stored.JoinUrl, board.Value.Mode));
    }
}
