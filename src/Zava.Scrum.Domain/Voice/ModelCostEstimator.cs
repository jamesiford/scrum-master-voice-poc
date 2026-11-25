namespace Zava.Scrum.Domain.Voice;

/// <summary>A realtime model candidate with list prices.</summary>
/// <param name="Name">Deployment or model name.</param>
/// <param name="AudioInPerMillionTokensUsd">Audio input price per million tokens, or <see langword="null"/> when the price has not been verified.</param>
/// <param name="AudioOutPerMillionTokensUsd">Audio output price per million tokens, or <see langword="null"/> when the price has not been verified.</param>
/// <param name="Notes">Free-text trade-offs.</param>
/// <param name="PricingTier">The Voice Live pricing tier Microsoft Learn lists for the model (Pro, Standard or Lite), or <see langword="null"/>.</param>
public sealed record ModelPrice(string Name, decimal? AudioInPerMillionTokensUsd, decimal? AudioOutPerMillionTokensUsd, string Notes, string? PricingTier = null);

/// <summary>Estimates what one standup costs on a realtime model. Pure arithmetic over stated assumptions.</summary>
public static class ModelCostEstimator
{
    // Microsoft Learn (Voice Live overview, token usage): Azure OpenAI models use about 10 audio tokens a second in and 20 out, so 600 and 1,200 a minute.
    private const int InTokensPerMinute = 600;
    private const int OutTokensPerMinute = 1200;

    /// <summary>Estimates the cost of one standup.</summary>
    /// <param name="price">The model price.</param>
    /// <param name="standupMinutes">Standup length.</param>
    /// <param name="agentTalkShare">Share of the standup the agent speaks (0 to 1).</param>
    /// <returns>Estimated USD for one standup, or <see langword="null"/> when either price is unverified (nothing is guessed).</returns>
    public static decimal? PerStandup(ModelPrice price, int standupMinutes, double agentTalkShare)
    {
        ArgumentNullException.ThrowIfNull(price);
        if (price.AudioInPerMillionTokensUsd is not { } priceIn || price.AudioOutPerMillionTokensUsd is not { } priceOut)
        {
            return null;
        }

        var minutes = Math.Max(0, standupMinutes);
        var share = (decimal)Math.Clamp(agentTalkShare, 0, 1);
        var input = minutes * InTokensPerMinute * priceIn / 1_000_000m;
        var output = minutes * share * OutTokensPerMinute * priceOut / 1_000_000m;
        return Math.Round(input + output, 4);
    }
}
