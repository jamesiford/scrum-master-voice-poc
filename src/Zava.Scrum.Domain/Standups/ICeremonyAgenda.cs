namespace Zava.Scrum.Domain.Standups;

/// <summary>
/// Decides what the floor does next in a ceremony. The standup and the retrospective use the same floor control and gateway; only the
/// order of steps differs, and that order lives behind this interface (SOLID: O). Pure: it reads the session and returns a step.
/// </summary>
public interface ICeremonyAgenda
{
    /// <summary>Gets the next step.</summary>
    /// <param name="session">The session, with its attendance, handled members and events.</param>
    /// <returns>The step, or <see langword="null"/> when the ceremony has nothing left to do (the agent closes).</returns>
    AgendaStep? Next(StandupSession session);
}
