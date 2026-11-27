using FluentValidation;

namespace Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;

/// <summary>Rejects absent identities and undefined skills before accessing any port.</summary>
internal sealed class AnswerCopilotQuestionValidator : AbstractValidator<AnswerCopilotQuestionQuery>
{
    /// <summary>Initialises the bounded skill validation rules.</summary>
    public AnswerCopilotQuestionValidator()
    {
        RuleFor(q => q.Asker).NotNull();
        RuleFor(q => q.Skill).IsInEnum();
        RuleFor(q => q.ChosenTeamId).MaximumLength(128);
    }
}
