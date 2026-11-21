using Mcm.Scrum.Domain.Work;

namespace Mcm.Scrum.Domain.Flags;

/// <summary>One flag rule. Add a rule by adding a class; do not edit a switch (SOLID: O).</summary>
public interface IFlagRule
{
    /// <summary>Gets the flag this rule raises.</summary>
    Flag Flag { get; }

    /// <summary>Decides whether the flag applies to an item.</summary>
    /// <param name="item">The item.</param>
    /// <param name="context">Shared inputs.</param>
    /// <returns><see langword="true"/> if the flag applies.</returns>
    bool Applies(WorkItem item, FlagContext context);
}
