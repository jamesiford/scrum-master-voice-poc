using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Graph.Communications.Core.Exceptions;
using Microsoft.Skype.Bots.Media;
using Zava.Scrum.MediaBot.Board;

namespace Zava.Scrum.MediaBot;

/// <summary>Draws only changed boards and resends a cached frame at a low keep-alive rate, off the audio path.</summary>
public sealed class BoardShareSession(
    IBoardShareTransport transport,
    TimeProvider time,
    ILogger logger,
    string teamId,
    Guid sessionId,
    Action<string, string?> status) : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly BoardRenderer _renderer = new();
    private readonly TaskCompletionSource _completionFrame = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _run;
    private JsonElement _pending;
    private bool _established;
    private bool _wanted;
    private bool _dirty;
    private bool _reshare;

    /// <summary>Starts the independent board worker once the Graph call is attached.</summary>
    public void Start() => _run = RunAsync();

    /// <summary>Tracks call reconnects without allowing them to end the voice worker.</summary>
    /// <param name="established">Whether the call is established and out of the lobby.</param>
    public void SetEstablished(bool established)
    {
        lock (_gate)
        {
            if (established && !_established) { _reshare = true; }
            _established = established;
        }
    }

    /// <summary>Replaces the bounded latest snapshot and retains an opened board until the call owner closes it.</summary>
    /// <param name="board">Authenticated team/session board, never persisted.</param>
    public void Update(JsonElement board)
    {
        lock (_gate)
        {
            _pending = board.Clone();
            _dirty = true;
            // The call owner closes sharing after the recap has actually been submitted, not before it renders.
            var wanted = board.GetProperty("opened").GetBoolean();
            if (_wanted != wanted) { _reshare = true; }
            _wanted = wanted;
        }
    }

    /// <summary>Requests role renegotiation after the API WebSocket reconnects.</summary>
    public void Reconnect()
    {
        lock (_gate) { _reshare = true; _dirty = true; }
    }

    /// <summary>Submits the received completion board before Graph deletes the call, then leaves it visible briefly.</summary>
    /// <param name="ct">The call owner's cancellation token.</param>
    /// <returns>A task; submission timeout is reported explicitly without preventing call cleanup.</returns>
    public async Task PresentCompletionAsync(CancellationToken ct)
    {
        lock (_gate)
        {
            if (_pending.ValueKind != JsonValueKind.Object || !_pending.GetProperty("ended").GetBoolean()) { return; }
        }

        try
        {
            await _completionFrame.Task.WaitAsync(TimeSpan.FromSeconds(2), ct);
            await Task.Delay(TimeSpan.FromSeconds(2), time, ct);
        }
        catch (TimeoutException ex)
        {
            logger.LogError(ex, "Native recap frame was not submitted for {TeamId}/{SessionId}", teamId, sessionId);
            status("Failed", "Native recap sharing timed out. The call will still close.");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        if (_run is not null) { await _run; }
        _renderer.Dispose();
        lock (_gate) { _pending = default; }
        _stop.Dispose();
    }

    private async Task RunAsync()
    {
        var sharing = false;
        var blocked = false;
        string? lastStatus = null;
        byte[]? frame = null;
        var completion = false;
        var nextFrame = DateTimeOffset.MinValue;
        void Report(string state, string? message = null)
        {
            if (state == lastStatus) { return; }
            lastStatus = state;
            status(state, message);
        }

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100), time);
            while (await timer.WaitForNextTickAsync(_stop.Token))
            {
                JsonElement board;
                bool wanted, dirty, reshare;
                lock (_gate)
                {
                    wanted = _wanted && _established;
                    dirty = _dirty;
                    reshare = _reshare;
                    board = _pending;
                    _dirty = false;
                    _reshare = false;
                }

                if (blocked && !reshare) { continue; }

                try
                {
                    if ((!wanted || reshare) && (sharing || blocked))
                    {
                        await transport.StopAsync(_stop.Token);
                        sharing = false;
                        Report("Stopped");
                    }

                    if (wanted && !sharing)
                    {
                        await transport.StartAsync(_stop.Token);
                        sharing = true;
                        Report("Negotiating");
                        nextFrame = DateTimeOffset.MinValue;
                    }

                    if (dirty)
                    {
                        var began = Stopwatch.GetTimestamp();
                        frame = _renderer.RenderRgb24(board, time.GetUtcNow().ToString("HH:mm:ss", CultureInfo.InvariantCulture));
                        completion = board.GetProperty("ended").GetBoolean();
                        nextFrame = DateTimeOffset.MinValue;
                        logger.LogInformation("Board rendered for {TeamId}/{SessionId} in {LatencyMs} ms",
                            teamId, sessionId, Stopwatch.GetElapsedTime(began).TotalMilliseconds);
                    }

                    if (sharing && frame is not null && time.GetUtcNow() >= nextFrame)
                    {
                        if (transport.TrySend(frame))
                        {
                            if (completion) { _completionFrame.TrySetResult(); }
                            Report("Sharing");
                            nextFrame = time.GetUtcNow().AddSeconds(1);
                        }
                        else { Report("Negotiating"); }
                    }

                    blocked = false;
                }
                catch (Exception ex) when (!_stop.IsCancellationRequested
                    && ex is ServiceException or SkypeMediaException or HttpRequestException
                        or InvalidOperationException or ArgumentException or OperationCanceledException)
                {
                    // Wait for reconnect or ceremony close, rather than retrying a failed role every 100 ms.
                    blocked = true;
                    logger.LogError(ex, "Native board sharing failed for {TeamId}/{SessionId}; voice remains independent", teamId, sessionId);
                    Report("Failed", "Native board sharing failed. Voice remains connected; inspect the media host diagnostics.");
                }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        finally
        {
            if (sharing)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try { await transport.StopAsync(timeout.Token); }
                catch (Exception ex) when (ex is ServiceException or SkypeMediaException or HttpRequestException
                    or InvalidOperationException or ArgumentException or OperationCanceledException)
                {
                    logger.LogError(ex, "Native board stop failed for {TeamId}/{SessionId}", teamId, sessionId);
                }
            }

            frame = null;
        }
    }
}
