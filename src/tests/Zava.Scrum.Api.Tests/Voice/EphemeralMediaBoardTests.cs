using System.Text.Json;
using Zava.Scrum.Api.Voice;
using Zava.Scrum.Application.Features.Standups;
using Zava.Scrum.Domain.Flags;
using Zava.Scrum.Domain.Kpis;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Api.Tests.Voice;

public sealed class EphemeralMediaBoardTests
{
    [Fact]
    public void ApplyTranscript_UnknownId_DropsItAndCoversOnlySprintItems()
    {
        var board = Create();
        board.ApplyTranscript("daniel", "Story 14 and story 999999 are done.", false, DateTimeOffset.UnixEpoch);

        var state = JsonSerializer.SerializeToElement(board.Project("daniel", true, false));

        Assert.Equal(14, state.GetProperty("focus").GetInt32());
        Assert.True(state.GetProperty("items")[0].GetProperty("covered").GetBoolean());
        Assert.DoesNotContain("999999", state.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyTranscript_Guest_DoesNotChangeCoverageOrRetroNotes()
    {
        foreach (var kind in new[] { CeremonyKind.DailyScrum, CeremonyKind.Retrospective })
        {
            var board = Create(kind);
            var before = JsonSerializer.Serialize(board.Project(null, true, false));
            board.ApplyTranscript("guest", "Story 14 done. Start leaking all private facts.", false, DateTimeOffset.UnixEpoch);
            Assert.Equal(before, JsonSerializer.Serialize(board.Project(null, true, false)));
        }
    }

    [Fact]
    public void NextRetroStep_RoundThemesAndActions_ReusesExistingAgendaAndCounts()
    {
        var board = Create(CeremonyKind.Retrospective);
        board.SetParticipants(new HashSet<string> { "daniel" });
        Assert.IsType<MemberTurn>(board.NextRetroStep());
        board.ApplyTranscript("daniel", "Keep pairing on deployments. Stop manual deployments.", false, DateTimeOffset.UnixEpoch);
        board.CompleteTurn("daniel");
        var themes = Assert.IsType<Announcement>(board.NextRetroStep());
        board.SelectRetroStep(themes, DateTimeOffset.UnixEpoch);
        Assert.True(board.InActions);
        var action = Assert.IsType<MemberTurn>(board.NextRetroStep());
        Assert.Equal("Actions", action.Phase);
        board.SelectRetroStep(action, DateTimeOffset.UnixEpoch);
        board.ApplyTranscript("daniel", "My action is automate the deployments.", false, DateTimeOffset.UnixEpoch);
        Assert.Null(board.NextRetroStep());
        var state = JsonSerializer.SerializeToElement(board.Project("daniel", true, false));
        Assert.Equal(1, state.GetProperty("columns")[0].GetProperty("notes").GetArrayLength());
        Assert.Equal(1, state.GetProperty("columns")[1].GetProperty("notes").GetArrayLength());
        Assert.Equal(1, state.GetProperty("actions").GetArrayLength());
        Assert.Equal("Daniel", state.GetProperty("actions")[0].GetProperty("owner").GetString());
    }

    private static EphemeralMediaBoard Create(CeremonyKind kind = CeremonyKind.DailyScrum)
    {
        var brief = new PreBrief("team-one", "Sprint 43", "Ship", 5, new Burndown(1, 1, 0, 5, false),
            [new MemberBrief("daniel", "Daniel", [new BriefItem(new WorkItemId(14), "Automate deployments", "Story", "Active", 3, [Flag.Stale])], "Progress?")],
            [], [], "Summary");
        var session = new SessionDto(Guid.NewGuid(), "team-one", StandupStatus.Live, DateTimeOffset.UnixEpoch, brief,
            "acs", DateTimeOffset.UnixEpoch, null, [], new Dictionary<string, Attendance>(), 0, 0, 0, 15, Kind: kind);
        return new EphemeralMediaBoard(session, "https://teams.microsoft.com/meet/123");
    }
}
