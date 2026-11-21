using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;
using Mcm.Scrum.Domain.FollowUps;

namespace Mcm.Scrum.Application.Features.FollowUps.ApplyApprovedChanges;

/// <summary>
/// The single place Azure DevOps is written. The approval guard is <see cref="FollowUp.GetApprovedChanges"/>: it is the
/// only source of <see cref="ApprovedChange"/>, so the writer cannot be reached for an unapproved follow-up (F7).
/// </summary>
internal sealed class ApplyApprovedChangesHandler(IFollowUpRepository followUps, IWorkItemWriter writer, IPublisher publisher, IClock clock) : IRequestHandler<ApplyApprovedChangesCommand, Result>
{
    public async Task<Result> Handle(ApplyApprovedChangesCommand request, CancellationToken ct)
    {
        var followUp = await followUps.GetAsync(request.FollowUpId, ct);
        if (followUp is null)
        {
            return Result.Failure(FollowUpErrors.NotFound(request.FollowUpId));
        }

        if (followUp.Status == FollowUpStatus.Applied)
        {
            return Result.Success();
        }

        var changes = followUp.GetApprovedChanges();
        if (!changes.IsSuccess)
        {
            return Result.Failure(changes.Error!);
        }

        foreach (var change in changes.Value)
        {
            var written = await writer.ApplyAsync(change, ct);
            if (!written.IsSuccess)
            {
                followUp.MarkApplyFailed(written.Error!.Message);
                await followUps.SaveAsync(followUp, ct);
                return written;
            }
        }

        followUp.MarkApplied(clock.UtcNow);
        await followUps.SaveAsync(followUp, ct);
        await followUp.PublishEventsAsync(publisher, ct);
        return Result.Success();
    }
}
