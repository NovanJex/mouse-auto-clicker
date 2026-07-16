using System.Runtime.InteropServices;
using AutoClicker.App.Interop;
using AutoClicker.App.Models;
using AutoClicker.App.Services.Interfaces;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 键盘触发服务 — 通过 WH_KEYBOARD_LL 全局钩子监听多个绑定按键并触发对应坐标的点击事件
/// 遵循 MouseRecordingService 的架构模式，单个钩子同时支持触发检测和按键录制
/// </summary>
public class KeyboardTriggerService : IKeyboardTriggerService
{
    private IntPtr _hookHandle;
    private NativeMethods.HookProc? _hookProc;
    private bool _disposed;

    private readonly HashSet<uint> _pressedKeys = new();       // 已按下的键，用于抑制自动重复
    private TaskCompletionSource<uint>? _recordingTcs;         // 录制模式的 TCS
    private bool _ownedHook;                                    // 钩子是否由 RecordKeyAsync 临时安装
    private Dictionary<uint, TriggerBinding> _bindings = new(); // vkCode → 绑定映射
    private List<TriggerBinding> _bindingList = new();          // 有序列表副本

    /// <inheritdoc />
    public bool IsEnabled { get; private set; }

    /// <inheritdoc />
    public bool IsListeningForKey => _recordingTcs is not null;

    /// <inheritdoc />
    public IReadOnlyList<TriggerBinding> Bindings => _bindingList;

    /// <inheritdoc />
    public event EventHandler<TriggerBinding>? TriggerKeyPressed;

    /// <inheritdoc />
    public void SetBindings(IEnumerable<TriggerBinding> bindings)
    {
        _bindingList = bindings.ToList();
        _bindings = new Dictionary<uint, TriggerBinding>();
        foreach (var b in _bindingList)
        {
            // 重复按键时后者覆盖前者
            _bindings[b.VkCode] = b;
        }
    }

    /// <inheritdoc />
    public void Enable()
    {
        if (IsEnabled) return;

        _hookProc = HookCallback;
        var hMod = NativeMethods.GetModuleHandle(null);
        _hookHandle = NativeMethods.SetWindowsHookEx(
            Win32Constants.WH_KEYBOARD_LL,
            _hookProc,
            hMod,
            0); // dwThreadId = 0 → 全局钩子

        IsEnabled = _hookHandle != IntPtr.Zero;
    }

    /// <inheritdoc />
    public void Disable()
    {
        if (!IsEnabled && _hookHandle == IntPtr.Zero) return;

        IsEnabled = false;

        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        _pressedKeys.Clear();
        _hookProc = null;
        _ownedHook = false;

        // 取消录制模式
        _recordingTcs?.TrySetResult(0);
        _recordingTcs = null;
    }

    /// <inheritdoc />
    public async Task<uint> RecordKeyAsync()
    {
        _recordingTcs = new TaskCompletionSource<uint>();

        // 如果钩子尚未安装，临时安装
        if (!IsEnabled)
        {
            Enable();
            _ownedHook = IsEnabled;
        }

        try
        {
            uint vkCode = await _recordingTcs.Task;
            return vkCode;
        }
        finally
        {
            _recordingTcs = null;

            // 如果是临时安装的钩子，录制完成后卸载
            if (_ownedHook)
            {
                Disable();
                _ownedHook = false;
            }
        }
    }

    /// <summary>
    /// 钩子回调 — 在 UI 线程上执行（钩子安装在 UI 线程上）
    /// 处理逻辑：
    ///   1. WM_KEYDOWN：过滤注入事件，抑制重复，触发录制或检测
    ///   2. WM_KEYUP：从 _pressedKeys 中移除
    ///   3. 始终调用 CallNextHookEx 传递消息
    /// </summary>
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msgId = (int)wParam;
            var hookData = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            if (msgId == Win32Constants.WM_KEYDOWN || msgId == Win32Constants.WM_SYSKEYDOWN)
            {
                // 过滤注入事件，防止 SendInput 等代码生成的按键触发反馈循环
                if ((hookData.flags & Win32Constants.LLKHF_INJECTED) != 0)
                    return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);

                uint vk = hookData.vkCode;

                // 抑制自动重复：已按下的键不再触发
                if (!_pressedKeys.Add(vk))
                    return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);

                // 录制模式：捕获下一个按键
                if (_recordingTcs is not null)
                {
                    _recordingTcs.TrySetResult(vk);
                }
                // 正常触发模式：在绑定字典中查找匹配的按键
                else if (IsEnabled && _bindings.TryGetValue(vk, out var binding))
                {
                    TriggerKeyPressed?.Invoke(this, binding);
                }
            }
            else if (msgId == Win32Constants.WM_KEYUP || msgId == Win32Constants.WM_SYSKEYUP)
            {
                // 按键释放时从跟踪集合中移除
                _pressedKeys.Remove(hookData.vkCode);
            }
        }

        // 必须调用 CallNextHookEx，不阻塞任何按键
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Disable();
        _hookProc = null;
    }
}
