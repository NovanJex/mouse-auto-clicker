using System.Windows;

namespace AutoClicker.App.Views;

/// <summary>
/// 关闭行为对话框
/// 返回: SelectedAction = "exit"(退出应用) | "tray"(最小化到托盘) | null(点击X取消)
/// </summary>
public partial class ConfirmDialog : Window
{
    /// <summary>用户选择: "exit" / "tray" / null(取消)</summary>
    public string? SelectedAction { get; private set; }

    /// <summary>是否勾选了"记住我的选择"</summary>
    public bool RememberChoice => CbRemember.IsChecked == true;

    public ConfirmDialog()
    {
        InitializeComponent();
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        SelectedAction = "exit";
        Close();
    }

    private void OnTrayClick(object sender, RoutedEventArgs e)
    {
        SelectedAction = "tray";
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (SelectedAction is null)
            SelectedAction = null; // 点击标题栏 X = 取消
        base.OnClosed(e);
    }
}
