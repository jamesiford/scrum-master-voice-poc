using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Zava.Scrum.Application.Abstractions.Messaging;
using Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;

namespace Zava.Scrum.Api.Bot;

public sealed partial class ScrumAgent
{
    private void RegisterCopilotSkills() =>
        AddRoute((context, _) => Task.FromResult(CopilotMessage.IsSkillTurn(context.Activity)),
            OnCopilotQuestionAsync, rank: 1000, autoSignInHandlers: _ssoHandlers!);

    private async Task OnCopilotQuestionAsync(ITurnContext context, ITurnState state, CancellationToken ct)
    {
        var question = CopilotMessage.Question(context.Activity);
        if (!CopilotQuestions.TryParse(question, out var skill, out var teamId))
        {
            await SendCopilotWelcomeAsync(context, ct);
            return;
        }

        var asker = TeamsActor.From(context.Activity);
        if (!asker.IsSuccess)
        {
            await context.SendActivityAsync(CopilotErrors.SignInRequired.Message, cancellationToken: ct);
            return;
        }

        await using var scope = _scopes.CreateAsyncScope();
        if (!await SignInApproverAsync(scope, context, state, ct))
        {
            await context.SendActivityAsync(CopilotErrors.SignInRequired.Message, cancellationToken: ct);
            return;
        }

        var stream = context.StreamingResponse;
        if (stream.IsStreamingChannel)
        {
            await stream.QueueInformativeUpdateAsync("Checking your team membership and your Azure DevOps access...", ct);
        }

        var answer = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new AnswerCopilotQuestionQuery(asker.Value, skill, teamId), ct);
        var text = answer.IsSuccess ? answer.Value.Text : answer.Error!.Message;
        var activity = MessageFactory.Text(text);
        if (answer.IsSuccess)
        {
            activity.Attachments = [CopilotAnswerCard.Create(answer.Value)];
        }

        // Validate the complete answer before streaming it: partial model tokens must never leak a forbidden id or figure.
        if (stream.IsStreamingChannel)
        {
            stream.EnableGeneratedByAILabel = answer.IsSuccess && answer.Value.FromModel;
            stream.FinalMessage = activity;
            stream.QueueTextChunk(text);
            await stream.EndStreamAsync(ct);
        }
        else
        {
            await context.SendActivityAsync(activity, ct);
        }
    }

    private static Task<ResourceResponse> SendCopilotWelcomeAsync(ITurnContext context, CancellationToken ct) =>
        context.SendActivityAsync("I answer with your own Azure DevOps permissions. Changes still need an approved follow-up.\n\n" +
            string.Join("\n", CopilotQuestions.Starters), cancellationToken: ct);
}
