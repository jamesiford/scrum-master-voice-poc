using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Standups.GetFacilitatorInstructions;

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

        var name = session.Kind == Domain.Standups.CeremonyKind.Retrospective ? "facilitator-retro" : "facilitator";
        var template = await prompts.GetAsync(name, ct);
        if (template is null)
        {
            return Result<string>.Failure(Error.NotFound("prompt.not_found", $"Prompt '{name}' is missing from agents/instructions."));
        }

        var team = await teams.GetAsync(session.TeamId, ct);
        return Result<string>.Success(PromptTemplate.Render(template, new Dictionary<string, string> { ["teamName"] = team?.Name ?? "the team" }));
    }
}
