namespace AutoClicker.App.Models;

/// <summary>
/// 录制的单个鼠标事件 — 包含事件类型、屏幕坐标和相对时间戳
/// </summary>
public record RecordedMouseEvent(
    MouseEventType EventType,
    int X,
    int Y,
    double TimestampMs
);
