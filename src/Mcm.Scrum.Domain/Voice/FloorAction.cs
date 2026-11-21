namespace Mcm.Scrum.Domain.Voice;

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
