using Zava.Scrum.Domain.FollowUps;
using Zava.Scrum.Domain.Members;
using Zava.Scrum.Domain.Standups;
using Zava.Scrum.Domain.Voice;
using Zava.Scrum.Domain.Work;

namespace Zava.Scrum.Domain.Tests.Standups;

public sealed class RetroItemClassifierTests
{
    [Theory]
    [InlineData("Start pairing on the hard stories", RetroCategory.Start)]
    [InlineData("We should try mob programming", RetroCategory.Start)]
    [InlineData("We are missing a test environment", RetroCategory.Start)]
    [InlineData("We should stop having so many meetings", RetroCategory.Stop)]
    [InlineData("Deployments take too long", RetroCategory.Stop)]
    [InlineData("We need to do better at estimating", RetroCategory.Stop)]
    [InlineData("Keep the daily demos", RetroCategory.Continue)]
    [InlineData("Code reviews went well", RetroCategory.Continue)]
    [InlineData("More of the pairing sessions", RetroCategory.Continue)]
    [InlineData("The release process", null)]
    [InlineData("", null)]
    public void Classify_Text_ReturnsCategoryOrNull(string text, RetroCategory? expected) =>
        Assert.Equal(expected, RetroItemClassifier.Classify(text));

    [Fact]
    public void Split_SentencesAndBut_ReturnsSeparateNotes()
    {
        var notes = RetroItemClassifier.Split("Reviews went well, but we should stop the long standups. Also the build is slow. Yes.");

        Assert.Equal(["Reviews went well", "We should stop the long standups", "The build is slow"], notes);
    }

    [Fact]
    public void Split_Fragments_AreDropped() =>
        Assert.Empty(RetroItemClassifier.Split("Yes. Okay then."));
}

