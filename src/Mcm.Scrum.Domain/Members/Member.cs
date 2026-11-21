namespace Mcm.Scrum.Domain.Members;

/// <summary>A roster member (SPEC 15, <c>members</c> container).</summary>
/// <param name="Id">Member id.</param>
/// <param name="TeamId">Owning team.</param>
/// <param name="Key">Short stable key; work items map to it through the <c>demo-owner-{key}</c> tag or the assignee.</param>
/// <param name="DisplayName">Name the agent says aloud.</param>
/// <param name="AdoUniqueName">Azure DevOps unique name, or <see langword="null"/> when not mapped.</param>
/// <param name="Pronunciation">Phonetic hint for speech.</param>
/// <param name="SpeakingOrder">Position in the standup.</param>
/// <param name="Active">False when absent today.</param>
public sealed record Member(
    string Id,
    string TeamId,
    string Key,
    string DisplayName,
    string? AdoUniqueName,
    string Pronunciation,
    int SpeakingOrder,
    bool Active);
