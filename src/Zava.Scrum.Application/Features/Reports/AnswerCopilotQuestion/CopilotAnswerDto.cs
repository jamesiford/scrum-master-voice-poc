namespace Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;

/// <summary>A short, code-cited answer and links to the same permission-protected Azure DevOps items.</summary>
/// <param name="TeamId">Resolved owning team.</param>
/// <param name="Text">Validated phrasing followed by deterministic facts.</param>
/// <param name="ItemLinks">Code-generated links, never model-generated URLs.</param>
/// <param name="FromModel">Whether the introductory phrasing came from the model.</param>
public sealed record CopilotAnswerDto(string TeamId, string Text, IReadOnlyList<string> ItemLinks, bool FromModel);
