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
    // The models deployed by default in infra/main.bicep (realtimeModels). gpt-realtime-2.1-mini was measured and rejected (ADR 0019); add it here when it is deployed. Tiers are from Microsoft Learn (Voice Live overview, 25 November 2026).
    // VERIFY: https://azure.microsoft.com/pricing/details/speech/ lists the USD rates per million tokens for each tier, but the page loads them by
    // script and they could not be read here. Prices are therefore left null and shown as unverified; set them under ModelComparison:Models.
    public List<ModelPriceOption> Models { get; set; } =
    [
        new() { Name = "gpt-realtime-2.1", PricingTier = "Pro", Notes = "Chosen from measurements against gpt-realtime-2.1-mini (ADR 0019)." },
    ];
}

/// <summary>One configured candidate.</summary>
public sealed class ModelPriceOption
{
    /// <summary>Gets or sets the model name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the Voice Live pricing tier (Pro, Standard or Lite).</summary>
    public string? PricingTier { get; set; }

    /// <summary>Gets or sets the audio input price per million tokens; <see langword="null"/> means unverified.</summary>
    public decimal? AudioInPerMillionTokensUsd { get; set; }

    /// <summary>Gets or sets the audio output price per million tokens; <see langword="null"/> means unverified.</summary>
    public decimal? AudioOutPerMillionTokensUsd { get; set; }

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
            .Select(m => new ModelPrice(m.Name, m.AudioInPerMillionTokensUsd, m.AudioOutPerMillionTokensUsd, m.Notes, m.PricingTier))
            .ToList();
        return Task.FromResult(prices);
    }
}
