using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Audit.GetAuditChain;

/// <summary>Reads the whole chain for one correlation id: session, turns, proposals, approvals, Azure DevOps writes and cards.</summary>
/// <param name="Reference">The W3C trace id, or its short <c>zsa-</c> form as written in an Azure DevOps comment.</param>
public sealed record GetAuditChainQuery(string Reference) : IQuery<AuditChainDto>;
