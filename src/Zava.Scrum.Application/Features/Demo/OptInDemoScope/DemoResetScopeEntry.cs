using Zava.Scrum.Domain.Teams;

namespace Zava.Scrum.Application.Features.Demo.OptInDemoScope;

/// <summary>An explicitly named registry record and its reviewed exact ADO fingerprint, never a display-name match.</summary>
public sealed record DemoResetScopeEntry(string TeamId, AdoSettings ExpectedAdo);
