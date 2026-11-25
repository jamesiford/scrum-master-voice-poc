namespace Zava.Scrum.Domain.Identity;

/// <summary>
/// The app roles defined on the portal's Entra app registration (<c>infra/entra/portal-app-roles.json</c>). They are app-wide;
/// team-scoped access is a later step.
/// </summary>
public static class AppRoles
{
    /// <summary>Operates sessions, schedules and settings.</summary>
    public const string Admin = "Zava.Scrum.Admin";

    /// <summary>Approves follow-ups, plans and calendars.</summary>
    public const string Approver = "Zava.Scrum.Approver";

    /// <summary>Reads everything, including the audit chain.</summary>
    public const string Reader = "Zava.Scrum.Reader";
}
