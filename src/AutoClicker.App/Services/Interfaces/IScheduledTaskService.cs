using System.Collections.ObjectModel;
using AutoClicker.App.Models;

namespace AutoClicker.App.Services.Interfaces;

/// <summary>
/// 定时任务执行服务 — 按顺序执行多个定时点击任务
/// </summary>
public interface IScheduledTaskService
{
    /// <summary>是否正在执行任务</summary>
    bool IsRunning { get; }

    /// <summary>当前任务列表（自动通知 UI 变更）</summary>
    ObservableCollection<ScheduledClickTask> Tasks { get; }

    /// <summary>任务开始执行时触发，参数为任务索引</summary>
    event EventHandler<int>? TaskStarted;

    /// <summary>单个任务点击完成后触发，参数为任务索引</summary>
    event EventHandler<int>? TaskCompleted;

    /// <summary>所有任务执行完成</summary>
    event EventHandler? AllCompleted;

    /// <summary>任务被手动停止</summary>
    event EventHandler? Stopped;

    /// <summary>启动顺序执行所有任务</summary>
    Task StartAsync(CancellationToken ct);

    /// <summary>停止任务执行</summary>
    void Stop();
}
