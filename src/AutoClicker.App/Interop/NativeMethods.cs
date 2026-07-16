using System.Runtime.InteropServices;

namespace AutoClicker.App.Interop;

/// <summary>
/// Win32 API 函数声明（P/Invoke）
/// </summary>
public static class NativeMethods
{
    // ── 鼠标模拟 ──

    /// <summary>发送鼠标/键盘输入事件</summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>获取当前光标屏幕坐标</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>获取系统参数（屏幕分辨率等）</summary>
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    /// <summary>获取当前线程的消息额外信息</summary>
    [DllImport("user32.dll")]
    public static extern IntPtr GetMessageExtraInfo();

    /// <summary>获取系统双击间隔（毫秒）</summary>
    [DllImport("user32.dll")]
    public static extern uint GetDoubleClickTime();

    // ── 全局热键 ──

    /// <summary>注册系统全局热键</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    /// <summary>注销全局热键</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // ── 定时器精度 ──

    /// <summary>提升系统定时器精度</summary>
    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    public static extern uint TimeBeginPeriod(uint uPeriod);

    /// <summary>恢复系统定时器精度</summary>
    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    public static extern uint TimeEndPeriod(uint uPeriod);

    // ── 系统托盘 ──

    /// <summary>创建/修改/删除系统托盘图标</summary>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    /// <summary>销毁图标句柄</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>从文件加载图标/图片</summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    /// <summary>从 EXE/DLL 中提取图标</summary>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, uint nIcons);

    /// <summary>将窗口设为前台</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    // ── 鼠标钩子 ──

    /// <summary>钩子回调委托 — 必须存为类字段防止 GC 回收</summary>
    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>安装 Windows 消息钩子</summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    /// <summary>卸载 Windows 消息钩子</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    /// <summary>调用钩子链中的下一个钩子过程</summary>
    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>获取当前进程或指定模块的句柄</summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    // ── 消息泵控制 ──

    /// <summary>从线程消息队列中获取消息（阻塞）</summary>
    [DllImport("user32.dll")]
    public static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    /// <summary>翻译虚拟键消息为字符消息</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TranslateMessage([In] ref MSG lpMsg);

    /// <summary>分发消息到窗口过程</summary>
    [DllImport("user32.dll")]
    public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

    /// <summary>向指定窗口的消息队列投递消息</summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    /// <summary>获取指定屏幕坐标处的窗口句柄</summary>
    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(int x, int y);

    /// <summary>将屏幕坐标转换为窗口客户区坐标</summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
}
