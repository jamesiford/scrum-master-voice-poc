using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Application.Features.Standups.GetNextSpeaker;

/// <summary>The next speaker. <see cref="MemberId"/> is <see langword="null"/> when everyone has had a turn.</summary>
/// <param name="MemberId">Member id.</param>
/// <param name="DisplayName">Full display name.</param>
/// <param name="FirstName">Name to say aloud.</param>
/// <param name="Absent">True when absent today.</param>
/// <param name="CircleBack">True when the member was skipped earlier and is being asked again.</param>
/// <param name="HasFlaggedItems">True when the member owns a flagged item, so a grounded clarifying question is possible.</param>
/// <param name="Announcement">When set, the agent speaks to the whole team for this reason (for example the retrospective themes) and nobody holds the floor.</param>
/// <param name="Phase">The phase of the ceremony this step belongs to (<c>Round</c> for the standup).</param>
public sealed record NextSpeakerDto(string? MemberId, string? DisplayName, string? FirstName, bool Absent, bool HasFlaggedItems, bool CircleBack = false, SpeakReason? Announcement = null, string Phase = "Round");
