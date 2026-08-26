using AutoClicker.App.Models;

namespace AutoClicker.App.Services.Interfaces;

/// <summary>
/// 鼠标录制服务 — 通过 WH_MOUSE_LL 全局钩子捕获鼠标事件
/// </summary>
public interface IMouseRecordingService
{
    bool IsRecording { get; }
    event EventHandler<RecordedMouseEvent>? EventCaptured;

    /// <summary>录制停止事件（手动或达到上限自动停止），携带录制会话</summary>
    event EventHandler<RecordingSession?>? RecordingStopped;
    void StartRecording();
    RecordingSession? StopRecording();
}
