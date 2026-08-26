using System.Diagnostics;
using System.Runtime.InteropServices;
using AutoClicker.App.Interop;
using AutoClicker.App.Models;
using AutoClicker.App.Services.Interfaces;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 通过 WH_MOUSE_LL 全局钩子录制鼠标事件的实现
/// 钩子安装在 WPF UI 线程上，回调直接运行在 UI 线程，无跨线程问题
/// </summary>
public class MouseRecordingService : IMouseRecordingService, IDisposable
{
    private IntPtr _hookHandle;
    private NativeMethods.HookProc? _hookProc;
    private RecordingSession? _session;
    private Stopwatch? _stopwatch;
    private bool _disposed;

    /// <summary>录制事件数上限（达到后自动停止，防止内存/文件无限增长）</summary>
    private const int MaxRecordedEvents = 5000;

    public bool IsRecording { get; private set; }
    public event EventHandler<RecordedMouseEvent>? EventCaptured;
    public event EventHandler<RecordingSession?>? RecordingStopped;

    public void StartRecording()
    {
        if (IsRecording) return;

        _session = new RecordingSession { RecordedAt = DateTime.Now };
        _stopwatch = Stopwatch.StartNew();

        _hookProc = HookCallback;
        var hMod = NativeMethods.GetModuleHandle(null);
        _hookHandle = NativeMethods.SetWindowsHookEx(
            Win32Constants.WH_MOUSE_LL,
            _hookProc,
            hMod,
            0);

        IsRecording = _hookHandle != IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _stopwatch is not null)
        {
            var hookData = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            if ((hookData.flags & Win32Constants.LLMHF_INJECTED) == 0)
            {
                int msgId = (int)wParam;

                // 跳过鼠标移动事件：移动频率极高（125Hz+），会淹没点击事件
                if (msgId == Win32Constants.WM_MOUSEMOVE)
                    return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);

                var eventType = MapMessageToEventType(msgId);
                var timestamp = _stopwatch.Elapsed.TotalMilliseconds;

                if (eventType is { } et)
                {
                    var recordEvent = new RecordedMouseEvent(
                        et, hookData.pt.X, hookData.pt.Y, timestamp);

                    EventCaptured?.Invoke(this, recordEvent);
                    _session?.Events.Add(recordEvent);

                    // 达到事件数上限自动停止，防止内存/文件无限增长
                    if (_session is not null && _session.Events.Count >= MaxRecordedEvents)
                        StopRecording();
                }
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public RecordingSession? StopRecording()
    {
        if (!IsRecording) return null;

        _stopwatch?.Stop();
        IsRecording = false;

        // 卸载钩子
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        if (_session is not null)
            _session.TotalDurationMs = _stopwatch?.Elapsed.TotalMilliseconds ?? 0;

        var result = _session;
        _session = null;
        RecordingStopped?.Invoke(this, result);
        return result;
    }

    private static MouseEventType? MapMessageToEventType(int msg)
    {
        return msg switch
        {
            Win32Constants.WM_LBUTTONDOWN => MouseEventType.LeftDown,
            Win32Constants.WM_LBUTTONUP => MouseEventType.LeftUp,
            Win32Constants.WM_RBUTTONDOWN => MouseEventType.RightDown,
            Win32Constants.WM_RBUTTONUP => MouseEventType.RightUp,
            Win32Constants.WM_MBUTTONDOWN => MouseEventType.MiddleDown,
            Win32Constants.WM_MBUTTONUP => MouseEventType.MiddleUp,
            Win32Constants.WM_MOUSEMOVE => MouseEventType.Move,
            _ => null
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (IsRecording)
            StopRecording();

        _hookProc = null;
    }
}
