namespace Zava.Scrum.Application.Abstractions.Ports;

/// <summary>An event from the realtime voice service.</summary>
public abstract record VoiceEvent;

/// <summary>A chunk of agent audio (PCM16, 24 kHz, mono).</summary>
/// <param name="Pcm">The audio.</param>
public sealed record AgentAudio(ReadOnlyMemory<byte> Pcm) : VoiceEvent;

/// <summary>The full transcript of what the agent just said.</summary>
/// <param name="Text">The text.</param>
public sealed record AgentTranscript(string Text) : VoiceEvent;

/// <summary>A response finished (spoken, cancelled or failed).</summary>
/// <param name="HadToolCalls">True when the model asked for tools, so a continuation response is needed.</param>
public sealed record ResponseDone(bool HadToolCalls) : VoiceEvent;

/// <summary>The service heard someone start talking.</summary>
public sealed record UserSpeechStarted : VoiceEvent;

/// <summary>The service heard them stop.</summary>
public sealed record UserSpeechStopped : VoiceEvent;

/// <summary>A transcribed utterance from the incoming audio.</summary>
/// <param name="Text">The text.</param>
public sealed record UserTranscript(string Text) : VoiceEvent;

/// <summary>The model wants a tool run.</summary>
/// <param name="CallId">Call id to answer.</param>
/// <param name="Name">Tool name.</param>
/// <param name="ArgumentsJson">Arguments as JSON.</param>
public sealed record ToolCallRequested(string CallId, string Name, string ArgumentsJson) : VoiceEvent;

/// <summary>The service reported an error or the connection dropped.</summary>
/// <param name="Message">Plain description for the portal.</param>
public sealed record VoiceFault(string Message) : VoiceEvent;
