using System.Text.Json.Serialization;

namespace AutoClicker.App.Models;

/// <summary>
/// 应用配置 — 可序列化为 JSON 持久化存储
/// </summary>
public class AppSettings
{
    public ClickMode ClickMode { get; set; } = ClickMode.Single;
    public ClickIntervalMode IntervalMode { get; set; } = ClickIntervalMode.Ms;
    public int IntervalMs { get; set; } = 100;
    public int Cps { get; set; } = 10;
    public int HoldDurationMs { get; set; } = 500;

    public bool UseFixedPosition { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }

    /// <summary>关闭行为：true=最小化到托盘，false=退出应用</summary>
    public bool CloseToTray { get; set; }

    /// <summary>是否记住关闭选择，不再弹出提示</summary>
    public bool RememberCloseChoice { get; set; }

    /// <summary>循环回放次数，默认 1</summary>
    public int RepeatCount { get; set; } = 1;

    /// <summary>是否启用键盘触发点击</summary>
    public bool KeyboardTriggerEnabled { get; set; }

    /// <summary>触发按键的虚拟键码（Win32 VK_*），默认 VK_SPACE (0x20)</summary>
    /// <remarks>v1.3.0 起已弃用，由 TriggerBindings 列表替代，保留此字段仅用于旧版数据迁移</remarks>
    public int TriggerVkCode { get; set; }

    /// <summary>快捷键触发绑定列表（多键 → 多坐标）</summary>
    public List<TriggerBinding> TriggerBindings { get; set; } = new();

    /// <summary>定时点击任务列表</summary>
    public List<ScheduledClickTask> ScheduledTasks { get; set; } = new();
}
