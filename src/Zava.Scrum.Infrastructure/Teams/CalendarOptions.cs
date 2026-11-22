using System.ComponentModel.DataAnnotations;
using Zava.Scrum.Application.Abstractions.Ports;

namespace Zava.Scrum.Infrastructure.Teams;

/// <summary>Binds <c>Calendar__*</c>: the sprint calendar (M9d). Implements the Application port so handlers never read configuration.</summary>
public sealed class CalendarOptions : ICalendarSettings
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Calendar";

    /// <summary>Gets or sets the agent's mailbox in the demo tenant (user principal name) that owns the meetings. Empty means Graph is not configured and the <c>.ics</c> download is the fallback.</summary>
    public string? Organizer { get; set; }

    /// <summary>Gets or sets the Entra app (client) id that holds the Graph <c>Calendars.ReadWrite</c> application permission. Empty means the default Azure credential (a managed identity in Azure).</summary>
    public string? ClientId { get; set; }

    /// <summary>Gets or sets the secret of <see cref="ClientId"/>. A secret: user secrets locally, a Container Apps secret in Azure. PROD: a managed identity, no secret.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Gets or sets the sprint planning length in minutes.</summary>
    [Range(5, 480)]
    public int PlanningMinutes { get; set; } = 60;

    /// <summary>Gets or sets the sprint review length in minutes.</summary>
    [Range(5, 480)]
    public int ReviewMinutes { get; set; } = 30;

    /// <summary>Gets or sets the sprint retrospective length in minutes.</summary>
    [Range(5, 480)]
    public int RetroMinutes { get; set; } = 45;

    /// <summary>Gets or sets a value indicating whether the team holds a sprint review (<c>Team:HasSprintReview</c>, copied at startup).</summary>
    public bool HasSprintReview { get; set; }
}
