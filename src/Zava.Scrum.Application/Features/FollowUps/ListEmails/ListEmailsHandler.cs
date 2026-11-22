using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Common;

namespace Mcm.Scrum.Application.Features.FollowUps.ListEmails;

/// <summary>Reads the email sender's outbox.</summary>
internal sealed class ListEmailsHandler(IEmailSender emails) : IRequestHandler<ListEmailsQuery, Result<IReadOnlyList<EmailMessage>>>
{
    public async Task<Result<IReadOnlyList<EmailMessage>>> Handle(ListEmailsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<EmailMessage>>.Success(await emails.ListAsync(ct));
}
