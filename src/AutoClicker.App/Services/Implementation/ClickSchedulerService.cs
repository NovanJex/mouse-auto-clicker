using AutoClicker.App.Interop;
using AutoClicker.App.Services.Interfaces;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 点击调度器 — 在独立线程上循环执行点击任务
/// 启动时调用 timeBeginPeriod(1) 提升定时器精度
/// </summary>
public class ClickSchedulerService : IClickSchedulerService, IDisposable
{
    private CancellationTokenSource? _cts;
    private volatile bool _isRunning;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public async Task StartAsync(Func<CancellationToken, Task> clickCycle, CancellationToken cancellationToken)
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
                while (!_cts.Token.IsCancellationRequested)
                {
                    await clickCycle(_cts.Token);
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
