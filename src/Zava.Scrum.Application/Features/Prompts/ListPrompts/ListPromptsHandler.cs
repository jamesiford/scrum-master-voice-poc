using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
namespace Zava.Scrum.Application.Features.Prompts.ListPrompts;

/// <summary>Reads prompt names.</summary>
internal sealed class ListPromptsHandler(IPromptStore prompts) : IRequestHandler<ListPromptsQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(ListPromptsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<string>>.Success(await prompts.ListAsync(ct));
}
