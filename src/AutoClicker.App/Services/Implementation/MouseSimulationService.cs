using System.Runtime.InteropServices;
using AutoClicker.App.Interop;
using AutoClicker.App.Models;
using AutoClicker.App.Services.Interfaces;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 鼠标模拟服务 — 通过 Win32 SendInput 实现鼠标移动和点击
/// </summary>
public class MouseSimulationService : IMouseSimulationService
{
    /// <inheritdoc />
    public void MoveTo(int x, int y)
    {
        var input = CreateMoveInput(x, y);
        NativeMethods.SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    /// <inheritdoc />
    public void Click(ClickMode mode)
    {
        Down(mode);
        Up(mode);
    }

    /// <inheritdoc />
    public void Down(ClickMode mode)
    {
        var input = CreateMouseInput(mode, isDown: true);
        NativeMethods.SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    /// <inheritdoc />
    public void Up(ClickMode mode)
    {
        var input = CreateMouseInput(mode, isDown: false);
        NativeMethods.SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    /// <inheritdoc />
    public (int X, int Y) GetCurrentPosition()
    {
        NativeMethods.GetCursorPos(out var pt);
        return (pt.X, pt.Y);
    }

    /// <inheritdoc />
    /// <remarks>
    /// 核心思路：将 MoveTo(目标) + Down + Up + MoveTo(原位) 合并为一次 SendInput 调用，
    /// 让 Windows 将它们作为原子批次处理，鼠标指针不会在屏幕上闪现到目标位置。
    /// </remarks>
    public void ClickAtWithoutMoving(int targetX, int targetY, int restoreX, int restoreY, ClickMode mode)
    {
        if (mode == ClickMode.Double)
        {
            // 双击：移动 + 两组按下/释放 + 移回（共 6 个 INPUT）
            var inputs = new INPUT[6];
            inputs[0] = CreateMoveInput(targetX, targetY);
            inputs[1] = CreateMouseInput(ClickMode.Single, isDown: true);
            inputs[2] = CreateMouseInput(ClickMode.Single, isDown: false);
            inputs[3] = CreateMouseInput(ClickMode.Single, isDown: true);
            inputs[4] = CreateMouseInput(ClickMode.Single, isDown: false);
            inputs[5] = CreateMoveInput(restoreX, restoreY);
            NativeMethods.SendInput(6, inputs, Marshal.SizeOf<INPUT>());
        }
        else
        {
            // 普通点击：移动 + 按下 + 释放 + 移回（共 4 个 INPUT）
            var inputs = new INPUT[4];
            inputs[0] = CreateMoveInput(targetX, targetY);
            inputs[1] = CreateMouseInput(mode, isDown: true);
            inputs[2] = CreateMouseInput(mode, isDown: false);
            inputs[3] = CreateMoveInput(restoreX, restoreY);
            NativeMethods.SendInput(4, inputs, Marshal.SizeOf<INPUT>());
        }
    }

    /// <summary>
    /// 构建鼠标移动输入 — 使用 0~65535 归一化坐标，覆盖多显示器虚拟桌面
    /// </summary>
    private static INPUT CreateMoveInput(int x, int y)
    {
        int screenWidth = NativeMethods.GetSystemMetrics(0);  // SM_CXSCREEN
        int screenHeight = NativeMethods.GetSystemMetrics(1); // SM_CYSCREEN

        int normalizedX = (int)(x * 65536.0 / screenWidth);
        int normalizedY = (int)(y * 65536.0 / screenHeight);

        return new INPUT
        {
            type = Win32Constants.INPUT_MOUSE,
            u = new INPUT_UNION
            {
                mi = new MOUSEINPUT
                {
                    dx = normalizedX,
                    dy = normalizedY,
                    dwFlags = Win32Constants.MOUSEEVENTF_MOVE
                           | Win32Constants.MOUSEEVENTF_ABSOLUTE
                           | Win32Constants.MOUSEEVENTF_VIRTUALDESK,
                    time = 0,
                    dwExtraInfo = NativeMethods.GetMessageExtraInfo()
                }
            }
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// 通过 WindowFromPoint 定位目标窗口，用 PostMessage 直接投递点击消息。
    /// 完全不移动物理光标，因此不会有任何光标闪烁或瞬移。
    /// 如果目标坐标处没有窗口（如桌面），则回退到 ClickAtWithoutMoving。
    /// </remarks>
    public void PostClickAt(int targetX, int targetY, ClickMode mode)
    {
        var hWnd = NativeMethods.WindowFromPoint(targetX, targetY);
        if (hWnd == IntPtr.Zero)
        {
            // 桌面或无窗口区域，回退 SendInput 方案
            var (curX, curY) = GetCurrentPosition();
            ClickAtWithoutMoving(targetX, targetY, curX, curY, mode);
            return;
        }

        var pt = new POINT { X = targetX, Y = targetY };
        NativeMethods.ScreenToClient(hWnd, ref pt);
        IntPtr lParam = (IntPtr)((pt.Y << 16) | (pt.X & 0xFFFF));

        switch (mode)
        {
            case ClickMode.Single:
                NativeMethods.PostMessage(hWnd, Win32Constants.WM_LBUTTONDOWN, (IntPtr)1, lParam);
                NativeMethods.PostMessage(hWnd, Win32Constants.WM_LBUTTONUP, IntPtr.Zero, lParam);
                break;

            case ClickMode.Double:
                NativeMethods.PostMessage(hWnd, Win32Constants.WM_LBUTTONDOWN, (IntPtr)1, lParam);
                NativeMethods.PostMessage(hWnd, Win32Constants.WM_LBUTTONUP, IntPtr.Zero, lParam);
                NativeMethods.PostMessage(hWnd, Win32Constants.WM_LBUTTONDBLCLK, (IntPtr)1, lParam);
                NativeMethods.PostMessage(hWnd, Win32Constants.WM_LBUTTONUP, IntPtr.Zero, lParam);
                break;

            case ClickMode.Right:
                NativeMethods.PostMessage(hWnd, Win32Constants.WM_RBUTTONDOWN, (IntPtr)2, lParam);
                NativeMethods.PostMessage(hWnd, Win32Constants.WM_RBUTTONUP, IntPtr.Zero, lParam);
                break;

            case ClickMode.Middle:
                NativeMethods.PostMessage(hWnd, Win32Constants.WM_MBUTTONDOWN, (IntPtr)16, lParam);
                NativeMethods.PostMessage(hWnd, Win32Constants.WM_MBUTTONUP, IntPtr.Zero, lParam);
                break;
        }
    }

    /// <summary>
    /// 构建鼠标按键输入（按下或释放）
    /// </summary>
    private static INPUT CreateMouseInput(ClickMode mode, bool isDown)
    {
        uint flags = mode switch
        {
            ClickMode.Single => isDown ? (uint)Win32Constants.MOUSEEVENTF_LEFTDOWN : (uint)Win32Constants.MOUSEEVENTF_LEFTUP,
            ClickMode.Double => isDown ? (uint)Win32Constants.MOUSEEVENTF_LEFTDOWN : (uint)Win32Constants.MOUSEEVENTF_LEFTUP,
            ClickMode.Right => isDown ? (uint)Win32Constants.MOUSEEVENTF_RIGHTDOWN : (uint)Win32Constants.MOUSEEVENTF_RIGHTUP,
            ClickMode.Middle => isDown ? (uint)Win32Constants.MOUSEEVENTF_MIDDLEDOWN : (uint)Win32Constants.MOUSEEVENTF_MIDDLEUP,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        return new INPUT
        {
            type = Win32Constants.INPUT_MOUSE,
            u = new INPUT_UNION
            {
                mi = new MOUSEINPUT
                {
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = NativeMethods.GetMessageExtraInfo()
                }
            }
        };
    }
}
