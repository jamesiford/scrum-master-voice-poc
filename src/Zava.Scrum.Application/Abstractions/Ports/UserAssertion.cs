namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>
/// The signed-in person's own token for the request being handled, already validated by the presentation adapter (the portal bearer
/// token, or the Teams single sign-on token). It is the proof that lets the approver, and only the approver, be the author of an
/// Azure DevOps write (ADR 0020).
/// </summary>
/// <param name="Token">The access token. Never logged and never stored.</param>
/// <param name="ObjectId">The validated <c>oid</c> claim.</param>
/// <param name="TenantId">The validated <c>tid</c> claim.</param>
public sealed record UserAssertion(string Token, Guid ObjectId, Guid TenantId);
