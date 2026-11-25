using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.Planning.RejectPlan;

/// <summary>Rejects a sprint plan. Nothing is written to Azure DevOps.</summary>
/// <param name="PackId">The pack.</param>
/// <param name="Decision">Who is rejecting and how that was proven.</param>
public sealed record RejectPlanCommand(Guid PackId, DecisionContext Decision) : ICommand;
