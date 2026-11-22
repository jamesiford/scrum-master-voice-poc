using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Voice;
using Microsoft.Extensions.Options;

namespace Zava.Scrum.Infrastructure.Voice;

/// <summary>Candidate realtime models for the comparison page, bound from <c>ModelComparison:Models</c>.</summary>
public sealed class ModelComparisonOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "ModelComparison";

    /// <summary>Gets or sets the candidates.</summary>
    // VERIFY: list prices at https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/ (audio tokens); these are placeholders for the demo.
    public List<ModelPriceOption> Models { get; set; } =
    [
        new() { Name = "gpt-realtime-2.1", AudioInPerMillionTokensUsd = 32m, AudioOutPerMillionTokensUsd = 64m, Notes = "Best quality; current default." },
        new() { Name = "gpt-realtime-mini", AudioInPerMillionTokensUsd = 10m, AudioOutPerMillionTokensUsd = 20m, Notes = "Cheaper and faster; weaker on long, multi-item updates." },
    ];
}

/// <summary>One configured candidate.</summary>
public sealed class ModelPriceOption
{
    /// <summary>Gets or sets the model name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the audio input price per million tokens.</summary>
    public decimal AudioInPerMillionTokensUsd { get; set; }

    /// <summary>Gets or sets the audio output price per million tokens.</summary>
    public decimal AudioOutPerMillionTokensUsd { get; set; }

    /// <summary>Gets or sets the trade-off notes.</summary>
    public string Notes { get; set; } = string.Empty;
}

/// <summary>Serves the configured candidates, marking the deployed model as current.</summary>
public sealed class ConfigModelCatalog(IOptions<ModelComparisonOptions> options, IOptions<VoiceLiveOptions> voice) : IModelCatalog
{
    /// <inheritdoc />
    public string CurrentModel => voice.Value.Model;

    /// <inheritdoc />
    public Task<IReadOnlyList<ModelPrice>> ListAsync(CancellationToken ct)
    {
        IReadOnlyList<ModelPrice> prices = options.Value.Models
            .Select(m => new ModelPrice(m.Name, m.AudioInPerMillionTokensUsd, m.AudioOutPerMillionTokensUsd, m.Notes))
            .ToList();
        return Task.FromResult(prices);
    }
}
