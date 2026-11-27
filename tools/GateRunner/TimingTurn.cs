namespace Zava.Scrum.Tools.GateRunner;

/// <summary>One agent reply that followed someone speaking, split by hop. All values are milliseconds.</summary>
/// <param name="Reason">Why the agent spoke (for example Clarify, Addressed, CallOn).</param>
/// <param name="TotalMs">From the end of the user's utterance to the first audio of the reply.</param>
/// <param name="SpeechEndToTranscriptMs">From the voice service hearing speech stop to the transcript arriving.</param>
/// <param name="AnalyserMs">The analyser excluding the decision model call (rules, grounding, mediator).</param>
/// <param name="DecisionModelMs">The decision model call; 0 when it was not asked.</param>
/// <param name="OrchestrationMs">Floor control, the wait for silence and the reconcile, up to the response request.</param>
/// <param name="VoiceFirstAudioMs">From asking Voice Live to speak to the first audio.</param>
/// <param name="Source">Whether the decision model or the rules read the utterance.</param>
/// <param name="TraceId">Persisted session trace id exported to Insights.</param>
/// <param name="SpanId">The corresponding reactive-turn span.</param>
internal sealed record TimingTurn(
    string Reason,
    int TotalMs,
    int SpeechEndToTranscriptMs,
    int AnalyserMs,
    int DecisionModelMs,
    int OrchestrationMs,
    int VoiceFirstAudioMs,
    string Source,
    string? TraceId = null,
    string? SpanId = null);
