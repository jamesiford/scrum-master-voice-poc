using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Identity;

namespace Zava.Scrum.Application.Features.Demo.OptInDemoScope;

/// <summary>Atomically opts in only the reviewed manifest's seed and explicitly named test-debris records.</summary>
public sealed record OptInDemoScopeCommand(DemoResetScopeManifest Manifest, DecisionContext Decision) : ICommand;
