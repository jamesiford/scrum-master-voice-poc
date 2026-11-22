using System.Globalization;
using System.Text;

namespace Zava.Scrum.Domain.Calendars;

/// <summary>Formats meetings as an iCalendar file: the fallback when Microsoft Graph is not configured, so the Scrum Master can import them by hand.</summary>
public static class IcsFormatter
{
    /// <summary>Formats the meetings (RFC 5545, UTC times, CRLF line ends).</summary>
    /// <param name="calendarName">The calendar name, for example the iteration.</param>
    /// <param name="meetings">The meetings.</param>
    /// <param name="stamp">The creation stamp (UTC).</param>
    /// <returns>The <c>.ics</c> text.</returns>
    public static string Format(string calendarName, IReadOnlyList<CalendarMeeting> meetings, DateTimeOffset stamp)
    {
        ArgumentNullException.ThrowIfNull(meetings);
        var sb = new StringBuilder();
        Line(sb, "BEGIN:VCALENDAR");
        Line(sb, "VERSION:2.0");
        Line(sb, "PRODID:-//Zava Scrum Agent//Sprint calendar//EN");
        Line(sb, "X-WR-CALNAME:" + Escape(calendarName));
        foreach (var m in meetings)
        {
            Line(sb, "BEGIN:VEVENT");
            Line(sb, $"UID:{m.Key}@zava-scrum-agent");
            Line(sb, "DTSTAMP:" + Stamp(stamp));
            Line(sb, "DTSTART:" + Stamp(m.StartsAt));
            Line(sb, "DTEND:" + Stamp(m.EndsAt));
            Line(sb, "SUMMARY:" + Escape(m.Title));
            if (m.JoinUrl is not null)
            {
                Line(sb, "URL:" + m.JoinUrl);
            }

            Line(sb, "END:VEVENT");
        }

        Line(sb, "END:VCALENDAR");
        return sb.ToString();
    }

    private static void Line(StringBuilder sb, string text) => sb.Append(text).Append("\r\n");

    private static string Stamp(DateTimeOffset t) => t.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Escape(string s) => s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace(";", "\\;", StringComparison.Ordinal).Replace(",", "\\,", StringComparison.Ordinal);
}
