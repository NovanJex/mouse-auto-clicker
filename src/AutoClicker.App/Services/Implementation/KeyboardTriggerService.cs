using System.IO;
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

    private bool _foregroundFilterEnabled;                      // 是否启用前台进程过滤
    private HashSet<string> _allowedProcesses = new(StringComparer.OrdinalIgnoreCase);

    private static readonly int CurrentProcessId = Environment.ProcessId;

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
    public void SetForegroundProcessFilter(bool enabled, IEnumerable<string> allowedProcessNames)
    {
        _foregroundFilterEnabled = enabled;

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in allowedProcessNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;

            // 兼容用户填写完整路径/带引号的情况，只保留文件名
            string fileName = Path.GetFileName(name.Trim().Trim('"'));
            if (!string.IsNullOrEmpty(fileName))
                set.Add(fileName);
        }

        _allowedProcesses = set;
    }

    /// <inheritdoc />
    public void Enable()
    {
        if (IsEnabled) return;

        _pressedKeys.Clear();
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
        // 录制开始前清空按键状态，避免上一步残留导致首次按键被当成"重复按下"
        _pressedKeys.Clear();
        _ownedHook = false;

        // 异步续体不在钩子回调内同步执行，避免拖长钩子处理时间
        var tcs = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        _recordingTcs = tcs;

        // 如果钩子尚未安装，临时安装
        if (!IsEnabled)
        {
            Enable();
            _ownedHook = IsEnabled;
        }

        if (!IsEnabled)
        {
            _recordingTcs = null;
            return 0;
        }

        try
        {
            return await tcs.Task;
        }
        finally
        {
            if (ReferenceEquals(_recordingTcs, tcs))
                _recordingTcs = null;

            // 如果是临时安装的钩子，录制完成后卸载
            if (_ownedHook)
            {
                Disable();
                _ownedHook = false;
            }
        }
    }

    /// <inheritdoc />
    public void CancelRecording() => _recordingTcs?.TrySetResult(0);

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

                // 录制模式：捕获下一个按键（Esc 表示取消），录制期间不触发点击
                if (_recordingTcs is not null)
                {
                    _recordingTcs.TrySetResult(vk == Win32Constants.VK_ESCAPE ? 0u : vk);
                }
                // 正常触发模式：在绑定字典中查找匹配的按键
                else if (IsEnabled && _bindings.TryGetValue(vk, out var binding))
                {
                    if (CanTriggerInForeground())
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

    /// <summary>
    /// 判断当前前台窗口是否允许触发。
    /// 1. 应用自己的窗口聚焦时不触发（避免在软件内输入/录键时误点）；
    /// 2. 启用白名单后，只有列表中的进程在前台时才触发。
    /// </summary>
    private bool CanTriggerInForeground()
    {
        IntPtr foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
            return false;

        NativeMethods.GetWindowThreadProcessId(foreground, out uint pid);
        if (pid == 0 || (int)pid == CurrentProcessId)
            return false;

        if (!_foregroundFilterEnabled)
            return true;

        string processName = WindowHelper.GetProcessName((int)pid);
        return processName.Length > 0 && _allowedProcesses.Contains(processName);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Disable();
        _hookProc = null;
    }
}
