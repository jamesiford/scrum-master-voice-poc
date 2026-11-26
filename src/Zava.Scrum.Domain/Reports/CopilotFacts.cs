using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Reports;

/// <summary>A bounded answer computed from the asker's readable sprint, never from cached agent-identity work data.</summary>
/// <param name="TeamId">Owning team.</param>
/// <param name="Summary">Code-computed figures and lifecycle status.</param>
/// <param name="Lines">At most five cited item or commitment lines.</param>
/// <param name="ItemIds">Ids actually cited in the answer.</param>
public sealed record CopilotFacts(string TeamId, string Summary, IReadOnlyList<string> Lines, IReadOnlyList<WorkItemId> ItemIds);
