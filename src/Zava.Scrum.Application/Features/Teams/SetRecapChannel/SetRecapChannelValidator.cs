using FluentValidation;

namespace Zava.Scrum.Application.Features.Teams.SetRecapChannel;

/// <summary>Validates <see cref="SetRecapChannelCommand"/>.</summary>
internal sealed class SetRecapChannelValidator : AbstractValidator<SetRecapChannelCommand>
{
    public SetRecapChannelValidator()
    {
        RuleFor(c => c.TeamId).NotEmpty();
        RuleFor(c => c.ConversationId).NotEmpty();
    }
}
