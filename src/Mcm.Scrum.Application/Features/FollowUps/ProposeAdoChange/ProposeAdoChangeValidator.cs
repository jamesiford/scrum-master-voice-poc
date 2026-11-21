using FluentValidation;

namespace Mcm.Scrum.Application.Features.FollowUps.ProposeAdoChange;

/// <summary>Validates <see cref="ProposeAdoChangeCommand"/>.</summary>
internal sealed class ProposeAdoChangeValidator : AbstractValidator<ProposeAdoChangeCommand>
{
    public ProposeAdoChangeValidator()
    {
        RuleFor(c => c.SessionId).NotEmpty();
        RuleFor(c => c.MemberId).NotEmpty();
        RuleFor(c => c.WorkItemId).GreaterThan(0);
        RuleFor(c => c.Value).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.Value).Must(v => v.Length > 1 && (v[0] == '+' || v[0] == '-')).When(c => c.Op == Domain.FollowUps.ChangeOp.Tag)
            .WithMessage("A tag change must start with + or -.");
    }
}
