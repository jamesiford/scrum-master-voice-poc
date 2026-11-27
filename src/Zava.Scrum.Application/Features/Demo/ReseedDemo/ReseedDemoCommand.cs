using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.Demo.ReseedDemo;

/// <summary>Stages a film reset or resumes publication after the reset follow-up was approved and applied.</summary>
/// <param name="TeamId">Explicit target team.</param>
/// <param name="Decision">Validated Admin caller, never supplied by the request body.</param>
/// <param name="OperationId">Existing operation to publish; null stages or resumes the current operation.</param>
public sealed record ReseedDemoCommand(string TeamId, DecisionContext Decision, Guid? OperationId = null) : ICommand<Guid>;
