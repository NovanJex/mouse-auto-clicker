namespace AutoClicker.App.Services.Interfaces;

/// <summary>
/// 系统托盘服务接口 — 通过 Shell_NotifyIcon 原生实现
/// </summary>
public interface ITrayService : IDisposable
{
    /// <summary>创建托盘图标</summary>
    void Create(IntPtr hWnd);

    /// <summary>移除托盘图标</summary>
    void Remove();

    /// <summary>处理托盘图标消息（转发自 WndProc）</summary>
    void HandleMessage(int msg, IntPtr wParam, IntPtr lParam);

    /// <summary>托盘图标左键点击事件</summary>
    event Action? TrayLeftClick;

    /// <summary>托盘图标右键点击事件</summary>
    event Action? TrayRightClick;
}
