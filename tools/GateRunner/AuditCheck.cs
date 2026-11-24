namespace Zava.Scrum.Tools.GateRunner;

/// <summary>
/// The traceability gate: after a run that approves follow-ups, the audit endpoint must show a complete chain from the spoken update to the
/// approved Azure DevOps write. The endpoint arrives with prompt 3, so until then this check fails by design; a gate that passes without
/// checking would be worse than no gate.
/// </summary>
internal static class AuditCheck
{
    /// <summary>Runs the check.</summary>
    /// <param name="sessionId">The session whose follow-ups were approved.</param>
    /// <returns><see langword="true"/> when the chain is complete. Currently always <see langword="false"/>.</returns>
    public static bool Run(string sessionId)
    {
        // DEMO: placeholder that fails on purpose until the audit endpoint exists (prompt 3). Replace the body with a call to it,
        // and fail on any missing link (utterance, analysis, follow-up, approval, work item revision).
        Console.WriteLine($"AUDIT CHECK FAILED for session {sessionId}: TODO prompt 3, the audit endpoint does not exist yet, so traceability is unverified.");
        return false;
    }
}
