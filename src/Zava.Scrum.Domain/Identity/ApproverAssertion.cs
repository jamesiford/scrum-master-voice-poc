using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Domain.Identity;

/// <summary>
/// The rule behind delegated writes (ADR 0020): Azure DevOps is written as the person who approved, so the token presented when the
/// write happens must belong to exactly that person. A retry by someone else, or a request with no token, never reaches the writer.
/// </summary>
public static class ApproverAssertion
{
    /// <summary>Checks that the presented token belongs to the recorded approver.</summary>
    /// <param name="approver">The approver recorded on the approval record.</param>
    /// <param name="tokenObjectId">The <c>oid</c> of the token presented now, or <see langword="null"/> when there is none.</param>
    /// <param name="tokenTenantId">The <c>tid</c> of the token presented now, or <see langword="null"/> when there is none.</param>
    /// <returns>Success, or the reason the write is refused.</returns>
    public static Result Check(ActorIdentity approver, Guid? tokenObjectId, Guid? tokenTenantId)
    {
        ArgumentNullException.ThrowIfNull(approver);
        if (tokenObjectId is null || tokenTenantId is null)
        {
            return Result.Failure(IdentityErrors.DelegatedTokenRequired);
        }

        return tokenObjectId == approver.ObjectId && tokenTenantId == approver.TenantId
            ? Result.Success()
            : Result.Failure(IdentityErrors.DelegatedTokenNotApprover);
    }
}
