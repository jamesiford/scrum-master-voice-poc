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
internal sealed record RunResult(
    string RunId,
    string Label,
    string Build,
    string Api,
    DateTimeOffset StartedAt,
    string SessionId,
    int ReportedP50Ms,
    IReadOnlyList<TimingTurn> Turns,
    string? Team = null);
