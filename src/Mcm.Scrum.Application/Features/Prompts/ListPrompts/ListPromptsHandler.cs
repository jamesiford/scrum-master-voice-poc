using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
namespace Mcm.Scrum.Application.Features.Prompts.ListPrompts;

/// <summary>Reads prompt names.</summary>
internal sealed class ListPromptsHandler(IPromptStore prompts) : IRequestHandler<ListPromptsQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(ListPromptsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<string>>.Success(await prompts.ListAsync(ct));
}
