using Zava.Scrum.Domain.Identity;

namespace Zava.Scrum.Api.Auth;

/// <summary>Authorization policy names and the app roles behind them (SPEC 13).</summary>
public static class Policies
{
    /// <summary>The authentication scheme the portal API uses.</summary>
    public const string Scheme = "Portal";

    /// <summary>Any signed-in app role: read everything.</summary>
    public const string Reader = "Zava.Reader";

    /// <summary>Operate sessions, schedules and settings.</summary>
    public const string Admin = "Zava.Admin";

    /// <summary>Approve follow-ups, plans and calendars.</summary>
    public const string Approver = "Zava.Approver";

    /// <summary>Gets the roles that grant <see cref="Reader"/>: reading is allowed to every role.</summary>
    public static IReadOnlyList<string> ReadRoles { get; } = [AppRoles.Reader, AppRoles.Approver, AppRoles.Admin];
}
