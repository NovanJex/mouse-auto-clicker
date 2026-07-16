using System.Windows;
using System.Windows.Input;
using AutoClicker.App.Interop;

namespace AutoClicker.App.Views;

/// <summary>
/// 坐标选取覆盖层窗口 — 全屏半透明，用户点击任意位置获取屏幕坐标
/// </summary>
public partial class CoordinatePickerWindow : Window
{
    public event Action<int, int>? CoordinatesPicked;
    public event Action? PickerCancelled;

    public CoordinatePickerWindow()
    {
        InitializeComponent();
        MouseDown += OnMouseDown;
        KeyDown += OnKeyDown;
    }

    /// <summary>鼠标点击时获取当前光标位置并通知调用方</summary>
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        NativeMethods.GetCursorPos(out var pt);
        var x = pt.X;
        var y = pt.Y;
        // 使用 BeginInvoke 延迟触发，避免在鼠标事件处理中关闭窗口
        Dispatcher.BeginInvoke(() =>
        {
            CoordinatesPicked?.Invoke(x, y);
        });
    }

    /// <summary>按 ESC 取消选取</summary>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            PickerCancelled?.Invoke();
    }
}
