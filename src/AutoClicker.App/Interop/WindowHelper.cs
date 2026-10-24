using System.Diagnostics;
using System.IO;
using System.Text;

namespace AutoClicker.App.Interop;

/// <summary>
/// 前台窗口辅助方法 — 用于键盘触发的前台进程过滤。
/// 仅在按键命中绑定后调用，不在每次键盘事件中执行，开销可接受。
/// </summary>
public static class WindowHelper
{
    /// <summary>获取当前前台窗口所属进程 ID；无前台窗口时返回 0</summary>
    public static int GetForegroundProcessId()
    {
        var hWnd = NativeMethods.GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return 0;

        NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
        return (int)pid;
    }

    /// <summary>获取当前前台窗口所属进程名（含 .exe）；无法获取时返回空字符串</summary>
    public static string GetForegroundProcessName()
    {
        int pid = GetForegroundProcessId();
        return pid <= 0 ? string.Empty : GetProcessName(pid);
    }

    /// <summary>按进程 ID 获取进程名（含 .exe）</summary>
    public static string GetProcessName(int pid)
    {
        if (pid <= 0)
            return string.Empty;

        IntPtr hProcess = NativeMethods.OpenProcess(
            Win32Constants.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);

        if (hProcess == IntPtr.Zero)
        {
            // 权限不足时退回 .NET API（部分系统进程会走到这里）
            try
            {
                return Process.GetProcessById(pid).ProcessName + ".exe";
            }
            catch
            {
                return string.Empty;
            }
        }

        try
        {
            var buffer = new StringBuilder(1024);
            int size = buffer.Capacity;
            if (NativeMethods.QueryFullProcessImageName(hProcess, 0, buffer, ref size))
            {
                string fileName = Path.GetFileName(buffer.ToString());
                return fileName;
            }
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }
}
