using Mcm.Scrum.Application.Abstractions.Messaging;

namespace Mcm.Scrum.Application.Features.Stories.DraftUserStory;

/// <summary>A drafted user story. Draft only: it is never written to Azure DevOps without an approved follow-up (F7).</summary>
/// <param name="Title">Story title.</param>
/// <param name="Story">The "As a, I want, so that" sentence.</param>
/// <param name="AcceptanceCriteria">Acceptance criteria.</param>
/// <param name="FromModel">Whether the model wrote it (otherwise a template).</param>
public sealed record StoryDraft(string Title, string Story, IReadOnlyList<string> AcceptanceCriteria, bool FromModel);

/// <summary>Drafts a user story and acceptance criteria from a parked decision or topic (F9, SPEC M8).</summary>
/// <param name="TeamId">Team id.</param>
/// <param name="Topic">The topic or parked decision.</param>
public sealed record DraftUserStoryQuery(string TeamId, string Topic) : IQuery<StoryDraft>;
