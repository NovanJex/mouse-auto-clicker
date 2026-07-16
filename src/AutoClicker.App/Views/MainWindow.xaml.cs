using System.Windows;
using System.Windows.Interop;
using AutoClicker.App.Models;
using AutoClicker.App.Services.Interfaces;
using AutoClicker.App.ViewModels;

namespace AutoClicker.App.Views;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private readonly ITrayService _trayService;
    private bool _initializing;

    public MainWindow(MainViewModel viewModel, ITrayService trayService)
    {
        DataContext = viewModel;
        _trayService = trayService;
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        ViewModel.RegisterHotkeys(handle);
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProcHook);
    }

    private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        ViewModel.WndProc(hwnd, msg, wParam, lParam, ref handled);
        _trayService.HandleMessage(msg, wParam, lParam);
        return IntPtr.Zero;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _initializing = true;
        if (ViewModel.UseFixedPosition) RbFixedPos.IsChecked = true; else RbCurrentPos.IsChecked = true;
        SelectRadioForClickMode(ViewModel.SelectedClickMode);
        SelectRadioForIntervalMode(ViewModel.SelectedIntervalMode);
        _initializing = false;
    }

    private void OnClickTargetCurrent(object sender, RoutedEventArgs e) { if (!_initializing) ViewModel.UseFixedPosition = false; }
    private void OnClickTargetFixed(object sender, RoutedEventArgs e) { if (!_initializing) ViewModel.UseFixedPosition = true; }
    private void OnClickModeSingle(object sender, RoutedEventArgs e) { if (!_initializing) ViewModel.SelectedClickMode = ClickMode.Single; }
    private void OnClickModeDouble(object sender, RoutedEventArgs e) { if (!_initializing) ViewModel.SelectedClickMode = ClickMode.Double; }
    private void OnClickModeRight(object sender, RoutedEventArgs e) { if (!_initializing) ViewModel.SelectedClickMode = ClickMode.Right; }
    private void OnClickModeMiddle(object sender, RoutedEventArgs e) { if (!_initializing) ViewModel.SelectedClickMode = ClickMode.Middle; }
    private void OnIntervalMs(object sender, RoutedEventArgs e) { if (!_initializing) ViewModel.SelectedIntervalMode = ClickIntervalMode.Ms; }
    private void OnIntervalCps(object sender, RoutedEventArgs e) { if (!_initializing) ViewModel.SelectedIntervalMode = ClickIntervalMode.Cps; }
    private void OnIntervalHold(object sender, RoutedEventArgs e) { if (!_initializing) ViewModel.SelectedIntervalMode = ClickIntervalMode.HoldDuration; }

    private void SelectRadioForClickMode(ClickMode mode) { RbModeSingle.IsChecked = mode == ClickMode.Single; RbModeDouble.IsChecked = mode == ClickMode.Double; RbModeRight.IsChecked = mode == ClickMode.Right; RbModeMiddle.IsChecked = mode == ClickMode.Middle; }
    private void SelectRadioForIntervalMode(ClickIntervalMode mode) { RbIntervalMs.IsChecked = mode == ClickIntervalMode.Ms; RbIntervalCps.IsChecked = mode == ClickIntervalMode.Cps; RbIntervalHold.IsChecked = mode == ClickIntervalMode.HoldDuration; }

    private void OnRepeatCountPreviewInput(object sender, System.Windows.Input.TextCompositionEventArgs e) { e.Handled = !int.TryParse(e.Text, out _); }
    private void OnRepeatCountLostFocus(object sender, RoutedEventArgs e) { if (!int.TryParse(TbRepeatCount.Text, out int val) || val < 1) ViewModel.RepeatCount = 1; }
}