public sealed class RetroInterpreterTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    private static readonly MemberBrief[] Roster =
    [
        new("m1", "Daniel Park", [], "q"),
        new("m2", "Maya Lin", [], "q"),
        new("m3", "Admin", [], "q"),
    ];

    private static StandupEvent Note(string member, string text, RetroCategory? category) =>
        new(Guid.NewGuid(), Now, StandupEventType.RetroItem, member, [], text, Category: category);

    [Fact]
    public void Round_ClearAndUnclearNotes_RecordsBothAndAsksOneQuestion()
    {
        var result = RetroInterpreter.Round("m1", "We should stop long meetings. The release process is painful.", false, false, []);

        Assert.Equal([RetroCategory.Stop, null], result.Notes.Select(n => n.Category));
        Assert.Equal(RetroItemClassifier.ClarifyingQuestion, result.Question);
    }

    [Fact]
    public void Round_AnswerToQuestion_ReplacesTheUnsortedNote()
    {
        var pending = Note("m1", "The release process is painful", null);

        var result = RetroInterpreter.Round("m1", "Stop that one", true, false, [pending]);

        Assert.Equal([pending.Id], result.RetractedEventIds);
        var note = Assert.Single(result.Notes);
        Assert.Equal(RetroCategory.Stop, note.Category);
        Assert.Equal("The release process is painful", note.Text);
        Assert.Null(result.Question);
    }

    [Fact]
    public void Round_AnswerThatIsNoCategory_DoesNotAskAgain()
    {
        var result = RetroInterpreter.Round("m1", "Not sure really", true, false, [Note("m1", "The release process is painful", null)]);

        Assert.Empty(result.Notes);
        Assert.Null(result.Question);
    }

    [Fact]
    public void Round_Correction_RetractsTheLastNote()
    {
        var first = Note("m1", "We should stop long meetings", RetroCategory.Stop);

        var result = RetroInterpreter.Round("m1", "Actually we should keep the long meetings for planning", false, true, [first]);

        Assert.Equal([first.Id], result.RetractedEventIds);
        Assert.Equal(RetroCategory.Continue, Assert.Single(result.Notes).Category);
    }

    [Fact]
    public void Round_CorrectionInsideOneUtterance_KeepsOnlyTheCorrectedNoteAndRetractsNothingEarlier()
    {
        var earlier = Note("m1", "Code reviews went well", RetroCategory.Continue);

        var result = RetroInterpreter.Round("m1", "We should stop writing long tickets. Scratch that, we should keep tickets detailed for complex stories", false, true, [earlier]);

        Assert.Empty(result.RetractedEventIds);
        var note = Assert.Single(result.Notes);
        Assert.Equal(RetroCategory.Continue, note.Category);
        Assert.StartsWith("We should keep tickets detailed", note.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Round_AlreadyThreeNotes_AddsNoMore()
    {
        var existing = Enumerable.Range(0, RetroAgenda.MaxItemsPerMember).Select(i => Note("m1", $"We should stop thing {i}", RetroCategory.Stop)).ToList();

        var result = RetroInterpreter.Round("m1", "We should stop another thing", false, false, existing);

        Assert.Empty(result.Notes);
    }

    [Fact]
    public void Action_NamesATeammate_TheyOwnItAndAreConfirmed()
    {
        var result = RetroInterpreter.Action("m1", "Maya will add a test environment checklist", false, Roster, []);

        var note = Assert.Single(result.Notes);
        Assert.Equal("m2", note.OwnerMemberId);
        Assert.Contains("owned by Maya Lin", result.Question, StringComparison.Ordinal);
    }

    [Fact]
    public void Action_NoOneNamed_SpeakerOwnsIt() =>
        Assert.Equal("m1", Assert.Single(RetroInterpreter.Action("m1", "Write the release checklist this sprint", false, Roster, []).Notes).OwnerMemberId);

    [Fact]
    public void Action_Nothing_RecordsNothing() =>
        Assert.Empty(RetroInterpreter.Action("m1", "Nothing from me", false, Roster, []).Notes);

    [Fact]
    public void Action_ConfirmWithAnotherName_ChangesTheOwner()
    {
        var action = new StandupEvent(Guid.NewGuid(), Now, StandupEventType.ActionItem, "m1", [], "Write the checklist", OwnerMemberId: "m1");

        var result = RetroInterpreter.Action("m1", "No, give it to Maya", true, Roster, [action]);

        Assert.Equal([action.Id], result.RetractedEventIds);
        Assert.Equal("m2", Assert.Single(result.Notes).OwnerMemberId);
    }
}

public sealed class RetroAgendaTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    private static StandupSession Retro(CeremonyKind kind = CeremonyKind.Retrospective)
    {
        MemberBrief[] members = [new("m1", "Daniel Park", [], "q"), new("m2", "Maya Lin", [], "q"), new("m3", "Admin", [], "q")];
        var brief = new PreBrief("t1", "p", "g", 3, new Zava.Scrum.Domain.Kpis.Burndown(10, 4, 6, 3, false), members, [], [], "s");
        var s = StandupSession.Prepare(Guid.NewGuid(), "t1", Now, brief, kind);
        s.Start("simulated", null, 15, Now);
        return s;
    }

    [Fact]
    public void Next_RoundThenThemesThenActionsThenClose()
    {
        var s = Retro();
        var order = new List<string>();
        for (var guard = 0; guard < 20; guard++)
        {
            var step = RetroAgenda.Instance.Next(s);
            if (step is null)
            {
                break;
            }

            switch (step)
            {
                case MemberTurn m:
                    order.Add($"{m.Phase}:{m.Member.MemberId}");
                    if (m.Phase == "Round")
                    {
                        s.CompleteTurn(m.Member.MemberId);
                    }
                    else
                    {
                        s.RecordEvent(StandupEventType.Phase, m.Member.MemberId, [], RetroAgenda.ActionTurnPrefix + m.Member.MemberId, Now);
                        s.RecordEvent(StandupEventType.ActionItem, m.Member.MemberId, [], "Do a thing", Now, ownerMemberId: m.Member.MemberId);
                    }

                    break;
                case Announcement a:
                    order.Add("Announce:" + a.Reason);
                    s.RecordEvent(StandupEventType.Phase, null, [], a.Phase, Now);
                    break;
            }
        }

        Assert.Equal(["Round:m1", "Round:m2", "Round:m3", "Announce:RetroThemes", "Actions:m1", "Actions:m2", "Actions:m3"], order);
    }

    [Fact]
    public void Next_AbsentAndSilentMembers_AreNotAskedForActions()
    {
        var s = Retro();
        s.MarkAbsent("m2", Now);
        foreach (var id in new[] { "m1", "m2", "m3" })
        {
            s.CompleteTurn(id);
        }

        s.RecordEvent(StandupEventType.NoResponse, "m3", [], "no response", Now);
        s.RecordEvent(StandupEventType.Phase, null, [], RetroAgenda.ThemesMarker, Now);

        var step = Assert.IsType<MemberTurn>(RetroAgenda.Instance.Next(s));
        Assert.Equal("m1", step.Member.MemberId);
        s.RecordEvent(StandupEventType.Phase, "m1", [], RetroAgenda.ActionTurnPrefix + "m1", Now);
        Assert.Null(RetroAgenda.Instance.Next(s));
    }

    [Fact]
    public void Next_StandupAgenda_MatchesNextSpeaker()
    {
        var s = Retro(CeremonyKind.DailyScrum);

        var step = Assert.IsType<MemberTurn>(StandupAgenda.Instance.Next(s));

        Assert.Equal(s.NextSpeaker()!.Value.Member.MemberId, step.Member.MemberId);
    }

    [Fact]
    public void End_Retrospective_RaisesRetrospectiveEndedOnly()
    {
        var s = Retro();

        s.End(Now);

        var raised = s.DomainEvents.Select(e => e.GetType()).ToList();
        Assert.Contains(typeof(RetrospectiveEnded), raised);
        Assert.DoesNotContain(typeof(StandupEnded), raised);
    }

    [Fact]
    public void End_DailyScrum_RaisesStandupEnded()
    {
        var s = Retro(CeremonyKind.DailyScrum);

        s.End(Now);

        Assert.Contains(typeof(StandupEnded), s.DomainEvents.Select(e => e.GetType()));
    }
}

