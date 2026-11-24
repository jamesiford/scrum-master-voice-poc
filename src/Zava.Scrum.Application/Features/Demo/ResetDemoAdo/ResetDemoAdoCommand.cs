using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Demo.ResetDemoAdo;

/// <summary>Deletes the comments the agent wrote to the team's Azure DevOps project. Demo only; nothing else in Azure DevOps is touched.</summary>
/// <param name="TeamId">The team whose project is cleaned.</param>
public sealed record ResetDemoAdoCommand(string TeamId) : ICommand<int>;
