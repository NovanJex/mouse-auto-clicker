using System.Runtime.InteropServices;
using AutoClicker.App.Interop;
using AutoClicker.App.Services.Interfaces;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 系统托盘服务 — 通过 Win32 Shell_NotifyIcon 原生实现
/// 无需外部 NuGet 依赖，兼容 .NET 8
/// </summary>
public class TrayService : ITrayService
{
    private IntPtr _hWnd;
    private IntPtr _hIcon;
    private bool _added;
    private readonly ISettingsService _settings;
    private readonly IClickSchedulerService _scheduler;

    /// <inheritdoc />
    public event Action? TrayLeftClick;
    /// <inheritdoc />
    public event Action? TrayRightClick;

    public TrayService(ISettingsService settings, IClickSchedulerService scheduler)
    {
        _settings = settings;
        _scheduler = scheduler;
    }

    /// <inheritdoc />
    public void Create(IntPtr hWnd)
    {
        _hWnd = hWnd;
        _hIcon = LoadTrayIcon();

        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hWnd,
            uID = 1,
            uFlags = Win32Constants.NIF_MESSAGE | Win32Constants.NIF_ICON | Win32Constants.NIF_TIP,
            uCallbackMessage = Win32Constants.WM_TRAYICON,
            hIcon = _hIcon,
            szTip = "鼠标连点器"
        };

        NativeMethods.Shell_NotifyIcon(Win32Constants.NIM_ADD, ref nid);
        _added = true;
    }

    /// <inheritdoc />
    public void Remove()
    {
        if (!_added) return;

        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hWnd,
            uID = 1
        };

        NativeMethods.Shell_NotifyIcon(Win32Constants.NIM_DELETE, ref nid);
        _added = false;

        if (_hIcon != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }

    /// <inheritdoc />
    public void HandleMessage(int msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg != Win32Constants.WM_TRAYICON) return;

        var mouseMsg = (uint)lParam;
        if (mouseMsg == Win32Constants.WM_LBUTTONUP)
            TrayLeftClick?.Invoke();
        else if (mouseMsg == Win32Constants.WM_RBUTTONUP)
            TrayRightClick?.Invoke();
    }

    /// <summary>
    /// 退出程序 — 停止连点、保存配置、移除托盘图标并终止进程
    /// </summary>
    public void ExitApplication()
    {
        _scheduler.Stop();
        Task.Run(_settings.Save).Wait(TimeSpan.FromSeconds(2));
        Remove();
        Environment.Exit(0);
    }

    public void Dispose()
    {
        Remove();
    }

    /// <summary>
    /// 从当前 EXE 提取图标句柄（Win32 ExtractIconEx），失败时回退到系统默认图标
    /// </summary>
    private static IntPtr LoadTrayIcon()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (exePath is null) return IntPtr.Zero;

            var smallIcons = new IntPtr[1];
            var largeIcons = new IntPtr[1];

            uint count = NativeMethods.ExtractIconEx(exePath, 0, largeIcons, smallIcons, 1);
            if (count > 0)
            {
                // 使用小图标，释放不需要的大图标
                if (smallIcons[0] != IntPtr.Zero)
                {
                    if (largeIcons[0] != IntPtr.Zero)
                        NativeMethods.DestroyIcon(largeIcons[0]);
                    return smallIcons[0];
                }
                return largeIcons[0];
            }
        }
        catch { }

        // 回退：加载系统默认应用图标 (IDI_APPLICATION = 32512)
        return NativeMethods.LoadImage(IntPtr.Zero, "#32512", Win32Constants.IMAGE_ICON, 16, 16, 0);
    }
}
