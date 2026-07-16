using System.Windows;
using AutoClicker.App.ViewModels;

namespace AutoClicker.App.Views;

/// <summary>
/// 快捷键绑定列表窗口 — 管理多个按键 → 坐标的映射
/// </summary>
public partial class TriggerBindListView : Window
{
    public TriggerBindListView(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
