namespace Mcm.Scrum.Domain.Ceremonies;

/// <summary>One computed number or fact shown on a ceremony card.</summary>
/// <param name="Label">What it is.</param>
/// <param name="Value">The value, already formatted.</param>
public sealed record PackFact(string Label, string Value);
