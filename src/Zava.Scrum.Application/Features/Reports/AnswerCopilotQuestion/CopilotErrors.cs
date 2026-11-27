using Zava.Scrum.Domain.Common;

namespace Zava.Scrum.Application.Features.Reports.AnswerCopilotQuestion;

/// <summary>Explicit refusals for permission-scoped Copilot skills.</summary>
public static class CopilotErrors
{
    /// <summary>Gets the refusal when the channel and delegated assertion do not identify the same deployment user.</summary>
    public static Error SignInRequired { get; } = Error.Forbidden("copilot.sign_in_required", "Sign in as yourself in this context. I cannot read Azure DevOps without your own matching sign-in.");

    /// <summary>Gets the refusal for a user with no open team membership.</summary>
    public static Error NoTeam { get; } = Error.Forbidden("copilot.no_team", "You are not a member of an active Zava team. Ask a team lead to add your Entra identity; no team data was read.");

    /// <summary>Gets the refusal for an absent or ambiguous verified roster mapping.</summary>
    public static Error RosterRequired { get; } = Error.Forbidden("copilot.roster_required", "Your Entra identity has no unique roster mapping for this team. Ask the team lead to verify your roster identity and Azure DevOps unique name; I will not guess from a display name or demo tag.");

    /// <summary>Gets the refusal when Azure DevOps denies the asker's access.</summary>
    public static Error AccessDenied { get; } = Error.Forbidden("copilot.ado_denied", "Azure DevOps refused your access to this team's project or sprint. Ask the Azure DevOps project administrator to grant you access; I will not use the agent's permissions.");
}
