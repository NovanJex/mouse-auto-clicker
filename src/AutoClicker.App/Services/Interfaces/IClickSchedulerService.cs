namespace AutoClicker.App.Services.Interfaces;

/// <summary>
/// 点击调度器服务接口 — 后台循环执行点击任务，按固定间隔校正周期
/// </summary>
public interface IClickSchedulerService
{
    /// <summary>是否正在运行</summary>
    bool IsRunning { get; }

    /// <summary>启动后台点击循环</summary>
    /// <param name="clickCycle">每次点击周期的异步操作（只含点击逻辑，不含延迟）</param>
    /// <param name="intervalMs">目标周期间隔（毫秒），调度器负责保证每个周期总时长为该值</param>
    /// <param name="cancellationToken">外部取消令牌</param>
    Task StartAsync(Func<CancellationToken, Task> clickCycle, int intervalMs, CancellationToken cancellationToken);

    /// <summary>停止点击循环</summary>
    void Stop();
}
