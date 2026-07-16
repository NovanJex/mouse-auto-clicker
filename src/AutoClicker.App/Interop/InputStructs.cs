using System.Runtime.InteropServices;

namespace AutoClicker.App.Interop;

/// <summary>
/// Win32 坐标结构体
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

/// <summary>
/// SendInput 鼠标输入结构体
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MOUSEINPUT
{
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

/// <summary>
/// SendInput 键盘输入结构体
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct KEYBDINPUT
{
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

/// <summary>
/// SendInput 联合体 — 鼠标或键盘输入二选一
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct INPUT_UNION
{
    [FieldOffset(0)] public MOUSEINPUT mi;
    [FieldOffset(0)] public KEYBDINPUT ki;
}

/// <summary>
/// SendInput 顶层输入结构体
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct INPUT
{
    public int type;
    public INPUT_UNION u;
}

/// <summary>
/// Shell_NotifyIcon 托盘图标数据
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct NOTIFYICONDATA
{
    public int cbSize;
    public IntPtr hWnd;
    public uint uID;
    public uint uFlags;
    public uint uCallbackMessage;
    public IntPtr hIcon;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string szTip;
    public uint dwState;
    public uint dwStateMask;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string szInfo;
    public uint uVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string szInfoTitle;
    public uint dwInfoFlags;
    public Guid guidItem;
    public IntPtr hBalloonIcon;
}

/// <summary>
/// WH_MOUSE_LL 钩子回调中传递的鼠标事件数据
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MSLLHOOKSTRUCT
{
    public POINT pt;
    public uint mouseData;
    public uint flags;
    public uint time;
    public IntPtr dwExtraInfo;
}

/// <summary>
/// Windows 消息结构体（用于 GetMessage / DispatchMessage）
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct MSG
{
    public IntPtr hwnd;
    public uint message;
    public IntPtr wParam;
    public IntPtr lParam;
    public uint time;
    public POINT pt;
}

/// <summary>
/// WH_KEYBOARD_LL 钩子回调中传递的键盘事件数据
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct KBDLLHOOKSTRUCT
{
    /// <summary>虚拟键码（VK_*）</summary>
    public uint vkCode;
    /// <summary>硬件扫描码</summary>
    public uint scanCode;
    /// <summary>事件标志（含 LLKHF_INJECTED）</summary>
    public uint flags;
    /// <summary>事件时间戳</summary>
    public uint time;
    /// <summary>额外信息</summary>
    public IntPtr dwExtraInfo;
}
