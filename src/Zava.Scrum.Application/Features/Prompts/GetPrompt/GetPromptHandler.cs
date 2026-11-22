using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
namespace Mcm.Scrum.Application.Features.Prompts.GetPrompt;

/// <summary>Reads a prompt.</summary>
internal sealed class GetPromptHandler(IPromptStore prompts) : IRequestHandler<GetPromptQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetPromptQuery request, CancellationToken ct)
    {
        var text = await prompts.GetAsync(request.Name, ct);
        return text is null ? Result<string>.Failure(PromptErrors.NotFound(request.Name)) : Result<string>.Success(text);
    }
}
