using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Demo.ResetDemo;

/// <summary>Clears sessions, follow-ups and cards so the demo can be re-run. Azure DevOps is not touched.</summary>
public sealed record ResetDemoCommand : ICommand;
