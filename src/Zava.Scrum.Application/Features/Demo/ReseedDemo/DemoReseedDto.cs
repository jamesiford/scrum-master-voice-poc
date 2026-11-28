namespace Zava.Scrum.Application.Features.Demo.ReseedDemo;

/// <summary>The recoverable reset contract: approve/apply FollowUpId, then publish OperationId. Cast files remain immutable.</summary>
/// <param name="OperationId">Stable retry identity.</param>
/// <param name="TeamId">Target demo team.</param>
/// <param name="FollowUpId">Pending reset approval using the existing follow-up API.</param>
/// <param name="State">AwaitingApprovalOrApply, Completed, or Aborted; the clean baseline is published only at Completed.</param>
/// <param name="TeamIds">Exact explicitly demo-marked destructive scope.</param>
/// <param name="Boundary">Exact atomicity boundary.</param>
public sealed record DemoReseedDto(Guid OperationId, string TeamId, Guid FollowUpId, string State, IReadOnlyList<string> TeamIds, string Boundary);
