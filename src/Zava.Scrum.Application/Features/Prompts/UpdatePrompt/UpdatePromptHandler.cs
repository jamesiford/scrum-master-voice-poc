using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
namespace Zava.Scrum.Application.Features.Prompts.UpdatePrompt;

/// <summary>Saves a prompt.</summary>
internal sealed class UpdatePromptHandler(IPromptStore prompts) : IRequestHandler<UpdatePromptCommand, Result>
{
    public async Task<Result> Handle(UpdatePromptCommand request, CancellationToken ct) =>
        await prompts.SaveAsync(request.Name, request.Text, ct) ? Result.Success() : Result.Failure(PromptErrors.NotFound(request.Name));
}
