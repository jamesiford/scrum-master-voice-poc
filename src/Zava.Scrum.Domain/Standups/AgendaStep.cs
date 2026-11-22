using Mcm.Scrum.Domain.Voice;

namespace Mcm.Scrum.Domain.Standups;

/// <summary>One thing the floor does next in a ceremony.</summary>
public abstract record AgendaStep;

/// <summary>Call on a member.</summary>
/// <param name="Member">The member.</param>
/// <param name="Absent">True when they are absent, so the agent only announces it.</param>
/// <param name="CircleBack">True when they were skipped earlier and are being asked again.</param>
/// <param name="Phase">The phase this turn belongs to (<c>Round</c> or <c>Actions</c> in a retrospective).</param>
public sealed record MemberTurn(MemberBrief Member, bool Absent, bool CircleBack, string Phase = "Round") : AgendaStep;

/// <summary>The agent speaks to everyone, for example reading back themes. No member holds the floor.</summary>
/// <param name="Reason">Why the agent speaks.</param>
/// <param name="Phase">The phase this announcement opens.</param>
public sealed record Announcement(SpeakReason Reason, string Phase) : AgendaStep;
