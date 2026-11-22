using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Domain.Tests.Voice;

public sealed class ModelCostEstimatorTests
{
    [Fact]
    public void PerStandup_KnownPrices_MatchesHandCalculation()
    {
        var price = new ModelPrice("m", 32m, 64m, string.Empty);

        // 15 min: 9,000 in-tokens * $32/M = 0.288; 15 * 0.25 * 1,200 = 4,500 out-tokens * $64/M = 0.288.
        Assert.Equal(0.576m, ModelCostEstimator.PerStandup(price, 15, 0.25));
    }

    [Fact]
    public void PerStandup_NegativeMinutes_CostsNothing() =>
        Assert.Equal(0m, ModelCostEstimator.PerStandup(new ModelPrice("m", 32m, 64m, string.Empty), -5, 0.25));
}
