using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;
using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.FollowUps.ListEmails;

/// <summary>Reads the email sender's outbox.</summary>
internal sealed class ListEmailsHandler(IEmailSender emails) : IRequestHandler<ListEmailsQuery, Result<IReadOnlyList<EmailMessage>>>
{
    public async Task<Result<IReadOnlyList<EmailMessage>>> Handle(ListEmailsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<EmailMessage>>.Success(await emails.ListAsync(ct));
}
