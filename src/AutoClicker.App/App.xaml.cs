using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using AutoClicker.App.Services.Implementation;
using AutoClicker.App.Services.Interfaces;
using AutoClicker.App.ViewModels;
using AutoClicker.App.Views;

namespace AutoClicker.App;

/// <summary>
/// 应用入口 — DI 容器配置、启动流程、单实例检测、窗口生命周期管理
/// </summary>
public partial class App : Application
{
    /// <summary>单实例互斥体名称</summary>
    private static readonly string AppMutexName = "AutoClicker_MouseClicker_SingleInstance";
    private static Mutex? _appMutex;

    private ServiceProvider? _serviceProvider;
    private MainWindow? _mainWindow;
    private ITrayService? _trayService;

    protected override void OnStartup(StartupEventArgs e)
    {
        // ── 单实例检测 ──
        _appMutex = new Mutex(true, AppMutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("鼠标连点器已在运行中。\n请查看系统任务栏托盘区域。",
                "鼠标连点器", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _serviceProvider = ConfigureServices();

        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _trayService = _serviceProvider.GetRequiredService<ITrayService>();

        _mainWindow.StateChanged += OnMainWindowStateChanged;
        _mainWindow.Closing += OnMainWindowClosing;

        // 恢复上次窗口位置/大小（有保存值时）
        var settings = _serviceProvider.GetService<ISettingsService>();
        if (settings?.Settings is { } s)
        {
            if (s.WindowLeft is double l && s.WindowTop is double t)
            {
                _mainWindow.Left = l;
                _mainWindow.Top = t;
            }
            if (s.WindowWidth is double w && w >= 580) _mainWindow.Width = w;
            if (s.WindowHeight is double h && h >= 500) _mainWindow.Height = h;
        }

        // 先显示窗口，确保 HWND 句柄已创建
        _mainWindow.Show();

        // HWND 已存在，创建系统托盘图标
        var handle = new WindowInteropHelper(_mainWindow).Handle;
        _trayService.Create(handle);

        // 托盘左键 → 恢复窗口
        _trayService.TrayLeftClick += () => Dispatcher.Invoke(RestoreWindow);
        // 托盘右键 → 弹出上下文菜单
        _trayService.TrayRightClick += () => Dispatcher.Invoke(ShowTrayContextMenu);

        // 监听会话切换事件（锁屏/休眠恢复后重新注册热键）
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        _trayService?.Dispose();
        _serviceProvider?.Dispose();
        _appMutex?.Dispose();
        base.OnExit(e);
    }

    /// <summary>恢复主窗口（从托盘显示）</summary>
    private void RestoreWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    /// <summary>保存窗口位置/大小到设置（关闭时调用，随设置持久化）</summary>
    private void SaveWindowPosition(ISettingsService? settings)
    {
        if (settings is null || _mainWindow is null) return;
        // 最大化时不保存位置（恢复时用默认居中）
        if (_mainWindow.WindowState == WindowState.Maximized) return;

        settings.Settings.WindowLeft = _mainWindow.Left;
        settings.Settings.WindowTop = _mainWindow.Top;
        settings.Settings.WindowWidth = _mainWindow.Width;
        settings.Settings.WindowHeight = _mainWindow.Height;
    }

    /// <summary>显示托盘右键菜单</summary>
    private void ShowTrayContextMenu()
    {
        var vm = _serviceProvider?.GetService<MainViewModel>();
        var menu = new ContextMenu();

        menu.Items.Add(new MenuItem
        {
            Header = "显示窗口",
            Command = new CommunityToolkit.Mvvm.Input.RelayCommand(RestoreWindow)
        });

        // 开始/停止连点快捷项（动态标题跟随运行状态）
        if (vm is not null)
        {
            menu.Items.Add(new MenuItem
            {
                Header = vm.IsRunning ? "停止连点" : "开始连点",
                Command = vm.ToggleClickingCommand
            });
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(new MenuItem
        {
            Header = "退出",
            Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                (_trayService as TrayService)?.ExitApplication();
            })
        });

        menu.IsOpen = true;
    }

    /// <summary>最小化时保留在任务栏（不再隐藏到托盘）</summary>
    private void OnMainWindowStateChanged(object? sender, EventArgs e)
    {
        // 窗口正常最小化到任务栏，托盘图标保留用于右键菜单
    }

    /// <summary>关闭按钮 → 弹出关闭行为对话框，用户选择退出或最小化到托盘</summary>
    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;

        var vm = _serviceProvider?.GetService<MainViewModel>();
        var settings = _serviceProvider?.GetService<ISettingsService>();

        // 记住窗口位置/大小
        SaveWindowPosition(settings);

        // 如果已勾选"记住选择"，直接执行不弹窗
        if (settings?.Settings.RememberCloseChoice == true)
        {
            if (settings.Settings.CloseToTray)
            {
                _mainWindow?.Hide();
                return;
            }

            vm?.SaveAll();
            vm?.StopAndCleanup();
            _trayService?.Remove();
            Environment.Exit(0);
            return;
        }

        // 后台保存配置 — 用户阅读对话框时并发完成
        var saveTask = Task.Run(() => vm?.SaveAll());

        var dialog = new ConfirmDialog { Owner = _mainWindow };
        dialog.ShowDialog();

        if (dialog.SelectedAction == "exit")
        {
            try { saveTask.Wait(TimeSpan.FromSeconds(2)); } catch { }

            if (dialog.RememberChoice && settings is not null)
            {
                settings.Settings.RememberCloseChoice = true;
                settings.Settings.CloseToTray = false;
                settings.Save();
            }

            vm?.StopAndCleanup();
            _trayService?.Remove();
            Environment.Exit(0);
        }
        else if (dialog.SelectedAction == "tray")
        {
            if (dialog.RememberChoice && settings is not null)
            {
                settings.Settings.RememberCloseChoice = true;
                settings.Settings.CloseToTray = true;
                settings.Save();
            }

            _mainWindow?.Hide();
        }
        // 点击 X 关闭对话框 → 什么都不做
    }

    /// <summary>会话恢复（解锁/登录）后重新注册热键</summary>
    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock
            || e.Reason == SessionSwitchReason.ConsoleConnect)
        {
            Dispatcher.Invoke(() =>
            {
                var vm = _serviceProvider?.GetService<MainViewModel>();
                vm?.OnSessionSwitch();
            });
        }
    }

    /// <summary>配置依赖注入容器</summary>
    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IMouseSimulationService, MouseSimulationService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<IClickSchedulerService, ClickSchedulerService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ICoordinatePickerService, CoordinatePickerService>();
        services.AddSingleton<ITrayService, TrayService>();
        services.AddSingleton<IMouseRecordingService, MouseRecordingService>();
        services.AddSingleton<IRecordingPlayerService, RecordingPlayerService>();
        services.AddSingleton<IKeyboardTriggerService, KeyboardTriggerService>();
        services.AddSingleton<IScheduledTaskService, ScheduledTaskService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
