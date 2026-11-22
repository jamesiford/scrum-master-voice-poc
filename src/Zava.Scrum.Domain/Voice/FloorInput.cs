namespace Mcm.Scrum.Domain.Voice;

/// <summary>Something that happened that <see cref="FloorControl"/> must react to.</summary>
public abstract record FloorInput;

/// <summary>The standup begins.</summary>
/// <param name="Now">Current time.</param>
public sealed record StartFloor(DateTimeOffset Now) : FloorInput;

/// <summary>The agent finished speaking (audio fully played).</summary>
/// <param name="Now">Current time.</param>
public sealed record AgentSpeechEnded(DateTimeOffset Now) : FloorInput;

/// <summary>The application chose the next speaker.</summary>
/// <param name="MemberId">The member, or <see langword="null"/> when everyone has had a turn.</param>
/// <param name="Absent">True when the member is absent today.</param>
/// <param name="CircleBack">True when the member was skipped earlier and is being asked again.</param>
/// <param name="CanClarify">False when no clarifying question is possible (nothing flagged), so the next speaker is called without waiting.</param>
public sealed record SpeakerSelected(string? MemberId, bool Absent, bool CanClarify = true, bool CircleBack = false) : FloorInput;

/// <summary>A participant finished an utterance.</summary>
/// <param name="MemberId">Who spoke.</param>
/// <param name="DurationMs">How long they spoke.</param>
/// <param name="At">When the utterance ended.</param>
public sealed record MemberSpoke(string MemberId, int DurationMs, DateTimeOffset At) : FloorInput;

/// <summary>Clock tick; drives silence, timebox and no-response rules.</summary>
/// <param name="Now">Current time.</param>
public sealed record Tick(DateTimeOffset Now) : FloorInput;

/// <summary>The reconcile check finished.</summary>
/// <param name="Version">The version from the <see cref="RunReconcile"/> action; stale results are ignored.</param>
/// <param name="HasQuestion">True when there is a clarifying question to ask.</param>
public sealed record ReconcileFinished(int Version, bool HasQuestion) : FloorInput;

/// <summary>A human takes over, or hands back.</summary>
/// <param name="On">True to take over.</param>
public sealed record HumanOverride(bool On) : FloorInput;

/// <summary>The agent is muted or unmuted from the portal.</summary>
/// <param name="On">True to pause.</param>
public sealed record PauseFloor(bool On) : FloorInput;

/// <summary>Someone said the agent's name.</summary>
public sealed record AgentAddressed : FloorInput;

/// <summary>Move on to the next speaker now.</summary>
public sealed record SkipSpeaker : FloorInput;

/// <summary>Someone started talking over the agent.</summary>
/// <param name="MemberId">Who.</param>
/// <param name="IsHuman">True for the human Scrum Master, who may always interrupt.</param>
public sealed record BargeIn(string MemberId, bool IsHuman) : FloorInput;

/// <summary>A participant started or stopped talking, as heard by the voice service. Silence is only counted once speech has stopped.</summary>
/// <param name="MemberId">Who.</param>
/// <param name="Active">True while they talk.</param>
/// <param name="At">When it changed.</param>
public sealed record SpeechActive(string MemberId, bool Active, DateTimeOffset At) : FloorInput;

/// <summary>The current speaker cannot speak right now (stepped away, or was said to have), so skip them and circle back.</summary>
/// <param name="Reason">Why.</param>
public sealed record DeferCurrent(DeferReason Reason) : FloorInput;

/// <summary>The agenda says the agent speaks to everyone next (a ceremony step that is not a member turn).</summary>
/// <param name="Reason">What the agent says.</param>
public sealed record AnnounceStep(SpeakReason Reason) : FloorInput;
