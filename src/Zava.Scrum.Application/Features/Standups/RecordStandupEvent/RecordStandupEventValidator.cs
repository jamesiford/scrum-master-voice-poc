using FluentValidation;

namespace Mcm.Scrum.Application.Features.Standups.RecordStandupEvent;

/// <summary>Validates <see cref="RecordStandupEventCommand"/>.</summary>
internal sealed class RecordStandupEventValidator : AbstractValidator<RecordStandupEventCommand>
{
    public RecordStandupEventValidator()
    {
        RuleFor(c => c.SessionId).NotEmpty();
        RuleFor(c => c.Text).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.Type).IsInEnum();
    }
}
