using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;
using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Application.Features.Identity;

/// <summary>
/// Turns the request's token into Azure DevOps access for the recorded approver. The write handler and the approve handlers both use it,
/// so a follow-up is never left approved but unwritable (DRY). The approver is checked first by <see cref="ApproverAssertion"/>.
/// </summary>
internal sealed class DelegatedAccessProvider(IUserAssertionAccessor assertions, IApproverTokenSource tokens)
{
    /// <summary>Gets Azure DevOps access as the approver.</summary>
    /// <param name="approver">The recorded approver.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The access, or the reason the write must not go ahead.</returns>
    public async Task<Result<DelegatedAdoAccess>> AcquireAsync(ActorIdentity approver, CancellationToken ct)
    {
        var current = assertions.Current;
        var check = ApproverAssertion.Check(approver, current?.ObjectId, current?.TenantId);
        return check.IsSuccess
            ? await tokens.ExchangeAsync(current!, ct)
            : Result<DelegatedAdoAccess>.Failure(check.Error!);
    }
}
