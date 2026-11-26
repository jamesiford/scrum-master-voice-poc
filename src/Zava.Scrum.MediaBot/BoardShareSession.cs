using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Graph.Communications.Core.Exceptions;
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

    /// <summary>Replaces the bounded latest snapshot and requests sharing only while the ceremony is open.</summary>
    /// <param name="board">Authenticated team/session board, never persisted.</param>
    public void Update(JsonElement board)
    {
        lock (_gate)
        {
            _pending = board.Clone();
            _dirty = true;
            _wanted = board.GetProperty("opened").GetBoolean() && !board.GetProperty("ended").GetBoolean();
        }
    }

    /// <summary>Requests role renegotiation after the API WebSocket reconnects.</summary>
    public void Reconnect()
    {
        lock (_gate) { _reshare = true; _dirty = true; }
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
        byte[]? frame = null;
        var nextFrame = DateTimeOffset.MinValue;
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

                if ((!wanted || reshare) && sharing)
                {
                    await transport.StopAsync(_stop.Token);
                    sharing = false;
                    status("Stopped", null);
                }

                if (wanted && !sharing)
                {
                    await transport.StartAsync(_stop.Token);
                    sharing = true;
                    status("Negotiating", null);
                    nextFrame = DateTimeOffset.MinValue;
                }

                if (dirty)
                {
                    var began = Stopwatch.GetTimestamp();
                    frame = _renderer.RenderRgb24(board, time.GetUtcNow().ToString("HH:mm:ss", CultureInfo.InvariantCulture));
                    nextFrame = DateTimeOffset.MinValue;
                    logger.LogInformation("Board rendered for {TeamId}/{SessionId} in {LatencyMs} ms",
                        teamId, sessionId, Stopwatch.GetElapsedTime(began).TotalMilliseconds);
                }

                if (sharing && transport.Active && frame is not null && time.GetUtcNow() >= nextFrame)
                {
                    transport.Send(frame);
                    status("Sharing", null);
                    nextFrame = time.GetUtcNow().AddSeconds(1);
                }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (Exception ex) when (ex is ServiceException or HttpRequestException or InvalidOperationException or ArgumentException)
        {
            logger.LogError(ex, "Native board sharing failed for {TeamId}/{SessionId}; voice remains independent", teamId, sessionId);
            status("Failed", "Native board sharing failed. Voice remains connected; inspect the media host diagnostics.");
        }
        finally
        {
            if (sharing)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try { await transport.StopAsync(timeout.Token); }
                catch (Exception ex) when (ex is ServiceException or HttpRequestException or OperationCanceledException)
                {
                    logger.LogError(ex, "Native board stop failed for {TeamId}/{SessionId}", teamId, sessionId);
                }
            }

            frame = null;
        }
    }
}
