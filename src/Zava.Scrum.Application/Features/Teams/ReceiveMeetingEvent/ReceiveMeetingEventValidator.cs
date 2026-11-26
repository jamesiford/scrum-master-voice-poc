using FluentValidation;

namespace Zava.Scrum.Application.Features.Teams.ReceiveMeetingEvent;

/// <summary>Rejects incomplete system events before any session is read or changed.</summary>
internal sealed class ReceiveMeetingEventValidator : AbstractValidator<ReceiveMeetingEventCommand>
{
    /// <summary>Initialises the required identity and lifecycle checks.</summary>
    public ReceiveMeetingEventValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.MeetingId).NotEmpty();
        RuleFor(x => x.JoinUrl).NotEmpty().Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
        RuleFor(x => x.Kind).IsInEnum();
    }
}
