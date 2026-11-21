namespace Mcm.Scrum.Application.Features.Standups.GetNextSpeaker;

/// <summary>The next speaker. <see cref="MemberId"/> is <see langword="null"/> when everyone has had a turn.</summary>
/// <param name="MemberId">Member id.</param>
/// <param name="DisplayName">Full display name.</param>
/// <param name="FirstName">Name to say aloud.</param>
/// <param name="Absent">True when absent today.</param>
/// <param name="HasFlaggedItems">True when the member owns a flagged item, so a grounded clarifying question is possible.</param>
public sealed record NextSpeakerDto(string? MemberId, string? DisplayName, string? FirstName, bool Absent, bool HasFlaggedItems);
