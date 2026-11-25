namespace Zava.Scrum.Domain.Teams;

/// <summary>A person's role within one team (ADR 0018). The app-wide Admin role is separate and reaches every team.</summary>
public enum TeamRole
{
    /// <summary>Reads the team.</summary>
    Reader,

    /// <summary>Reads the team and answers for themselves.</summary>
    Member,

    /// <summary>Approves, schedules, edits prompts and behaviours, and manages the team's memberships.</summary>
    TeamLead,
}

/// <summary>What a request needs, derived from its route.</summary>
public enum TeamPermission
{
    /// <summary>Read the team's data.</summary>
    Read,

    /// <summary>Run, schedule or edit: ceremonies, schedule, prompts, behaviours, roster.</summary>
    Operate,

    /// <summary>Approve or reject a decision.</summary>
    Approve,

    /// <summary>Manage the registry and anything across teams. Admin only.</summary>
    Registry,
}

/// <summary>One person's membership of one team.</summary>
/// <param name="TeamId">The team.</param>
/// <param name="ObjectId">The person's Entra object id. Never matched by name.</param>
/// <param name="Role">The role in this team.</param>
public sealed record TeamMembership(string TeamId, Guid ObjectId, TeamRole Role);
