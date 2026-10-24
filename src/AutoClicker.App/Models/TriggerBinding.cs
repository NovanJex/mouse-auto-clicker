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

    /// <summary>按键可读名称（如 "~"、"Space"、"F1"）</summary>
    [JsonIgnore]
    public string KeyDisplay
    {
        get => GetKeyDisplayName(VkCode);
        set { /* WPF DataTemplate 绑定需要 setter */ }
    }

    /// <summary>将 Win32 虚拟键码转换为直观的显示名称</summary>
    private static string GetKeyDisplayName(uint vk)
    {
        // 主键盘数字和字母：虚拟键码与 ASCII 一致
        if (vk is >= 0x30 and <= 0x39) return ((char)vk).ToString();
        if (vk is >= 0x41 and <= 0x5A) return ((char)vk).ToString();

        return vk switch
        {
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0D => "Enter",
            0x10 or 0xA0 or 0xA1 => "Shift",
            0x11 or 0xA2 or 0xA3 => "Ctrl",
            0x12 or 0xA4 or 0xA5 => "Alt",
            0x13 => "Pause",
            0x14 => "CapsLock",
            0x1B => "Esc",
            0x20 => "Space",
            0x21 => "PageUp",
            0x22 => "PageDown",
            0x23 => "End",
            0x24 => "Home",
            0x25 => "←",
            0x26 => "↑",
            0x27 => "→",
            0x28 => "↓",
            0x2C => "PrtSc",
            0x2D => "Insert",
            0x2E => "Delete",
            0x5B or 0x5C => "Win",
            0x5D => "Menu",
            0x90 => "NumLock",
            0x91 => "ScrollLock",
            >= 0x60 and <= 0x69 => $"Num{vk - 0x60}",
            0x6A => "Num*",
            0x6B => "Num+",
            0x6C => "Num,",
            0x6D => "Num-",
            0x6E => "Num.",
            0x6F => "Num/",
            >= 0x70 and <= 0x87 => $"F{vk - 0x6F}",
            0xBA => ";",
            0xBB => "=",
            0xBC => ",",
            0xBD => "-",
            0xBE => ".",
            0xBF => "/",
            0xC0 => "~",
            0xDB => "[",
            0xDC => "\\",
            0xDD => "]",
            0xDE => "'",
            0xE2 => "\\",
            _ => KeyInterop.KeyFromVirtualKey((int)vk).ToString()
        };
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
