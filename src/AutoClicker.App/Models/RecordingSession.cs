namespace AutoClicker.App.Models;

/// <summary>
/// 一次完整的录制会话，可序列化为 JSON
/// </summary>
public class RecordingSession
{
    public DateTime RecordedAt { get; set; }
    public List<RecordedMouseEvent> Events { get; set; } = new();
    public double TotalDurationMs { get; set; }
}
