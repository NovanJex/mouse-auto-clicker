using System.Text.Json.Serialization;
using System.Windows.Input;

namespace AutoClicker.App.Models;

/// <summary>
/// 快捷键触发绑定 — 一个按键映射到一个坐标位置和点击模式
/// </summary>
public class TriggerBinding
{
    /// <summary>Win32 虚拟键码</summary>
    public uint VkCode { get; set; }

    /// <summary>目标 X 坐标</summary>
    public int TargetX { get; set; }

    /// <summary>目标 Y 坐标</summary>
    public int TargetY { get; set; }

    /// <summary>点击模式</summary>
    public ClickMode ClickMode { get; set; } = ClickMode.Single;

    /// <summary>按键可读名称（如 "Space"、"F1"）</summary>
    [JsonIgnore]
    public string KeyDisplay
    {
        get => KeyInterop.KeyFromVirtualKey((int)VkCode).ToString();
        set { /* WPF DataTemplate 绑定需要 setter */ }
    }

    /// <summary>坐标显示文本</summary>
    [JsonIgnore]
    public string PositionDisplay
    {
        get => $"({TargetX}, {TargetY})";
        set { }
    }

    /// <summary>点击模式中文显示</summary>
    [JsonIgnore]
    public string ClickModeDisplay
    {
        get => ClickMode switch
        {
            ClickMode.Single => "左键单击",
            ClickMode.Double => "左键双击",
            ClickMode.Right => "右键单击",
            ClickMode.Middle => "中键单击",
            _ => "未知"
        };
        set { }
    }
}
