namespace Mcm.Scrum.Domain.Standups;

/// <summary>
/// The daily standup's order: members in speaking order, skipping anyone deferred, then the deferred members so nobody is forgotten
/// (the agent circles back at the end). This is the former <c>StandupSession.NextSpeaker</c> logic, moved behind
/// <see cref="ICeremonyAgenda"/> without a behaviour change.
/// </summary>
public sealed class StandupAgenda : ICeremonyAgenda
{
    /// <summary>Gets the shared instance; the agenda has no state.</summary>
    public static StandupAgenda Instance { get; } = new();

    /// <inheritdoc />
    public AgendaStep? Next(StandupSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var open = session.PreBrief?.Members.Where(m => !session.HandledMemberIds.Contains(m.MemberId)).ToList() ?? [];
        var next = open.FirstOrDefault(m => !session.IsDeferred(m.MemberId));
        var circleBack = false;
        if (next is null)
        {
            next = open.FirstOrDefault();
            circleBack = next is not null;
        }

        return next is null
            ? null
            : new MemberTurn(next, session.Attendance.TryGetValue(next.MemberId, out var a) && a == Attendance.Absent, circleBack);
    }
}
