using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.Demo.ResetDemo;

/// <summary>Clears only the configured tenant's explicitly marked demo scope. Azure DevOps is not touched.</summary>
/// <param name="Decision">Validated Admin caller; omitted legacy in-process calls fail closed.</param>
public sealed record ResetDemoCommand(DecisionContext? Decision = null) : ICommand;
