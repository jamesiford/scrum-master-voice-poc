using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Kpis;

/// <summary>Computes <see cref="Burndown"/> from a sprint snapshot.</summary>
public static class BurndownCalculator
{
    private const double RiskFactor = 1.3;

    /// <summary>Calculates burndown at a point in time.</summary>
    /// <param name="sprint">The sprint.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The burndown.</returns>
    public static Burndown Calculate(SprintSnapshot sprint, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(sprint);
        var withPoints = sprint.Items.Where(i => i.StoryPoints is not null).ToList();
        var committed = withPoints.Sum(i => i.StoryPoints!.Value);
        var completed = withPoints.Where(i => i.IsDone).Sum(i => i.StoryPoints!.Value);
        var remaining = committed - completed;

        var daysLeft = Math.Max(0, (int)Math.Ceiling((sprint.EndsAt - now).TotalDays));
        var daysElapsed = (now - sprint.StartsAt).TotalDays;
        return new Burndown(committed, completed, remaining, daysLeft, IsAtRisk(remaining, completed, daysLeft, daysElapsed));
    }

    private static bool IsAtRisk(double remaining, double completed, int daysLeft, double daysElapsed)
    {
        if (remaining <= 0 || daysElapsed <= 0)
        {
            return false;
        }

        if (daysLeft == 0)
        {
            return true;
        }

        var pace = completed / daysElapsed;
        return remaining / daysLeft > RiskFactor * pace;
    }
}
