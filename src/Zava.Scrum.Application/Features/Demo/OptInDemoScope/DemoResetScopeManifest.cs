namespace Zava.Scrum.Application.Features.Demo.OptInDemoScope;

/// <summary>The authorised tenant, sandbox and exact seed/test-debris ids to mark; unknown registry entries remain untouched.</summary>
public sealed record DemoResetScopeManifest(Guid TenantId, string Organisation, IReadOnlyList<string> CanonicalSeedTeamIds,
    IReadOnlyList<string> TestDebrisTeamIds, IReadOnlyList<DemoResetScopeEntry> Teams);
