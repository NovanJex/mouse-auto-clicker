using AutoClicker.App.Models;

namespace AutoClicker.App.Services.Interfaces;

/// <summary>
/// 全局热键服务接口 — 注册/注销系统级热键
/// </summary>
public interface IHotkeyService
{
    /// <summary>窗口句柄（热键绑定到此窗口）</summary>
    IntPtr Handle { get; set; }

    /// <summary>注册一个热键</summary>
    bool Register(HotkeyBinding binding);

    /// <summary>注销指定 ID 的热键</summary>
    void Unregister(int id);

    /// <summary>注销所有已注册的热键</summary>
    void UnregisterAll();

    /// <summary>重新注册所有热键（锁屏恢复后调用）</summary>
    void ReregisterAll();

    /// <summary>热键被按下时触发，参数为热键 ID</summary>
    event EventHandler<int>? HotkeyPressed;
}
