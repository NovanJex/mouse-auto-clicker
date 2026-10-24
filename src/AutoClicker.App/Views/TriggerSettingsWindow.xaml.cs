using System.Windows;
using AutoClicker.App.ViewModels;

namespace AutoClicker.App.Views;

/// <summary>
/// 键盘触发设置窗口 — 监听开关、前台窗口过滤、自动暂停策略
/// </summary>
public partial class TriggerSettingsWindow : Window
{
    public TriggerSettingsWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
