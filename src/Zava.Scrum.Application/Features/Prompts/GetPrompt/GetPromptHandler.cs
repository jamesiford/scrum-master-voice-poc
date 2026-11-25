using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
namespace Zava.Scrum.Application.Features.Prompts.GetPrompt;

/// <summary>Reads a prompt.</summary>
internal sealed class GetPromptHandler(IPromptStore prompts) : IRequestHandler<GetPromptQuery, Result<PromptResolution>>
{
    public async Task<Result<PromptResolution>> Handle(GetPromptQuery request, CancellationToken ct)
    {
        var text = await prompts.ResolveAsync(request.TeamId, request.Name, ct);
        return text is null ? Result<PromptResolution>.Failure(PromptErrors.NotFound(request.Name)) : Result<PromptResolution>.Success(text);
    }
}
