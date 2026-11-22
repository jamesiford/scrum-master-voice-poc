using FluentValidation;

namespace Mcm.Scrum.Application.Features.Reports.AnswerTeamQuestion;

/// <summary>Validates <see cref="AnswerTeamQuestionQuery"/>.</summary>
internal sealed class AnswerTeamQuestionValidator : AbstractValidator<AnswerTeamQuestionQuery>
{
    public AnswerTeamQuestionValidator()
    {
        RuleFor(q => q.TeamId).NotEmpty();
        RuleFor(q => q.Question).NotEmpty().MaximumLength(1000);
    }
}
