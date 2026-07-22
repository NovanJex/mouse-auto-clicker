using System.Diagnostics;
using AutoClicker.App.Interop;
using AutoClicker.App.Services.Interfaces;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 点击调度器 — 在独立线程上循环执行点击任务，按固定间隔校正每个周期
/// 启动时调用 timeBeginPeriod(1) 提升定时器精度
/// </summary>
public class ClickSchedulerService : IClickSchedulerService, IDisposable
{
    private CancellationTokenSource? _cts;
    private volatile bool _isRunning;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public async Task StartAsync(Func<CancellationToken, Task> clickCycle, int intervalMs, CancellationToken cancellationToken)
    {
        if (_isRunning) return;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isRunning = true;

        // 提升系统定时器精度到 1ms
        NativeMethods.TimeBeginPeriod(Win32Constants.TIME_PERIOD);

        try
        {
            await Task.Run(async () =>
            {
                // 预计算 tick 每毫秒数，避免循环内重复计算
                double ticksPerMs = Stopwatch.Frequency / 1000.0;

                while (!_cts.Token.IsCancellationRequested)
                {
                    // 记录周期起始时点
                    long cycleStart = Stopwatch.GetTimestamp();

                    // 执行点击操作
                    await clickCycle(_cts.Token);

                    // 计算本周期已用时间，等待剩余时长确保间隔精确
                    long elapsedTicks = Stopwatch.GetTimestamp() - cycleStart;
                    int elapsedMs = (int)(elapsedTicks / ticksPerMs);
                    int remainingMs = intervalMs - elapsedMs;

                    if (remainingMs > 0)
                    {
                        if (remainingMs < 15)
                        {
                            // 短延迟：用绝对时点自旋，不受 CPU 频率波动影响
                            long targetTicks = Stopwatch.GetTimestamp() + remainingMs * (long)ticksPerMs;
                            while (Stopwatch.GetTimestamp() < targetTicks && !_cts.Token.IsCancellationRequested)
                            {
                                long remaining = targetTicks - Stopwatch.GetTimestamp();
                                if (remaining > 2 * (long)ticksPerMs)
                                    Thread.Sleep(0);       // 剩余 >2ms 让出 CPU 时间片
                                else
                                    Thread.SpinWait(50);    // 最后 2ms 内自旋
                            }
                        }
                        else
                        {
                            await Task.Delay(remainingMs, _cts.Token);
                        }
                    }
                }
            }, _cts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isRunning = false;
            NativeMethods.TimeEndPeriod(Win32Constants.TIME_PERIOD);
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        _cts?.Cancel();
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
