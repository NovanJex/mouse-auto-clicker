using System.Text.Json.Serialization;
using AutoClicker.App.Models;

namespace AutoClicker.App.Models;

/// <summary>
/// 定时点击任务 — 指定坐标、延迟时间和点击模式
/// </summary>
public class ScheduledClickTask
{
    /// <summary>任务标签（可选，如"打开QQ"）</summary>
    public string Label { get; set; } = "";

    /// <summary>目标 X 坐标</summary>
    public int TargetX { get; set; }

    /// <summary>目标 Y 坐标</summary>
    public int TargetY { get; set; }

    /// <summary>相对于上一个任务完成后的延迟秒数</summary>
    public int DelaySeconds { get; set; }

    /// <summary>点击模式</summary>
    public ClickMode ClickMode { get; set; } = ClickMode.Single;

    /// <summary>格式化延迟时间（如"1小时30分"）</summary>
    [JsonIgnore]
    public string DelayDisplay
    {
        get
        {
            if (DelaySeconds <= 0) return "立即";
            int h = DelaySeconds / 3600;
            int m = (DelaySeconds % 3600) / 60;
            int s = DelaySeconds % 60;
            if (h > 0 && m > 0) return $"{h}小时{m}分";
            if (h > 0) return $"{h}小时";
            if (m > 0 && s > 0) return $"{m}分{s}秒";
            if (m > 0) return $"{m}分钟";
            return $"{s}秒";
        }
        set { /* WPF ItemsControl DataTemplate 需要 setter 才能正常绑定 */ }
    }

    /// <summary>任务列表中的单行显示文本（含标签）</summary>
    [JsonIgnore]
    public string TaskSummary
    {
        get
        {
            string info = $"({TargetX}, {TargetY})  {DelayDisplay}";
            if (!string.IsNullOrWhiteSpace(Label))
                info = $"{Label}  {info}";
            return info;
        }
        set { }
    }
}
