namespace Mcm.Scrum.Domain.Voice;

/// <summary>A realtime model candidate with list prices.</summary>
/// <param name="Name">Deployment or model name.</param>
/// <param name="AudioInPerMillionTokensUsd">Audio input price per million tokens.</param>
/// <param name="AudioOutPerMillionTokensUsd">Audio output price per million tokens.</param>
/// <param name="Notes">Free-text trade-offs.</param>
public sealed record ModelPrice(string Name, decimal AudioInPerMillionTokensUsd, decimal AudioOutPerMillionTokensUsd, string Notes);

/// <summary>Estimates what one standup costs on a realtime model. Pure arithmetic over stated assumptions.</summary>
public static class ModelCostEstimator
{
    // VERIFY: audio tokens per minute (about 600 in, 1,200 out is a rough planning figure, not a billing rate).
    private const int InTokensPerMinute = 600;
    private const int OutTokensPerMinute = 1200;

    /// <summary>Estimates the cost of one standup.</summary>
    /// <param name="price">The model price.</param>
    /// <param name="standupMinutes">Standup length.</param>
    /// <param name="agentTalkShare">Share of the standup the agent speaks (0 to 1).</param>
    /// <returns>Estimated USD for one standup.</returns>
    public static decimal PerStandup(ModelPrice price, int standupMinutes, double agentTalkShare)
    {
        ArgumentNullException.ThrowIfNull(price);
        var minutes = Math.Max(0, standupMinutes);
        var share = (decimal)Math.Clamp(agentTalkShare, 0, 1);
        var input = minutes * InTokensPerMinute * price.AudioInPerMillionTokensUsd / 1_000_000m;
        var output = minutes * share * OutTokensPerMinute * price.AudioOutPerMillionTokensUsd / 1_000_000m;
        return Math.Round(input + output, 4);
    }
}
