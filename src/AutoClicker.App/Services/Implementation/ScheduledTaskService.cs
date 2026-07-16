using System.Collections.ObjectModel;
using System.Windows;
using AutoClicker.App.Models;
using AutoClicker.App.Services.Interfaces;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 定时任务执行器 — 按顺序执行任务列表，每个任务等待指定延迟后在目标坐标点击
/// </summary>
public class ScheduledTaskService : IScheduledTaskService
{
    private readonly IMouseSimulationService _mouseSimulator;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private volatile bool _isRunning;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public ObservableCollection<ScheduledClickTask> Tasks { get; } = new();

    /// <inheritdoc />
    public event EventHandler<int>? TaskStarted;

    /// <inheritdoc />
    public event EventHandler<int>? TaskCompleted;

    /// <inheritdoc />
    public event EventHandler? AllCompleted;

    /// <inheritdoc />
    public event EventHandler? Stopped;

    public ScheduledTaskService(IMouseSimulationService mouseSimulator)
    {
        _mouseSimulator = mouseSimulator;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken ct)
    {
        if (_isRunning || Tasks.Count == 0) return;

        lock (_lock)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        }
        _isRunning = true;

        try
        {
            await Task.Run(async () =>
            {
                for (int i = 0; i < Tasks.Count; i++)
                {
                    CancellationToken token;
                    lock (_lock) { token = _cts?.Token ?? ct; }
                    token.ThrowIfCancellationRequested();

                    var task = Tasks[i];

                    // 通知任务开始
                    SafeInvoke(() => TaskStarted?.Invoke(this, i));

                    // 等待延迟
                    if (task.DelaySeconds > 0)
                    {
                        int remainingMs = task.DelaySeconds * 1000;
                        while (remainingMs > 0)
                        {
                            lock (_lock) { token = _cts?.Token ?? ct; }
                            if (token.IsCancellationRequested) break;
                            int chunk = Math.Min(remainingMs, 1000);
                            await Task.Delay(chunk, token);
                            remainingMs -= chunk;
                        }
                    }

                    lock (_lock) { token = _cts?.Token ?? ct; }
                    token.ThrowIfCancellationRequested();

                    // 执行点击
                    SafeInvoke(() =>
                    {
                        var (origX, origY) = _mouseSimulator.GetCurrentPosition();
                        _mouseSimulator.ClickAtWithoutMoving(
                            task.TargetX, task.TargetY, origX, origY, task.ClickMode);
                    });

                    // 通知完成
                    SafeInvoke(() => TaskCompleted?.Invoke(this, i));
                }

                SafeInvoke(() => AllCompleted?.Invoke(this, EventArgs.Empty));
            }, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            SafeInvoke(() => Stopped?.Invoke(this, EventArgs.Empty));
        }
        finally
        {
            lock (_lock)
            {
                _isRunning = false;
                _cts?.Dispose();
                _cts = null;
            }
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        CancellationTokenSource? cts;
        lock (_lock) { cts = _cts; }
        if (cts is not null && !cts.IsCancellationRequested)
        {
            try { cts.Cancel(); } catch (ObjectDisposedException) { }
        }
    }

    /// <summary>安全地调度到 UI 线程，Application.Current 为 null 时直接跳过</summary>
    private static void SafeInvoke(Action action)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.Invoke(action);
    }
}