public sealed class ThemeGrouperTests
{
    private static StandupEvent Note(string text) =>
        new(Guid.NewGuid(), DateTimeOffset.UnixEpoch, StandupEventType.RetroItem, "m1", [], text, Category: RetroCategory.Stop);

    [Fact]
    public void Group_SharedKeyword_GroupsNotes()
    {
        var a = Note("Deployments take too long");
        var b = Note("Failed deployments block everyone");
        var c = Note("Standups run over");

        var themes = ThemeGrouper.Group([a, b, c]);

        var theme = Assert.Single(themes);
        Assert.Equal("deployment", theme.Keyword);
        Assert.Equal([a.Id, b.Id], theme.ItemIds);
    }

    [Fact]
    public void Group_OnlyAGenericWordIsShared_ReturnsNoThemes() =>
        Assert.Empty(ThemeGrouper.Group([Note("Too many meetings during the sprint"), Note("Code reviews went well this sprint")]));

    [Fact]
    public void Group_NothingShared_ReturnsNoThemes() =>
        Assert.Empty(ThemeGrouper.Group([Note("Standups run over"), Note("Pairing helped juniors")]));
}

public sealed class RetroOutcomeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Plan_ActionsBecomeTaggedTasksInTheNextSprintWithOwners()
    {
        MemberBrief[] members = [new("m1", "Daniel Park", [], "q"), new("m2", "Maya Lin", [], "q")];
        var brief = new PreBrief("t1", "p", "g", 3, new Zava.Scrum.Domain.Kpis.Burndown(10, 4, 6, 3, false), members, [], [], "s");
        var s = StandupSession.Prepare(Guid.NewGuid(), "t1", Now, brief, CeremonyKind.Retrospective);
        s.RecordEvent(StandupEventType.RetroItem, "m1", [], "Deployments take too long", Now, RetroCategory.Stop);
        s.RecordEvent(StandupEventType.ActionItem, "m1", [], "Automate the deployment checklist", Now, ownerMemberId: "m2");
        Member[] roster = [new("m1", "t1", "daniel", "Daniel Park", null, "Daniel", 1, true), new("m2", "t1", "maya", "Maya Lin", "maya@x.com", "Maya", 2, true)];

        var changes = RetroOutcomePlanner.Plan(RetroSummary.Build(s), roster, "P\\Sprint 43");

        var change = Assert.Single(changes);
        Assert.Equal(ChangeOp.CreateWorkItem, change.Op);
        Assert.Equal("Task", change.New!.Type);
        Assert.Equal(["retro-action"], change.New.Tags);
        Assert.Equal("P\\Sprint 43", change.New.IterationPath);
        Assert.Equal("maya@x.com", change.New.AssignedTo);
        Assert.Equal("maya", change.OwnerKey);
    }
}

public sealed class FloorAnnouncingTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 8, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AnnounceStep_FromAdvancing_AgentSpeaksThenAdvances()
    {
        var floor = new FloorControl(new FloorOptions(TimeSpan.FromMinutes(15)));
        floor.Handle(new StartFloor(Now));
        floor.Handle(new AgentSpeechEnded(Now));

        var speak = Assert.IsType<AgentSpeak>(Assert.Single(floor.Handle(new AnnounceStep(SpeakReason.RetroThemes))));
        Assert.Equal(SpeakReason.RetroThemes, speak.Reason);
        Assert.Null(speak.MemberId);
        Assert.True(floor.AgentHasFloor);
        Assert.Equal(FloorState.Announcing, floor.State);

        Assert.IsType<AdvanceSpeaker>(Assert.Single(floor.Handle(new AgentSpeechEnded(Now))));
        Assert.Equal(FloorState.Advancing, floor.State);
    }

    [Fact]
    public void AnnounceStep_WhenNotAdvancing_IsIgnored()
    {
        var floor = new FloorControl(new FloorOptions(TimeSpan.FromMinutes(15)));

        Assert.Empty(floor.Handle(new AnnounceStep(SpeakReason.RetroThemes)));
    }
}
