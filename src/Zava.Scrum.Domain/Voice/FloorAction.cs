namespace Zava.Scrum.Domain.Voice;

/// <summary>Something the gateway must do. The gateway only executes these; it decides nothing.</summary>
public abstract record FloorAction;

/// <summary>Make the agent speak.</summary>
/// <param name="Reason">Why.</param>
/// <param name="MemberId">The member it concerns, if any.</param>
public sealed record AgentSpeak(SpeakReason Reason, string? MemberId) : FloorAction;

/// <summary>Cancel the agent's current response and truncate its audio.</summary>
public sealed record CancelResponse : FloorAction;

/// <summary>Compare the member's update with their brief.</summary>
/// <param name="MemberId">The member.</param>
/// <param name="Version">Echo this in <see cref="ReconcileFinished"/>.</param>
/// <param name="Background">True when the floor moves on without waiting; the result only records events.</param>
public sealed record RunReconcile(string MemberId, int Version, bool Background = false) : FloorAction;

/// <summary>Ask the application for the next speaker.</summary>
public sealed record AdvanceSpeaker : FloorAction;

/// <summary>Record a parked topic from the recent off-topic discussion.</summary>
public sealed record ParkTopic : FloorAction;

/// <summary>Finish the session.</summary>
public sealed record EndSession : FloorAction;

/// <summary>Why a member is skipped for now.</summary>
public enum DeferReason
{
    /// <summary>They (or a teammate) said they stepped away.</summary>
    StepAway,

    /// <summary>They were called and did not respond.</summary>
    NoResponse,
}

/// <summary>Skip a member for now; the agent circles back to them at the end.</summary>
/// <param name="MemberId">The member.</param>
/// <param name="Reason">Why.</param>
public sealed record DeferSpeaker(string MemberId, DeferReason Reason) : FloorAction;
