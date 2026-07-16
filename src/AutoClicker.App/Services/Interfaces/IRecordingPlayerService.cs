using AutoClicker.App.Models;

namespace AutoClicker.App.Services.Interfaces;

/// <summary>
/// 录制回放服务 — 按时间戳精确回放录制的鼠标事件
/// </summary>
public interface IRecordingPlayerService
{
    bool IsPlaying { get; }
    event EventHandler? PlaybackCompleted;
    Task PlayAsync(RecordingSession session, CancellationToken ct);
    void Stop();
}
