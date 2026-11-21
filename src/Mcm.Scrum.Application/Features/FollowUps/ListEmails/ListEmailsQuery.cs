using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;

namespace Mcm.Scrum.Application.Features.FollowUps.ListEmails;

/// <summary>Lists follow-up emails sent so far (F3, stretch).</summary>
public sealed record ListEmailsQuery : IQuery<IReadOnlyList<EmailMessage>>;
