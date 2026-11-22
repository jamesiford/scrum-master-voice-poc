using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Calendars;

namespace Mcm.Scrum.Application.Features.Calendars;

/// <summary>
/// Builds the Teams card for a proposed sprint calendar and the card that replaces it after a decision. One place for the wording (DRY).
/// The card key is <c>calendar:{id}</c>; <see cref="TeamsMessage.FollowUpId"/> carries the calendar id.
/// </summary>
internal static class CalendarCards
{
    /// <summary>Gets the key the card is sent and updated under.</summary>
    /// <param name="id">The calendar.</param>
    /// <returns>The dedupe key.</returns>
    public static string Key(Guid id) => $"calendar:{id}";

    /// <summary>Builds the card with Approve and Reject.</summary>
    /// <param name="c">The calendar.</param>
    /// <returns>The message.</returns>
    public static TeamsMessage Initial(SprintCalendarData c) =>
        new(CardKind.SprintCalendar, c.TeamId, c.Id, Key(c.Id), $"Proposed sprint calendar: {Name(c)}", Summary(c), Facts(c), c.Id);

    /// <summary>Builds the card that replaces the first one once the calendar is decided.</summary>
    /// <param name="c">The calendar.</param>
    /// <param name="status">Plain-language outcome.</param>
    /// <returns>The message.</returns>
    public static TeamsMessage Result(SprintCalendarData c, string status) =>
        Initial(c) with { Kind = CardKind.FollowUpResult, Facts = [.. Facts(c), new CardFact("Status", status)] };

    private static string Name(SprintCalendarData c) => c.IterationPath[(c.IterationPath.LastIndexOf('\\') + 1)..];

    private static string Summary(SprintCalendarData c)
    {
        var standups = c.Meetings.Count(m => m.Kind == MeetingKind.Standup);
        return $"{c.Meetings.Count} meetings: {standups} daily standups, planning on day one and the retrospective on the last day. Approve to create them as Teams meetings in the agent's calendar and invite the team. Nothing is created until you approve.";
    }

    private static List<CardFact> Facts(SprintCalendarData c)
    {
        var zone = TimeZoneInfo.TryFindSystemTimeZoneById(c.TimeZone, out var z) ? z : TimeZoneInfo.Utc;
        string At(DateTimeOffset t) => TimeZoneInfo.ConvertTime(t, zone).ToString("ddd d MMM HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        var standups = c.Meetings.Where(m => m.Kind == MeetingKind.Standup).ToList();
        var facts = c.Meetings.Where(m => m.Kind != MeetingKind.Standup).Select(m => new CardFact(m.Kind.ToString(), $"{At(m.StartsAt)} ({(m.EndsAt - m.StartsAt).TotalMinutes:0} min)")).ToList();
        if (standups.Count > 0)
        {
            facts.Insert(0, new CardFact("Standups", $"{standups.Count}, daily from {At(standups[0].StartsAt)} ({(standups[0].EndsAt - standups[0].StartsAt).TotalMinutes:0} min)"));
        }

        return facts;
    }
}
