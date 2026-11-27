namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>Reads durable completed demo removals without resolving reset planning, Azure DevOps or identity credentials.</summary>
public interface IDemoTeamRemovalHistory
{
    /// <summary>Reports a journal-proven removed registry id, including when new reset operations are disabled.</summary>
    Task<bool> IsRemovedTeamAsync(string teamId, CancellationToken ct);
}
