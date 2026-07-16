using System.Windows.Input;

namespace AutoClicker.App.Models;

/// <summary>
/// 全局热键绑定模型
/// </summary>
/// <param name="Key">按键</param>
/// <param name="Modifiers">修饰键（Ctrl/Alt/Shift/Win）</param>
/// <param name="Id">热键唯一标识</param>
public record HotkeyBinding(Key Key, ModifierKeys Modifiers, int Id);
