using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Application.Features.FollowUps.ListEmails;

/// <summary>Lists follow-up emails sent so far (F3, stretch).</summary>
public sealed record ListEmailsQuery : IQuery<IReadOnlyList<EmailMessage>>;
