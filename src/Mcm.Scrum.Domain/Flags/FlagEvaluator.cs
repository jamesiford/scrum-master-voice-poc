using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Flags;

/// <summary>Runs every registered <see cref="IFlagRule"/> against an item. Numbers and flags come from code, never a model (rule 3).</summary>
/// <param name="rules">The rules to run.</param>
public sealed class FlagEvaluator(IEnumerable<IFlagRule> rules)
{
    private readonly IFlagRule[] _rules = rules.ToArray();

    /// <summary>Creates an evaluator with the six built-in item rules.</summary>
    /// <returns>The evaluator.</returns>
    public static FlagEvaluator CreateDefault() =>
        new([new StaleRule(), new BlockedRule(), new OverdueRule(), new UnassignedRule(), new NoRecentUpdateRule()]);

    /// <summary>Evaluates all rules for an item.</summary>
    /// <param name="item">The item.</param>
    /// <param name="context">Shared inputs.</param>
    /// <returns>Flags that apply, in rule order.</returns>
    public IReadOnlyList<Flag> Evaluate(WorkItem item, FlagContext context) =>
        _rules.Where(r => r.Applies(item, context)).Select(r => r.Flag).ToList();
}
