using Zava.Scrum.Domain.Voice;

namespace Zava.Scrum.Domain.Tests.Voice;

public sealed class FloorControlTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 8, 9, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset At(double seconds) => T0.AddMilliseconds(Math.Round(seconds * 1000));

    private static FloorControl Floor(double minutes = 15, int parkMs = 10_000, int maxClarify = 1) =>
        new(new FloorOptions(TimeSpan.FromMinutes(minutes), ParkAfterOffTopicMs: parkMs, MaxClarifications: maxClarify));

    /// <summary>Opens, greets, and gives the floor to "m1" so tests start in Listening.</summary>
    private static FloorControl Listening(double minutes = 15, int parkMs = 10_000, int maxClarify = 1)
    {
        var f = Floor(minutes, parkMs, maxClarify);
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(5)));
        f.Handle(new SpeakerSelected("m1", false));
        f.Handle(new AgentSpeechEnded(At(8)));
        return f;
    }

    [Fact]
    public void Start_FromIdle_OpensAndSpeaks()
    {
        var f = Floor();

        var actions = f.Handle(new StartFloor(T0));

        Assert.Equal(FloorState.Opening, f.State);
        Assert.Equal(new AgentSpeak(SpeakReason.Open, null), Assert.Single(actions));
    }

    [Fact]
    public void Start_Twice_IsIgnored()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));

        Assert.Empty(f.Handle(new StartFloor(T0)));
    }

    [Fact]
    public void AgentSpeechEnded_AfterOpening_AsksForNextSpeaker()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));

        var actions = f.Handle(new AgentSpeechEnded(At(5)));

        Assert.Equal(new AdvanceSpeaker(), Assert.Single(actions));
        Assert.Equal(FloorState.Advancing, f.State);
    }

    [Fact]
    public void SpeakerSelected_Present_CallsOnThenListensAfterSpeechEnds()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(5)));

        var call = f.Handle(new SpeakerSelected("m1", false));
        var after = f.Handle(new AgentSpeechEnded(At(8)));

        Assert.Equal(new AgentSpeak(SpeakReason.CallOn, "m1"), Assert.Single(call));
        Assert.Empty(after);
        Assert.Equal((FloorState.Listening, "m1"), (f.State, f.CurrentMemberId));
    }

    [Fact]
    public void SpeakerSelected_Absent_AnnouncesThenAdvancesWithoutListening()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(5)));

        var call = f.Handle(new SpeakerSelected("m5", true));
        var after = f.Handle(new AgentSpeechEnded(At(8)));

        Assert.Equal(new AgentSpeak(SpeakReason.Absent, "m5"), Assert.Single(call));
        Assert.Equal(new AdvanceSpeaker(), Assert.Single(after));
    }

    [Fact]
    public void SpeakerSelected_NobodyLeft_GoesToRecapThenClosesAfterRecap()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(5)));

        var recap = f.Handle(new SpeakerSelected(null, false));
        var end = f.Handle(new AgentSpeechEnded(At(30)));

        Assert.Equal(new AgentSpeak(SpeakReason.Recap, null), Assert.Single(recap));
        Assert.Equal(new EndSession(), Assert.Single(end));
        Assert.Equal(FloorState.Closed, f.State);
    }

    [Theory]
    [InlineData(1.0, false)]
    [InlineData(1.19, false)]
    [InlineData(1.2, true)]
    [InlineData(3.0, true)]
    public void Tick_AfterHolderSpoke_ReconcilesOnlyAfterSilenceThreshold(double silenceSeconds, bool expectReconcile)
    {
        var f = Listening();
        f.Handle(new MemberSpoke("m1", 4000, At(14)));

        var actions = f.Handle(new Tick(At(14 + silenceSeconds)));

        Assert.Equal(expectReconcile, actions.OfType<RunReconcile>().Any());
        Assert.Equal(expectReconcile ? FloorState.Reconciling : FloorState.Listening, f.State);
    }

    [Fact]
    public void Tick_NobodySpokeWithinNoResponseWindow_SkipsToNextSpeaker()
    {
        var f = Listening();

        Assert.Empty(f.Handle(new Tick(At(8 + 24))));
        var actions = f.Handle(new Tick(At(8 + 26)));

        Assert.Equal(new DeferSpeaker("m1", DeferReason.NoResponse), actions[0]);
        Assert.Equal(new AgentSpeak(SpeakReason.NoResponse, "m1"), actions[1]);
        Assert.Equal(new AdvanceSpeaker(), Assert.Single(f.Handle(new AgentSpeechEnded(At(8 + 30)))));
    }

    [Fact]
    public void Tick_SilenceAfterTheMemberAlreadySpoke_AdvancesWithoutCallingItNoResponse()
    {
        var f = Listening();
        f.Handle(new MemberSpoke("m1", 3000, At(12)));
        var reconcile = f.Handle(new Tick(At(14))).OfType<RunReconcile>().Single();
        f.Handle(new ReconcileFinished(reconcile.Version, true));
        f.Handle(new AgentSpeechEnded(At(18)));

        var actions = f.Handle(new Tick(At(18 + 26)));

        Assert.Equal(new AdvanceSpeaker(), Assert.Single(actions));
    }

    [Fact]
    public void DeferCurrent_SpeakerStepsAway_SpeaksThenAdvances()
    {
        var f = Listening();

        var actions = f.Handle(new DeferCurrent(DeferReason.StepAway));
        var after = f.Handle(new AgentSpeechEnded(At(12)));

        Assert.Equal(new DeferSpeaker("m1", DeferReason.StepAway), actions[0]);
        Assert.Equal(new AgentSpeak(SpeakReason.StepAway, "m1"), actions[1]);
        Assert.Equal(new AdvanceSpeaker(), Assert.Single(after));
    }

    [Fact]
    public void SpeakerSelected_CircleBack_UsesTheCircleBackReason()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(5)));

        var actions = f.Handle(new SpeakerSelected("m1", false, CircleBack: true));

        Assert.Equal(new AgentSpeak(SpeakReason.CircleBack, "m1"), Assert.Single(actions));
    }

    [Fact]
    public void ReconcileFinished_WithQuestion_AsksOnceThenAdvancesOnSecondRound()
    {
        var f = Listening();
        f.Handle(new MemberSpoke("m1", 3000, At(12)));
        var reconcile = f.Handle(new Tick(At(14))).OfType<RunReconcile>().Single();

        var ask = f.Handle(new ReconcileFinished(reconcile.Version, true));
        f.Handle(new AgentSpeechEnded(At(18)));
        f.Handle(new MemberSpoke("m1", 2000, At(21)));
        var second = f.Handle(new Tick(At(23)));

        Assert.Equal(new AgentSpeak(SpeakReason.Clarify, "m1"), Assert.Single(ask));
        Assert.Equal(new RunReconcile("m1", ((RunReconcile)second[0]).Version, Background: true), second[0]);
        Assert.Equal(new AdvanceSpeaker(), second[1]);
    }

    [Fact]
    public void ReconcileFinished_MoreFlaggedItems_AsksAgainUpToTheLimit()
    {
        var f = Listening(maxClarify: 2);
        f.Handle(new MemberSpoke("m1", 3000, At(12)));
        var first = f.Handle(new Tick(At(14))).OfType<RunReconcile>().Single();
        f.Handle(new ReconcileFinished(first.Version, true));
        f.Handle(new AgentSpeechEnded(At(18)));
        f.Handle(new MemberSpoke("m1", 2000, At(21)));
        var second = f.Handle(new Tick(At(23))).OfType<RunReconcile>().Single();

        var ask = f.Handle(new ReconcileFinished(second.Version, true));

        Assert.Equal(new AgentSpeak(SpeakReason.Clarify, "m1"), Assert.Single(ask));
    }

    [Fact]
    public void SpeechActive_TeammateStartsWhileReconciling_DropsTheStaleQuestion()
    {
        var f = Listening();
        f.Handle(new MemberSpoke("m1", 3000, At(12)));
        var reconcile = f.Handle(new Tick(At(14))).OfType<RunReconcile>().Single();
        f.Handle(new SpeechActive("m2", true, At(14)));

        var late = f.Handle(new ReconcileFinished(reconcile.Version, true));

        Assert.Empty(late);
        Assert.Equal(FloorState.Listening, f.State);
    }

    [Fact]
    public void Tick_MemberCannotBeAskedAQuestion_AdvancesAtOnceWithBackgroundReconcile()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(5)));
        f.Handle(new SpeakerSelected("m1", false, CanClarify: false));
        f.Handle(new AgentSpeechEnded(At(8)));
        f.Handle(new MemberSpoke("m1", 3000, At(12)));

        var actions = f.Handle(new Tick(At(13.3)));

        Assert.IsType<RunReconcile>(actions[0]);
        Assert.True(((RunReconcile)actions[0]).Background);
        Assert.Equal(new AdvanceSpeaker(), actions[1]);
    }

    [Fact]
    public void Tick_SideDiscussionInProgress_HoldsBackTheHoldersReconcile()
    {
        var f = Listening();
        f.Handle(new MemberSpoke("m1", 3000, At(12)));
        f.Handle(new SpeechActive("m2", true, At(12.5)));

        Assert.Empty(f.Handle(new Tick(At(20))));
        f.Handle(new SpeechActive("m2", false, At(21)));

        Assert.Contains(f.Handle(new Tick(At(22.3))), a => a is RunReconcile);
    }

    [Fact]
    public void ReconcileFinished_NoQuestion_Advances()
    {
        var f = Listening();
        f.Handle(new MemberSpoke("m1", 3000, At(12)));
        var reconcile = f.Handle(new Tick(At(14))).OfType<RunReconcile>().Single();

        var actions = f.Handle(new ReconcileFinished(reconcile.Version, false));

        Assert.Equal(new AdvanceSpeaker(), Assert.Single(actions));
    }

    [Fact]
    public void ReconcileFinished_HolderKeepsTalking_StaleResultIsIgnored()
    {
        var f = Listening();
        f.Handle(new MemberSpoke("m1", 3000, At(12)));
        var reconcile = f.Handle(new Tick(At(14))).OfType<RunReconcile>().Single();
        f.Handle(new MemberSpoke("m1", 2000, At(15)));

        var actions = f.Handle(new ReconcileFinished(reconcile.Version, true));

        Assert.Empty(actions);
        Assert.Equal(FloorState.Listening, f.State);
    }

    [Fact]
    public void Tick_OffTopicTalkReachesThreshold_ParksAtNextPauseAndResumesListening()
    {
        var f = Listening(parkMs: 10_000);
        f.Handle(new MemberSpoke("m1", 3000, At(12)));
        f.Handle(new MemberSpoke("m2", 6000, At(18)));
        f.Handle(new MemberSpoke("m3", 5000, At(23)));

        var early = f.Handle(new Tick(At(23.3)));
        var actions = f.Handle(new Tick(At(24)));
        var resume = f.Handle(new AgentSpeechEnded(At(27)));

        Assert.Empty(early);
        Assert.IsType<ParkTopic>(actions[0]);
        Assert.Equal(new AgentSpeak(SpeakReason.Park, "m1"), actions[1]);
        Assert.Empty(resume);
        Assert.Equal(FloorState.Listening, f.State);
    }

    [Fact]
    public void Tick_OffTopicTalkBelowThreshold_DoesNotPark()
    {
        var f = Listening(parkMs: 10_000);
        f.Handle(new MemberSpoke("m2", 4000, At(12)));

        Assert.DoesNotContain(f.Handle(new Tick(At(20))), a => a is ParkTopic);
    }

    [Fact]
    public void Tick_At80PercentOfTimebox_WarnsAtNextPause()
    {
        var f = Listening(minutes: 5);
        f.Handle(new MemberSpoke("m1", 3000, At(12)));

        f.Handle(new MemberSpoke("m1", 2000, At(239.9)));
        var beforePause = f.Handle(new Tick(At(240)));
        var atPause = f.Handle(new Tick(At(242.5)));

        Assert.DoesNotContain(beforePause, a => a is AgentSpeak);
        Assert.Contains(atPause, a => a is AgentSpeak { Reason: SpeakReason.TimeWarning });
    }

    [Fact]
    public void Tick_TimeboxElapsedWhileListening_MovesToRecap()
    {
        var f = Listening(minutes: 5);

        var actions = f.Handle(new Tick(At(301)));

        Assert.Equal(new AgentSpeak(SpeakReason.Recap, null), Assert.Single(actions));
        Assert.Equal(FloorState.Recap, f.State);
    }

    [Fact]
    public void Tick_TimeboxElapsedWhileAgentSpeaking_RecapStartsWhenItFinishes()
    {
        var f = Floor(minutes: 5);
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(2)));
        f.Handle(new SpeakerSelected("m1", false));

        Assert.Empty(f.Handle(new Tick(At(301))));
        var actions = f.Handle(new AgentSpeechEnded(At(305)));

        Assert.Equal(new AgentSpeak(SpeakReason.Recap, null), Assert.Single(actions));
    }

    [Fact]
    public void HumanOverride_WhileAgentSpeaking_CancelsAndNeverSpeaksUntilReleased()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(2)));
        f.Handle(new SpeakerSelected("m1", false));

        var cancel = f.Handle(new HumanOverride(true));
        var silent = f.Handle(new Tick(At(60)));
        var release = f.Handle(new HumanOverride(false));

        Assert.Equal(new CancelResponse(), Assert.Single(cancel));
        Assert.Empty(silent);
        Assert.Empty(release);
        Assert.Equal(FloorState.Listening, f.State);
    }

    [Fact]
    public void Pause_WhileListening_BlocksReconcileUntilResumed()
    {
        var f = Listening();
        f.Handle(new MemberSpoke("m1", 3000, At(12)));
        f.Handle(new PauseFloor(true));

        Assert.Empty(f.Handle(new Tick(At(20))));
        f.Handle(new PauseFloor(false));

        Assert.Equal(FloorState.Listening, f.State);
    }

    [Fact]
    public void Addressed_WhileListening_Interjects_ButNotWhilePaused()
    {
        var f = Listening();
        var spoke = f.Handle(new AgentAddressed());
        f.Handle(new AgentSpeechEnded(At(12)));
        f.Handle(new PauseFloor(true));

        Assert.Equal(new AgentSpeak(SpeakReason.Addressed, "m1"), Assert.Single(spoke));
        Assert.Empty(f.Handle(new AgentAddressed()));
    }

    [Fact]
    public void BargeIn_HolderTalksOverCallOn_CancelsAndListens()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(2)));
        f.Handle(new SpeakerSelected("m1", false));

        var actions = f.Handle(new BargeIn("m1", false));

        Assert.Equal(new CancelResponse(), Assert.Single(actions));
        Assert.Equal(FloorState.Listening, f.State);
    }

    [Fact]
    public void BargeIn_SomeoneElseTalksOverAgent_IsIgnored()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(2)));
        f.Handle(new SpeakerSelected("m1", false));

        Assert.Empty(f.Handle(new BargeIn("m2", false)));
    }

    [Fact]
    public void BargeIn_SomeoneElseKeepsTalkingOverAgent_CancelsAndListens()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));
        f.Handle(new AgentSpeechEnded(At(2)));
        f.Handle(new SpeakerSelected("m1", false));

        var actions = f.Handle(new BargeIn("m2", false, Sustained: true));

        Assert.Equal(new CancelResponse(), Assert.Single(actions));
        Assert.Equal(FloorState.Listening, f.State);
    }

    [Fact]
    public void BargeIn_SustainedWhileOpening_IsStillIgnored()
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));

        Assert.Empty(f.Handle(new BargeIn("m2", false, Sustained: true)));
        Assert.Equal(FloorState.Opening, f.State);
    }

    [Fact]
    public void Tick_HolderStillSpeaking_NeverReconcilesUntilSpeechStops()
    {
        var f = Listening();
        f.Handle(new SpeechActive("m1", true, At(10)));

        Assert.Empty(f.Handle(new Tick(At(20))));
        f.Handle(new SpeechActive("m1", false, At(21)));

        Assert.Empty(f.Handle(new Tick(At(21.5))));
        Assert.Contains(f.Handle(new Tick(At(22.3))), a => a is RunReconcile);
    }

    [Fact]
    public void SpeechActive_HolderResumesWhileReconciling_CancelsThatReconcile()
    {
        var f = Listening();
        f.Handle(new SpeechActive("m1", true, At(10)));
        f.Handle(new SpeechActive("m1", false, At(12)));
        var reconcile = f.Handle(new Tick(At(13.3))).OfType<RunReconcile>().Single();

        f.Handle(new SpeechActive("m1", true, At(13.6)));

        Assert.Empty(f.Handle(new ReconcileFinished(reconcile.Version, true)));
        Assert.Equal(FloorState.Listening, f.State);
    }

    [Fact]
    public void Skip_WhileListening_AdvancesImmediately()
    {
        var f = Listening();

        Assert.Equal(new AdvanceSpeaker(), Assert.Single(f.Handle(new SkipSpeaker())));
    }

    [Theory]
    [InlineData(FloorState.Opening, SpeakReason.Open)]
    [InlineData(FloorState.Recap, SpeakReason.Recap)]
    [InlineData(FloorState.Announcing, SpeakReason.RetroThemes)]
    public void WholeTeamStep_InterruptedByHuman_ReplaysOnReleaseWithoutSkipping(FloorState state, SpeakReason reason)
    {
        var f = WholeTeamStep(state);

        Assert.Empty(f.Handle(new BargeIn("guest", false, Sustained: true)));
        Assert.Equal(new CancelResponse(), Assert.Single(f.Handle(new BargeIn("human", true))));
        Assert.Empty(f.Handle(new AgentSpeechEnded(At(10))));
        Assert.Equal(new AgentSpeak(reason, null), Assert.Single(f.Handle(new HumanOverride(false))));
        Assert.Equal(state, f.State);
        Assert.IsType(state == FloorState.Recap ? typeof(EndSession) : typeof(AdvanceSpeaker),
            Assert.Single(f.Handle(new AgentSpeechEnded(At(12)))));
    }

    [Theory]
    [InlineData(FloorState.Opening, SpeakReason.Open)]
    [InlineData(FloorState.Recap, SpeakReason.Recap)]
    [InlineData(FloorState.Announcing, SpeakReason.RetroThemes)]
    public void WholeTeamStep_Paused_ReplaysOriginalStepOnResume(FloorState state, SpeakReason reason)
    {
        var f = WholeTeamStep(state);

        Assert.Equal(new CancelResponse(), Assert.Single(f.Handle(new PauseFloor(true))));
        Assert.Equal(new AgentSpeak(reason, null), Assert.Single(f.Handle(new PauseFloor(false))));
        Assert.Equal(state, f.State);
    }

    [Fact]
    public void SelectAudioSource_LouderGuestAndTeammate_PreservesAudibleHolder()
    {
        var f = Listening();
        FloorAudioSource[] sources = [new("holder", "m1", 400), new("other", "m2", 900), new("guest", null, 2000)];

        Assert.Equal(sources[0], f.SelectAudioSource(sources));
        Assert.Equal(sources[1], f.SelectAudioSource(sources[1..]));
        Assert.Null(f.SelectAudioSource(sources[2..]));
        Assert.Null(f.SelectAudioSource([]));
    }

    [Fact]
    public void SelectAudioSource_SilentHolder_DoesNotSuppressAudibleTeammate()
    {
        var f = Listening();
        FloorAudioSource[] sources = [new("holder", "m1", 10), new("other", "m2", 900)];

        Assert.Equal(sources[1], f.SelectAudioSource(sources));
    }

    private static FloorControl WholeTeamStep(FloorState state)
    {
        var f = Floor();
        f.Handle(new StartFloor(T0));
        if (state == FloorState.Opening) { return f; }
        f.Handle(new AgentSpeechEnded(At(2)));
        if (state == FloorState.Recap) { f.Handle(new SpeakerSelected(null, false)); }
        else { f.Handle(new AnnounceStep(SpeakReason.RetroThemes)); }
        return f;
    }
}
