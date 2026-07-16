using System.Diagnostics;
using AutoClicker.App.Models;
using AutoClicker.App.Services.Interfaces;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 按录制的时间戳精确回放鼠标事件
/// 短间隔(&lt;15ms)使用 SpinWait，长间隔使用 Task.Delay
/// </summary>
public class RecordingPlayerService : IRecordingPlayerService
{
    private readonly IMouseSimulationService _mouseSimulator;
    private CancellationTokenSource? _cts;

    public bool IsPlaying { get; private set; }
    public event EventHandler? PlaybackCompleted;

    public RecordingPlayerService(IMouseSimulationService mouseSimulator)
    {
        _mouseSimulator = mouseSimulator;
    }

    public async Task PlayAsync(RecordingSession session, CancellationToken ct)
    {
        if (IsPlaying) return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        IsPlaying = true;

        try
        {
            await Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                foreach (var evt in session.Events)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    var remainMs = evt.TimestampMs - sw.Elapsed.TotalMilliseconds;
                    if (remainMs > 0)
                        await WaitAsync((int)remainMs, _cts.Token);

                    _cts.Token.ThrowIfCancellationRequested();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        ExecuteEvent(evt));
                }
            }, _cts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _cts?.Dispose();
            _cts = null;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsPlaying = false;
                PlaybackCompleted?.Invoke(this, EventArgs.Empty);
            });
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private void ExecuteEvent(RecordedMouseEvent evt)
    {
        switch (evt.EventType)
        {
            case MouseEventType.Move:
                _mouseSimulator.MoveTo(evt.X, evt.Y);
                break;
            case MouseEventType.LeftDown:
                _mouseSimulator.MoveTo(evt.X, evt.Y);
                _mouseSimulator.Down(ClickMode.Single);
                break;
            case MouseEventType.LeftUp:
                _mouseSimulator.Up(ClickMode.Single);
                break;
            case MouseEventType.RightDown:
                _mouseSimulator.MoveTo(evt.X, evt.Y);
                _mouseSimulator.Down(ClickMode.Right);
                break;
            case MouseEventType.RightUp:
                _mouseSimulator.Up(ClickMode.Right);
                break;
            case MouseEventType.MiddleDown:
                _mouseSimulator.MoveTo(evt.X, evt.Y);
                _mouseSimulator.Down(ClickMode.Middle);
                break;
            case MouseEventType.MiddleUp:
                _mouseSimulator.Up(ClickMode.Middle);
                break;
        }
    }

    private static async Task WaitAsync(int milliseconds, CancellationToken ct)
    {
        if (milliseconds < 15)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < milliseconds && !ct.IsCancellationRequested)
                Thread.SpinWait(100);
        }
        else
        {
            await Task.Delay(milliseconds, ct);
        }
    }
}
