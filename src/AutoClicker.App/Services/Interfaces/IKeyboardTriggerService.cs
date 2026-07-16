using AutoClicker.App.Models;

namespace AutoClicker.App.Services.Interfaces;

/// <summary>
/// 键盘触发服务接口 — 通过 WH_KEYBOARD_LL 全局钩子监听多个按键并触发对应坐标的点击
/// </summary>
public interface IKeyboardTriggerService : IDisposable
{
    /// <summary>键盘钩子是否已安装并正在监听触发键</summary>
    bool IsEnabled { get; }

    /// <summary>是否正在等待用户按键（录制模式）</summary>
    bool IsListeningForKey { get; }

    /// <summary>当前所有绑定的只读列表</summary>
    IReadOnlyList<TriggerBinding> Bindings { get; }

    /// <summary>批量设置绑定列表（自动重建内部查找字典）</summary>
    void SetBindings(IEnumerable<TriggerBinding> bindings);

    /// <summary>检测到已绑定的触发键按下时触发，回调在 UI 线程上运行</summary>
    event EventHandler<TriggerBinding>? TriggerKeyPressed;

    /// <summary>安装 WH_KEYBOARD_LL 钩子并开始监听所有已绑定按键</summary>
    void Enable();

    /// <summary>卸载钩子并停止监听</summary>
    void Disable();

    /// <summary>
    /// 进入录制模式 — 临时安装钩子（如果未激活），等待下一次按键按下
    /// 返回捕获到的虚拟键码，录制期间正常触发事件被抑制
    /// </summary>
    Task<uint> RecordKeyAsync();
}
