using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.Standups.GetFacilitatorInstructions;

/// <summary>Loads and renders the facilitator prompt.</summary>
internal sealed class GetFacilitatorInstructionsHandler(ISessionRepository sessions, ITeamRepository teams, IPromptStore prompts)
    : IRequestHandler<GetFacilitatorInstructionsQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetFacilitatorInstructionsQuery request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(request.SessionId, ct);
        if (session is null)
        {
            return Result<string>.Failure(StandupErrors.SessionNotFound(request.SessionId));
        }

        var template = await prompts.GetAsync("facilitator", ct);
        if (template is null)
        {
            return Result<string>.Failure(Error.NotFound("prompt.not_found", "Prompt 'facilitator' is missing from agents/instructions."));
        }

        var team = await teams.GetAsync(session.TeamId, ct);
        return Result<string>.Success(PromptTemplate.Render(template, new Dictionary<string, string> { ["teamName"] = team?.Name ?? "the team" }));
    }
}
