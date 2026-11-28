namespace Zava.Scrum.Domain.FollowUps;

/// <summary>A deterministic seed baseline for one existing demo item, with an idempotency marker and exact scope.</summary>
/// <param name="OperationId">Recoverable reset operation.</param>
/// <param name="Project">Expected project.</param>
/// <param name="AreaPath">Expected exact demo area.</param>
/// <param name="KeyTag">Existing demo-key tag proving seed identity.</param>
/// <param name="Fields">Approved field baseline, using ADO field reference names; null clears an existing field.</param>
public sealed record DemoWorkItemReset(Guid OperationId, string Project, string AreaPath, string KeyTag, IReadOnlyDictionary<string, string?> Fields);
