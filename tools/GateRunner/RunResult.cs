namespace Zava.Scrum.Tools.GateRunner;

/// <summary>The result of one run of the Simulated Room cast, as saved to JSON.</summary>
/// <param name="RunId">A unique id for this run.</param>
/// <param name="Label">The configuration under test, for example <c>decision</c> or <c>rules</c>.</param>
/// <param name="Build">The build id: a commit, or the deployed image tag.</param>
/// <param name="Api">The API that was driven.</param>
/// <param name="StartedAt">When the run started (UTC).</param>
/// <param name="SessionId">The standup session id.</param>
/// <param name="ReportedP50Ms">The p50 the API itself reported for the session.</param>
/// <param name="Turns">Every timed agent reply.</param>
/// <param name="Team">The team the standup ran for; <see langword="null"/> in results saved before teams existed.</param>
/// <param name="TalkOvers">Times the agent was cut off by someone speaking: in a scripted run, the agent speaking over a person (a false end of speech).</param>
/// <param name="HumanSegments">Transcript pieces from people. A pause longer than the silence setting splits one update into several.</param>
/// <param name="Exchanges">The transcript in order, for the quality score.</param>
internal sealed record RunResult(
    string RunId,
    string Label,
    string Build,
    string Api,
    DateTimeOffset StartedAt,
    string SessionId,
    int ReportedP50Ms,
    IReadOnlyList<TimingTurn> Turns,
    string? Team = null,
    int TalkOvers = 0,
    int HumanSegments = 0,
    IReadOnlyList<Exchange>? Exchanges = null);

/// <summary>One transcript line.</summary>
/// <param name="MemberId">The roster member, or <see langword="null"/> for the agent.</param>
/// <param name="Text">The words.</param>
internal sealed record Exchange(string? MemberId, string Text);
