namespace Zava.Scrum.Domain.Reports;

/// <summary>The five read-only, permission-scoped questions available in Copilot.</summary>
public enum CopilotSkill
{
    /// <summary>Open items blocked in Azure DevOps.</summary>
    Blocked,
    /// <summary>The asker's recorded commitments that still refer to readable sprint items.</summary>
    Commitments,
    /// <summary>Open sprint items flagged at risk or overdue by the existing rules.</summary>
    SprintRisk,
    /// <summary>Today's standup lifecycle, without meeting content.</summary>
    StandupStatus,
    /// <summary>Readable sprint items assigned to the asker's verified roster identity.</summary>
    MyItems,
}
