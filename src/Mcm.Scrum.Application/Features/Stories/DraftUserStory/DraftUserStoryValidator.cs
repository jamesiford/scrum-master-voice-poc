using FluentValidation;

namespace Mcm.Scrum.Application.Features.Stories.DraftUserStory;

/// <summary>Validates <see cref="DraftUserStoryQuery"/>.</summary>
internal sealed class DraftUserStoryValidator : AbstractValidator<DraftUserStoryQuery>
{
    public DraftUserStoryValidator()
    {
        RuleFor(q => q.TeamId).NotEmpty();
        RuleFor(q => q.Topic).NotEmpty().MaximumLength(500);
    }
}
