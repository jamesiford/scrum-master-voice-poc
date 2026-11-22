using FluentValidation;

namespace Zava.Scrum.Application.Features.Prompts.UpdatePrompt;

/// <summary>Validates <see cref="UpdatePromptCommand"/>.</summary>
internal sealed class UpdatePromptValidator : AbstractValidator<UpdatePromptCommand>
{
    public UpdatePromptValidator()
    {
        RuleFor(c => c.Name).NotEmpty().Matches("^[A-Za-z0-9-]+$");
        RuleFor(c => c.Text).NotEmpty().MaximumLength(20000);
    }
}
