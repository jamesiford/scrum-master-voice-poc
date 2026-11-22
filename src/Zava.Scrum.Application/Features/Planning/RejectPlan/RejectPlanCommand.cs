using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Planning.RejectPlan;

/// <summary>Rejects a sprint plan. Nothing is written to Azure DevOps.</summary>
/// <param name="PackId">The pack.</param>
/// <param name="Rejecter">Display name of the person rejecting.</param>
public sealed record RejectPlanCommand(Guid PackId, string Rejecter) : ICommand;
