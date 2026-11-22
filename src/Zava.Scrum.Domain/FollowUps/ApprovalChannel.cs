namespace Zava.Scrum.Domain.FollowUps;

/// <summary>Where a person approved a follow-up. It is written into the attribution on Azure DevOps.</summary>
public enum ApprovalChannel
{
    /// <summary>A Teams card action.</summary>
    Teams,

    /// <summary>The admin portal.</summary>
    Portal,
}
