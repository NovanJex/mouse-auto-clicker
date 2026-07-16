namespace AutoClicker.App.Interop;

/// <summary>
/// Win32 API 常量定义
/// </summary>
public static class Win32Constants
{
    // ── 窗口消息 ──
    public const int WM_HOTKEY = 0x0312;
    public const int WM_CLOSE = 0x0010;

    // ── SendInput 输入类型 ──
    public const int INPUT_MOUSE = 0;
    public const int INPUT_KEYBOARD = 1;

    // ── 鼠标事件标志 ──
    public const int MOUSEEVENTF_MOVE = 0x0001;
    public const int MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const int MOUSEEVENTF_LEFTUP = 0x0004;
    public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const int MOUSEEVENTF_RIGHTUP = 0x0010;
    public const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const int MOUSEEVENTF_MIDDLEUP = 0x0040;
    public const int MOUSEEVENTF_ABSOLUTE = 0x8000;
    public const int MOUSEEVENTF_VIRTUALDESK = 0x4000;

    // ── RegisterHotKey 修饰键 ──
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    // ── 定时器精度 ──
    public const int TIME_PERIOD = 1;

    // ── Shell_NotifyIcon 操作 ──
    public const uint NIM_ADD = 0x00000000;
    public const uint NIM_MODIFY = 0x00000001;
    public const uint NIM_DELETE = 0x00000002;
    public const uint NIM_SETVERSION = 0x00000004;

    // ── NOTIFYICONDATA 字段标志 ──
    public const uint NIF_MESSAGE = 0x00000001;
    public const uint NIF_ICON = 0x00000002;
    public const uint NIF_TIP = 0x00000004;
    public const uint NIF_STATE = 0x00000008;
    public const uint NIF_INFO = 0x00000010;

    // ── 气泡通知标志 ──
    public const uint NIIF_INFO = 0x00000001;
    public const uint NIIF_WARNING = 0x00000002;
    public const uint NIIF_ERROR = 0x00000003;
    public const uint NIIF_NOSOUND = 0x00000010;

    // ── 托盘图标回调消息 ──
    public const int WM_TRAYICON = 0x8001;

    // ── 鼠标消息 ──
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_LBUTTONDBLCLK = 0x0203;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;
    public const int WM_MBUTTONDOWN = 0x0207;
    public const int WM_MBUTTONUP = 0x0208;

    // ── 消息泵 ──
    public const uint WM_QUIT = 0x0012;

    // ── 鼠标钩子 ──
    public const int WH_MOUSE_LL = 14;
    public const uint LLMHF_INJECTED = 0x00000001;

    // ── LoadImage 参数 ──
    public const uint IMAGE_ICON = 1;
    public const uint LR_LOADFROMFILE = 0x00000010;
    public const uint LR_DEFAULTSIZE = 0x00000040;

    // ── 键盘钩子 ──
    /// <summary>低层级键盘钩子类型</summary>
    public const int WH_KEYBOARD_LL = 13;

    // ── 键盘消息 ──
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int WM_SYSKEYUP = 0x0105;

    // ── LL 键盘钩子标志 ──
    /// <summary>键盘事件由代码注入（通过 SendInput / keybd_event）</summary>
    public const uint LLKHF_INJECTED = 0x00000010;
}
