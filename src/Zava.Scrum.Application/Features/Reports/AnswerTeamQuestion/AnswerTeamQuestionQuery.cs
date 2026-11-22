using Zava.Scrum.Application.Abstractions.Messaging;

namespace Zava.Scrum.Application.Features.Reports.AnswerTeamQuestion;

/// <summary>Answers a question from Teams or Copilot chat using KPIs, the current pre-brief and the last standup.</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="Question">What the person asked.</param>
public sealed record AnswerTeamQuestionQuery(string TeamId, string Question) : IQuery<string>;
