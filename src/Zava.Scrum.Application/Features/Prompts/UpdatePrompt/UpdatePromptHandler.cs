using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
namespace Mcm.Scrum.Application.Features.Prompts.UpdatePrompt;

/// <summary>Saves a prompt.</summary>
internal sealed class UpdatePromptHandler(IPromptStore prompts) : IRequestHandler<UpdatePromptCommand, Result>
{
    public async Task<Result> Handle(UpdatePromptCommand request, CancellationToken ct) =>
        await prompts.SaveAsync(request.Name, request.Text, ct) ? Result.Success() : Result.Failure(PromptErrors.NotFound(request.Name));
}
