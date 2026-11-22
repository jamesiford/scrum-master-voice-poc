using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Models.GetModelComparison;

/// <summary>One row of the realtime model comparison.</summary>
/// <param name="Name">Model name.</param>
/// <param name="IsCurrent">Whether the Facilitator uses it now.</param>
/// <param name="CostPerStandupUsd">Estimated cost of one standup, from list prices and stated assumptions.</param>
/// <param name="MeasuredP50Ms">Measured median reply latency; only known for the current model.</param>
/// <param name="MeasuredP95Ms">Measured 95th percentile latency; only known for the current model.</param>
/// <param name="Notes">Trade-offs.</param>
public sealed record ModelComparisonRow(string Name, bool IsCurrent, decimal CostPerStandupUsd, int? MeasuredP50Ms, int? MeasuredP95Ms, string Notes);

/// <summary>Compares realtime models on estimated cost and measured latency (SPEC M8).</summary>
/// <param name="TeamId">Team id whose standup length and sessions are used.</param>
public sealed record GetModelComparisonQuery(string TeamId) : IQuery<IReadOnlyList<ModelComparisonRow>>;
