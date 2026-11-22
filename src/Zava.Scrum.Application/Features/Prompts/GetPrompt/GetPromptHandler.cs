using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
namespace Zava.Scrum.Application.Features.Prompts.GetPrompt;

/// <summary>Reads a prompt.</summary>
internal sealed class GetPromptHandler(IPromptStore prompts) : IRequestHandler<GetPromptQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetPromptQuery request, CancellationToken ct)
    {
        var text = await prompts.GetAsync(request.Name, ct);
        return text is null ? Result<string>.Failure(PromptErrors.NotFound(request.Name)) : Result<string>.Success(text);
    }
}
