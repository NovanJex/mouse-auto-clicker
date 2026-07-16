using System.Windows;
using AutoClicker.App.ViewModels;

namespace AutoClicker.App.Views;

public partial class PlanListView : Window
{
    public PlanListView(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
