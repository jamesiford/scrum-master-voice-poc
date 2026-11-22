using Mcm.Scrum.Application.Abstractions.Messaging;
using Mcm.Scrum.Application.Abstractions.Ports;
using Mcm.Scrum.Domain.Flags;
using Mcm.Scrum.Domain.Kpis;
using Mcm.Scrum.Domain.Standups;
using Mcm.Scrum.Domain.Teams;
using Mcm.Scrum.Domain.Work;
using Microsoft.Extensions.DependencyInjection;

namespace Mcm.Scrum.Application.Tests.TestDoubles;

internal sealed class FakeReconcileAgent(ReconcileSuggestion? suggestion) : IReconcileAgent
{
    public Task<ReconcileSuggestion?> ReconcileAsync(MemberBrief brief, string utterance, CancellationToken ct, IReadOnlyList<RecordedStatement>? recorded = null) => Task.FromResult(suggestion);
}

internal sealed class FakePrompts(Dictionary<string, string> prompts) : IPromptStore
{
    public Task<string?> GetAsync(string name, CancellationToken ct) => Task.FromResult(prompts.GetValueOrDefault(name));

    public Task<IReadOnlyList<string>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>([.. prompts.Keys]);

    public Task<bool> SaveAsync(string name, string text, CancellationToken ct)
    {
        var exists = prompts.ContainsKey(name);
        if (exists)
        {
            prompts[name] = text;
        }

        return Task.FromResult(exists);
    }
}

/// <summary>A prepared standup with two members (m-priya has a stale story 1, m-marcus a blocked bug 12) and the services to run use cases against it.</summary>
internal sealed class StandupHarness : IAsyncDisposable
{
    public static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);
    private readonly ServiceProvider _sp;

    private StandupHarness(ServiceProvider sp, InMemorySessionRepository sessions, Guid sessionId)
    {
        _sp = sp;
        Sessions = sessions;
        SessionId = sessionId;
    }

    public InMemorySessionRepository Sessions { get; }

    public Guid SessionId { get; }

    public ISender Sender => _sp.GetRequiredService<ISender>();

    public T Get<T>()
        where T : notnull => _sp.GetRequiredService<T>();

    public static async Task<StandupHarness> CreateAsync(ReconcileSuggestion? suggestion = null, Dictionary<string, string>? prompts = null, Action<IServiceCollection>? configure = null)
    {
        var story = new BriefItem(new WorkItemId(1), "Payment retry", "User Story", "Active", 3, [Flag.Stale]);
        var bug = new BriefItem(new WorkItemId(12), "Gateway timeouts", "Bug", "Active", 4, [Flag.Blocked]);
        var brief = new PreBrief(
            "t1", "p\\s", "Ship retries", 6, new Burndown(10, 4, 6, 6, false),
            [
                new MemberBrief("m-priya", "Priya S.", [story], "Story 1 has been Active for 3 days. Still on track?"),
                new MemberBrief("m-marcus", "Marcus O.", [bug], "Bug 12 is blocked. What is needed?"),
            ],
            [], [], "Summary");
        var sessions = new InMemorySessionRepository();
        var session = StandupSession.Prepare(Guid.NewGuid(), "t1", Now, brief);
        await sessions.SaveAsync(session, CancellationToken.None);
        var teams = new InMemoryTeamRepository();
        await teams.SaveAsync(new Team("t1", "Payments", "UTC", 15, new AdoSettings("o", "p", "t", "a"), new TeamBehaviours(), "Ship retries"), CancellationToken.None);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<ISessionRepository>(sessions);
        services.AddSingleton<ITeamRepository>(teams);
        services.AddSingleton<IClock>(new FixedClock(Now));
        services.AddSingleton<ITeamsMessenger>(new RecordingTeamsMessenger());
        services.AddSingleton<IMeetingBridge>(new FakeMeetingBridge());
        services.AddSingleton<IMemberRepository>(new InMemoryMemberRepository());
        services.AddSingleton<IFollowUpRepository>(new InMemoryFollowUpRepository());
        services.AddSingleton<IReconcileAgent>(new FakeReconcileAgent(suggestion));
        services.AddSingleton<IPromptStore>(new FakePrompts(prompts ?? []));
        services.AddSingleton<IEmailSender>(new FakeEmailSender());
        services.AddSingleton<IRetroSettings>(new FakeRetroSettings());
        configure?.Invoke(services);
        return new StandupHarness(services.BuildServiceProvider(), sessions, session.Id);
    }

    public async Task<StandupSession> LoadAsync() => (await Sessions.GetAsync(SessionId, CancellationToken.None))!;

    public ValueTask DisposeAsync() => _sp.DisposeAsync();
}
