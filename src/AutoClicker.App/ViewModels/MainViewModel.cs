using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoClicker.App.Interop;
using AutoClicker.App.Models;
using AutoClicker.App.Serialization;
using AutoClicker.App.Services.Implementation;
using AutoClicker.App.Services.Interfaces;
using AutoClicker.App.Views;

namespace AutoClicker.App.ViewModels;

/// <summary>
/// 核心 ViewModel — 整合所有服务与 UI 状态
/// 使用 CommunityToolkit.Mvvm 源生成器实现 MVVM 模式
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IMouseSimulationService _mouseSimulator;
    private readonly IHotkeyService _hotkeyService;
    private readonly IClickSchedulerService _clickScheduler;
    private readonly ISettingsService _settingsService;
    private readonly ICoordinatePickerService _coordinatePicker;
    private readonly IMouseRecordingService _recordingService;
    private readonly IRecordingPlayerService _playerService;
    private readonly IKeyboardTriggerService _keyboardTriggerService;
    private readonly IScheduledTaskService _scheduledTaskService;

    private CancellationTokenSource? _appCts;
    private CancellationTokenSource? _loopCts;
    private CancellationTokenSource? _saveDebounceCts;
    private RecordingSession? _currentRecording;
    private bool _isPickingCoordinate;
    private bool _listenerWasEnabledBeforePause;
    private readonly List<Window> _childWindows = new();   // 保持子窗口引用，防止 GC 回收导致窗口异常
    private static readonly string RecordingPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoClicker", "recording.json");
    private const int HotkeyStartId = 1;
    private const int HotkeyStopId = 2;
    private const int HotkeyRecordId = 3;
    private const int HotkeyPlayId = 4;
    private const int HotkeyLoopPlayId = 5;
    private const int MinMs = 50;
    private const int MaxMs = 60000;
    private const int MinCps = 1;
    private const int MaxCps = 100;

    // ── 可观察属性（由 CommunityToolkit.Mvvm 源生成器生成公开属性） ──

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "已停止";

    [ObservableProperty]
    private bool _useFixedPosition;

    [ObservableProperty]
    private int _targetX;

    [ObservableProperty]
    private int _targetY;

    [ObservableProperty]
    private ClickMode _selectedClickMode = ClickMode.Single;

    [ObservableProperty]
    private ClickIntervalMode _selectedIntervalMode = ClickIntervalMode.Ms;

    [ObservableProperty]
    private int _intervalMs = 100;

    [ObservableProperty]
    private int _cps = 10;

    [ObservableProperty]
    private int _holdDurationMs = 500;

    // ── 录制相关属性 ──

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private int _recordingEventCount;

    [ObservableProperty]
    private string _recordingDuration = "0.0 秒";

    [ObservableProperty]
    private bool _hasRecording;

    [ObservableProperty]
    private int _repeatCount = 1;

    [ObservableProperty]
    private bool _isLoopPlaying;

    // ── 键盘触发相关属性 ──

    [ObservableProperty]
    private bool _keyboardTriggerEnabled;

    [ObservableProperty]
    private bool _isListeningForKey;

    /// <summary>总开关：true = 已暂停（所有自动化停止，按 F7 恢复）</summary>
    [ObservableProperty]
    private bool _isPaused = true;

    /// <summary>启动时是否自动恢复键盘监听</summary>
    [ObservableProperty]
    private bool _autoResumeListeningOnStartup;

    /// <summary>是否仅当指定程序在前台时才触发点击</summary>
    [ObservableProperty]
    private bool _triggerWindowFilterEnabled;

    /// <summary>关闭窗口到托盘时是否自动暂停全部</summary>
    [ObservableProperty]
    private bool _pauseWhenMinimizedToTray = true;

    /// <summary>锁屏时是否自动暂停全部</summary>
    [ObservableProperty]
    private bool _pauseWhenLocked = true;

    /// <summary>手动添加白名单进程的输入框</summary>
    [ObservableProperty]
    private string _triggerProcessInput = "";

    /// <summary>捕获前台窗口时的状态提示</summary>
    [ObservableProperty]
    private string _foregroundCaptureStatus = "";

    /// <summary>前台窗口白名单（进程名）</summary>
    public ObservableCollection<string> TriggerWindowProcesses { get; } = new();

    /// <summary>当前所有快捷键绑定的只读列表（来自服务层）</summary>
    public IReadOnlyList<TriggerBinding> TriggerBindings => _keyboardTriggerService.Bindings;

    /// <summary>触发键的可读显示名称（显示最新录制的按键，或"未绑定"）</summary>
    public string TriggerKeyDisplay =>
        _keyboardTriggerService.Bindings.Count switch
        {
            0 => "未绑定",
            1 => _keyboardTriggerService.Bindings[0].KeyDisplay,
            var count => $"{_keyboardTriggerService.Bindings[^1].KeyDisplay} 等 {count} 个键"
        };

    /// <summary>键盘监听状态文字</summary>
    public string KeyboardListenerStateText => KeyboardTriggerEnabled ? "监听中" : "已关闭";

    /// <summary>监听开关按钮文字（显示可执行的动作）</summary>
    public string KeyboardTriggerToggleText => KeyboardTriggerEnabled ? "停止监听" : "启动监听";

    /// <summary>总开关状态文字（命令条使用短文案，详细说明见 GlobalStateHint）</summary>
    public string GlobalStateText => IsPaused ? "已暂停" : "运行中";

    /// <summary>总开关说明文字</summary>
    public string GlobalStateHint => IsPaused
        ? "所有连点、回放、定时任务和键盘监听均已停止"
        : KeyboardTriggerEnabled
            ? "键盘监听中；按 F7 可随时暂停全部"
            : "键盘监听未开启；按 F7 可随时暂停全部";

    /// <summary>总开关按钮文字</summary>
    public string PauseAllButtonText => IsPaused ? "恢复全部 (F7)" : "暂停全部 (F7)";

    /// <summary>是否可以录制触发键（暂停或未选固定坐标时不可用）</summary>
    public bool CanRecordTriggerKey => !IsPaused && UseFixedPosition;

    /// <summary>托盘悬停提示</summary>
    public string TrayToolTip => IsPaused
        ? "鼠标连点器 — 已暂停"
        : KeyboardTriggerEnabled
            ? "鼠标连点器 — 键盘监听中"
            : "鼠标连点器 — 就绪";

    // ── 定时任务相关属性 ──

    [ObservableProperty]
    private bool _isScheduledRunning;

    [ObservableProperty]
    private string _scheduledStatusText = "未执行";

    [ObservableProperty]
    private string _newTaskLabel = "";

    [ObservableProperty]
    private int _newTaskDelayValue;

    [ObservableProperty]
    private int _selectedTimeUnitIndex; // 0=秒, 1=分钟, 2=小时

    // ── 设置属性变更自动保存（防抖 500ms） ──
    partial void OnUseFixedPositionChanged(bool value)
    {
        DebouncedSaveSettings();
        OnPropertyChanged(nameof(CanRecordTriggerKey));
    }
    partial void OnTargetXChanged(int value) => DebouncedSaveSettings();
    partial void OnTargetYChanged(int value) => DebouncedSaveSettings();

    partial void OnIntervalMsChanged(int value) => DebouncedSaveSettings();
    partial void OnCpsChanged(int value) => DebouncedSaveSettings();
    partial void OnHoldDurationMsChanged(int value) => DebouncedSaveSettings();
    partial void OnRepeatCountChanged(int value) => DebouncedSaveSettings();

    /// <summary>时间单位选项列表（中文显示）</summary>
    public static List<string> TimeUnitOptions => new() { "秒", "分钟", "小时" };

    /// <summary>新任务延迟的可读显示</summary>
    public string NewTaskDelayDisplay
    {
        get
        {
            if (NewTaskDelayValue <= 0) return "立即执行";
            int totalSeconds = NewTaskDelayValue * GetTimeUnitMultiplier();
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;
            if (h > 0 && m > 0) return $"= {h}小时{m}分钟";
            if (h > 0) return $"= {h}小时";
            if (m > 0 && s > 0) return $"= {m}分{s}秒";
            if (m > 0) return $"= {m}分钟";
            return $"= {s}秒";
        }
    }

    /// <summary>当前点击模式的中文显示</summary>
    public string SelectedClickModeDisplay => SelectedClickMode switch
    {
        ClickMode.Single => "左键单击",
        ClickMode.Double => "左键双击",
        ClickMode.Right => "右键单击",
        ClickMode.Middle => "中键单击",
        _ => "未知"
    };

    /// <summary>任务列表（自动通知 UI）</summary>
    public ObservableCollection<ScheduledClickTask> ScheduledTasks => _scheduledTaskService.Tasks;

    // ── 间隔模式衍生属性（用于 UI 可见性绑定） ──

    public bool IsMsMode => SelectedIntervalMode == ClickIntervalMode.Ms;
    public bool IsCpsMode => SelectedIntervalMode == ClickIntervalMode.Cps;
    public bool IsHoldMode => SelectedIntervalMode == ClickIntervalMode.HoldDuration;

    public MainViewModel(
        IMouseSimulationService mouseSimulator,
        IHotkeyService hotkeyService,
        IClickSchedulerService clickScheduler,
        ISettingsService settingsService,
        ICoordinatePickerService coordinatePicker,
        IMouseRecordingService recordingService,
        IRecordingPlayerService playerService,
        IKeyboardTriggerService keyboardTriggerService,
        IScheduledTaskService scheduledTaskService)
    {
        _mouseSimulator = mouseSimulator;
        _hotkeyService = hotkeyService;
        _clickScheduler = clickScheduler;
        _settingsService = settingsService;
        _coordinatePicker = coordinatePicker;
        _recordingService = recordingService;
        _playerService = playerService;
        _keyboardTriggerService = keyboardTriggerService;
        _scheduledTaskService = scheduledTaskService;

        LoadSettings();
        LoadRecording();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _recordingService.EventCaptured += OnEventCaptured;
        _recordingService.RecordingStopped += OnRecordingStopped;
        _playerService.PlaybackCompleted += OnPlaybackCompleted;
        _keyboardTriggerService.TriggerKeyPressed += OnTriggerKeyPressed;
        _scheduledTaskService.TaskStarted += OnScheduledTaskStarted;
        _scheduledTaskService.TaskCompleted += OnScheduledTaskCompleted;
        _scheduledTaskService.AllCompleted += OnScheduledAllCompleted;
        _scheduledTaskService.Stopped += OnScheduledStopped;
    }

    // ── 命令 ──

    /// <summary>切换连点状态（开始/停止）</summary>
    [RelayCommand]
    private async Task ToggleClicking()
    {
        if (IsPaused)
        {
            StatusText = "已暂停，请按 F7 恢复";
            return;
        }

        if (IsRunning)
            StopClicking();
        else
            await StartClickingAsync();
    }

    /// <summary>打开坐标选取器</summary>
    [RelayCommand]
    private async Task PickCoordinates()
    {
        _isPickingCoordinate = true;
        try
        {
            var result = await _coordinatePicker.PickCoordinatesAsync();
            if (result is { } coords)
            {
                TargetX = coords.X;
                TargetY = coords.Y;
            }
        }
        finally
        {
            _isPickingCoordinate = false;
        }
    }

    /// <summary>切换录制状态（开始/停止）</summary>
    [RelayCommand]
    private void ToggleRecording()
    {
        if (IsPaused)
        {
            StatusText = "已暂停，请按 F7 恢复";
            return;
        }

        if (IsPlaying) return;

        if (_recordingService.IsRecording)
        {
            // 状态同步、会话保存由 RecordingStopped 事件统一处理（OnRecordingStopped）
            _recordingService.StopRecording();
            IsRecording = false;
        }
        else
        {
            _recordingService.StartRecording();
            _recordingCount = 0;
            IsRecording = true;
            RecordingEventCount = 0;
            RecordingDuration = "0.0 秒";
            HasRecording = false;
        }
    }

    /// <summary>播放已录制的事件</summary>
    [RelayCommand]
    private async Task PlayRecording()
    {
        if (IsPaused)
        {
            StatusText = "已暂停，请按 F7 恢复";
            return;
        }

        if (_playerService.IsPlaying)
        {
            _playerService.Stop();
            return;
        }

        if (_currentRecording is null || _currentRecording.Events.Count == 0)
            return;

        // 如果文件中有未加载的录制，先加载
        if (_currentRecording.Events.Count == 0)
            return;

        IsPlaying = true;

        using var cts = new CancellationTokenSource();
        await _playerService.PlayAsync(_currentRecording, cts.Token);
    }

    /// <summary>循环回放 — 按设定次数重复播放录制</summary>
    [RelayCommand]
    private async Task LoopPlayRecording()
    {
        if (IsPaused)
        {
            StatusText = "已暂停，请按 F7 恢复";
            return;
        }

        // 循环播放中 → 停止循环
        if (IsLoopPlaying)
        {
            _loopCts?.Cancel();
            _playerService.Stop();
            return;
        }

        // 单次播放中 → 先停止单次播放，再启动循环
        if (_playerService.IsPlaying)
            _playerService.Stop();

        if (_currentRecording is null || _currentRecording.Events.Count == 0)
            return;

        IsLoopPlaying = true;
        int count = Math.Clamp(RepeatCount, 1, 999);

        _loopCts = new CancellationTokenSource();
        try
        {
            for (int i = 0; i < count && !_loopCts.IsCancellationRequested; i++)
            {
                await _playerService.PlayAsync(_currentRecording, _loopCts.Token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _loopCts?.Dispose();
            _loopCts = null;
            IsLoopPlaying = false;
        }
    }

    /// <summary>清空当前录制数据</summary>
    [RelayCommand]
    private void ClearRecording()
    {
        if (IsRecording || IsPlaying) return;

        _currentRecording = null;
        _recordingCount = 0;
        HasRecording = false;
        RecordingEventCount = 0;
        RecordingDuration = "0.0 秒";

        try
        {
            if (File.Exists(RecordingPath))
                File.Delete(RecordingPath);
        }
        catch { /* 文件被占用时忽略，下次录制会覆盖 */ }
    }

    /// <summary>切换键盘触发点击的启用状态</summary>
    [RelayCommand]
    private void ToggleKeyboardTrigger()
    {
        if (IsPaused)
        {
            StatusText = "已暂停，请先按 F7 恢复";
            return;
        }

        KeyboardTriggerEnabled = !KeyboardTriggerEnabled;

        if (!KeyboardTriggerEnabled)
        {
            StatusText = "键盘监听已关闭";
        }
        else if (!TriggerWindowFilterEnabled)
        {
            StatusText = "键盘监听已开启（建议在触发设置中开启前台窗口过滤）";
        }
        else if (TriggerWindowProcesses.Count == 0)
        {
            StatusText = "键盘监听已开启，但白名单为空，不会触发";
        }
        else
        {
            StatusText = "键盘监听已开启";
        }
    }

    /// <summary>切换总开关：暂停全部 / 恢复全部</summary>
    [RelayCommand]
    private void TogglePause()
    {
        if (IsPaused)
            ExitPause();
        else
            EnterPause();
    }

    /// <summary>录制触发按键 — 录键后使用当前坐标和点击模式创建绑定</summary>
    [RelayCommand]
    private async Task RecordTriggerKey()
    {
        if (IsPaused)
        {
            StatusText = "已暂停，请先按 F7 恢复";
            return;
        }

        if (!UseFixedPosition)
        {
            StatusText = "录键绑定前请先选择「固定屏幕位置」并选取坐标";
            return;
        }

        // 再次点击按钮 = 取消录制
        if (IsListeningForKey)
        {
            _keyboardTriggerService.CancelRecording();
            StatusText = "已取消录键";
            return;
        }

        IsListeningForKey = true;
        StatusText = "请按下要绑定的按键（Esc 取消）";
        try
        {
            uint vkCode = await _keyboardTriggerService.RecordKeyAsync();
            if (vkCode == 0)
            {
                StatusText = "已取消录键";
                return;
            }

            // F6–F10 已被全局热键占用，绑定后会永久冲突
            if (vkCode >= Win32Constants.VK_F6 && vkCode <= Win32Constants.VK_F10)
            {
                StatusText = "F6–F10 是全局热键，不能作为触发键，请换一个键";
                return;
            }

            // 检查是否与已有绑定按键重复，重复则更新该绑定的坐标和点击模式
            var existing = _keyboardTriggerService.Bindings.FirstOrDefault(b => b.VkCode == vkCode);
            var list = _keyboardTriggerService.Bindings.ToList();

            if (existing is not null)
            {
                existing.TargetX = TargetX;
                existing.TargetY = TargetY;
                existing.ClickMode = SelectedClickMode;
            }
            else
            {
                list.Add(new TriggerBinding
                {
                    VkCode = vkCode,
                    TargetX = TargetX,
                    TargetY = TargetY,
                    ClickMode = SelectedClickMode
                });
            }

            _keyboardTriggerService.SetBindings(list);
            OnPropertyChanged(nameof(TriggerBindings));
            OnPropertyChanged(nameof(TriggerKeyDisplay));
            DebouncedSaveSettings();

            var keyDisplay = _keyboardTriggerService.Bindings
                .FirstOrDefault(b => b.VkCode == vkCode)?.KeyDisplay ?? vkCode.ToString();
            StatusText = $"已绑定按键 {keyDisplay}";
        }
        finally
        {
            IsListeningForKey = false;
        }
    }

    /// <summary>打开快捷键绑定列表窗口</summary>
    [RelayCommand]
    private void ShowTriggerBindList()
    {
        ShowChildWindow(new TriggerBindListView(this));
    }

    /// <summary>打开键盘触发设置窗口</summary>
    [RelayCommand]
    private void ShowTriggerSettings()
    {
        ShowChildWindow(new TriggerSettingsWindow(this));
    }

    /// <summary>删除指定快捷键触发绑定</summary>
    [RelayCommand]
    private void RemoveTriggerBinding(TriggerBinding binding)
    {
        var list = _keyboardTriggerService.Bindings.ToList();
        list.Remove(binding);
        _keyboardTriggerService.SetBindings(list);
        OnPropertyChanged(nameof(TriggerBindings));
        OnPropertyChanged(nameof(TriggerKeyDisplay));
        DebouncedSaveSettings();
    }

    /// <summary>手动添加前台窗口白名单进程</summary>
    [RelayCommand]
    private void AddTriggerProcess()
    {
        AddTriggerProcessName(TriggerProcessInput);
        TriggerProcessInput = "";
    }

    /// <summary>删除前台窗口白名单进程</summary>
    [RelayCommand]
    private void RemoveTriggerProcess(string processName)
    {
        if (TriggerWindowProcesses.Remove(processName))
        {
            ApplyTriggerFilter();
            DebouncedSaveSettings();
        }
    }

    /// <summary>捕获当前前台窗口进程 — 先留 3 秒时间切换到目标窗口</summary>
    [RelayCommand]
    private async Task CaptureForegroundProcess()
    {
        ForegroundCaptureStatus = "请在 3 秒内切换到目标窗口...";
        await Task.Delay(3000);

        int pid = WindowHelper.GetForegroundProcessId();
        if (pid <= 0)
        {
            ForegroundCaptureStatus = "未检测到前台窗口，请手动输入进程名";
            return;
        }

        if (pid == Environment.ProcessId)
        {
            ForegroundCaptureStatus = "当前前台是本程序，请切换到目标窗口后重试";
            return;
        }

        string processName = WindowHelper.GetProcessName(pid);
        if (processName.Length == 0)
        {
            ForegroundCaptureStatus = "无法读取进程名，请手动输入";
            return;
        }

        bool added = AddTriggerProcessName(processName);
        ForegroundCaptureStatus = added
            ? $"已添加：{processName}"
            : $"{processName} 已在列表中";
    }

    /// <summary>添加白名单进程名，返回是否新增成功</summary>
    private bool AddTriggerProcessName(string? rawName)
    {
        string name = (rawName ?? "").Trim().Trim('"');
        if (name.Length == 0) return false;

        // 允许用户粘贴完整路径，只保留文件名
        name = Path.GetFileName(name);
        if (name.Length == 0) return false;
        if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name += ".exe";

        // 本程序自身不允许加入白名单
        string? currentProcess = Path.GetFileName(Environment.ProcessPath);
        if (!string.IsNullOrEmpty(currentProcess)
            && string.Equals(name, currentProcess, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (TriggerWindowProcesses.Any(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase)))
            return false;

        TriggerWindowProcesses.Add(name);
        ApplyTriggerFilter();
        DebouncedSaveSettings();
        return true;
    }

    // ── 定时任务命令 ──

    /// <summary>添加定时任务 — 使用当前固定坐标和设置的延迟</summary>
    [RelayCommand]
    private void AddScheduledTask()
    {
        var task = new ScheduledClickTask
        {
            Label = NewTaskLabel,
            TargetX = TargetX,
            TargetY = TargetY,
            DelaySeconds = NewTaskDelayValue * GetTimeUnitMultiplier(),
            ClickMode = SelectedClickMode
        };
        _scheduledTaskService.Tasks.Add(task);

        // 清空输入
        NewTaskLabel = "";
        NewTaskDelayValue = 0;

        DebouncedSaveSettings();
    }

    /// <summary>获取当前所选时间单位的秒数换算倍率</summary>
    private int GetTimeUnitMultiplier() => SelectedTimeUnitIndex switch
    {
        1 => 60,    // 分钟
        2 => 3600,  // 小时
        _ => 1      // 秒
    };

    /// <summary>删除指定定时任务</summary>
    [RelayCommand]
    private void RemoveScheduledTask(ScheduledClickTask task)
    {
        _scheduledTaskService.Tasks.Remove(task);
        DebouncedSaveSettings();
    }

    /// <summary>开始执行定时任务</summary>
    [RelayCommand]
    private async Task StartScheduledTasks()
    {
        if (IsPaused)
        {
            StatusText = "已暂停，请按 F7 恢复";
            return;
        }

        if (IsScheduledRunning || _scheduledTaskService.Tasks.Count == 0) return;

        IsScheduledRunning = true;
        ScheduledStatusText = "准备执行...";

        using var cts = new CancellationTokenSource();
        await _scheduledTaskService.StartAsync(cts.Token);
    }

    /// <summary>停止定时任务</summary>
    [RelayCommand]
    private void StopScheduledTasks()
    {
        _scheduledTaskService.Stop();
    }

    /// <summary>打开计划列表窗口</summary>
    [RelayCommand]
    private void ShowPlanList()
    {
        ShowChildWindow(new PlanListView(this));
    }

    // ── 公共 API ──

    /// <summary>注册全局热键 F6-F10，注册失败时在状态栏提示</summary>
    public void RegisterHotkeys(IntPtr handle)
    {
        _hotkeyService.Handle = handle;

        var bindings = new[]
        {
            new HotkeyBinding(Key.F6, ModifierKeys.None, HotkeyStartId),
            new HotkeyBinding(Key.F7, ModifierKeys.None, HotkeyStopId),
            new HotkeyBinding(Key.F8, ModifierKeys.None, HotkeyRecordId),
            new HotkeyBinding(Key.F9, ModifierKeys.None, HotkeyPlayId),
            new HotkeyBinding(Key.F10, ModifierKeys.None, HotkeyLoopPlayId),
        };

        var failed = new List<string>();
        foreach (var b in bindings)
        {
            if (!_hotkeyService.Register(b))
                failed.Add(b.Key.ToString());
        }

        if (failed.Count > 0)
        {
            StatusText = $"热键 {string.Join(", ", failed)} 注册失败（可能被其他程序占用）";
        }
    }

    /// <summary>注销所有热键</summary>
    public void UnregisterHotkeys()
    {
        _hotkeyService.UnregisterAll();
    }

    /// <summary>WndProc 消息处理 — 转发 WM_HOTKEY 给 HotkeyService</summary>
    public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_hotkeyService is HotkeyService hs)
            return hs.WndProc(hwnd, msg, wParam, lParam, ref handled);
        return IntPtr.Zero;
    }

    /// <summary>保存当前设置</summary>
    public void SaveAll() => SaveSettings();

    /// <summary>停止连点并注销热键 — 退出流程使用，跳过保存（已提前保存）</summary>
    public void StopAndCleanup()
    {
        StopClicking();
        _loopCts?.Cancel();
        if (_recordingService.IsRecording)
            _currentRecording = _recordingService.StopRecording();
        if (_playerService.IsPlaying)
            _playerService.Stop();
        if (_keyboardTriggerService.IsEnabled)
            _keyboardTriggerService.Disable();
        if (_scheduledTaskService.IsRunning)
            _scheduledTaskService.Stop();
        UnregisterHotkeys();
    }

    /// <summary>窗口关闭时调用 — 停止连点、保存设置、注销热键</summary>
    public void OnClosing()
    {
        StopClicking();
        _loopCts?.Cancel();
        if (_recordingService.IsRecording)
            _currentRecording = _recordingService.StopRecording();
        if (_playerService.IsPlaying)
            _playerService.Stop();
        if (_scheduledTaskService.IsRunning)
            _scheduledTaskService.Stop();
        SaveSettings();
        UnregisterHotkeys();
    }

    /// <summary>会话恢复后重新注册热键</summary>
    public void OnSessionSwitch()
    {
        _hotkeyService.ReregisterAll();
    }

    /// <summary>进入总暂停状态 — 停止所有自动化并禁用键盘监听</summary>
    public void EnterPause(string? status = null)
    {
        if (IsPaused) return;

        // 记录暂停前的监听状态，恢复时只恢复监听
        _listenerWasEnabledBeforePause = KeyboardTriggerEnabled;

        StopClicking();
        _loopCts?.Cancel();
        if (_playerService.IsPlaying)
            _playerService.Stop();
        IsPlaying = false;
        IsLoopPlaying = false;

        if (_recordingService.IsRecording)
            _recordingService.StopRecording();
        IsRecording = false;

        // 如果正在录键，一并取消
        if (IsListeningForKey)
            _keyboardTriggerService.CancelRecording();

        if (_scheduledTaskService.IsRunning)
            _scheduledTaskService.Stop();

        if (KeyboardTriggerEnabled)
            KeyboardTriggerEnabled = false;

        IsPaused = true;
        StatusText = status ?? "已暂停（按 F7 恢复）";
    }

    /// <summary>退出总暂停状态 — 只恢复暂停前的键盘监听，不自动重启连点/任务</summary>
    public void ExitPause()
    {
        if (!IsPaused) return;

        IsPaused = false;

        if (_listenerWasEnabledBeforePause)
        {
            KeyboardTriggerEnabled = true;
            _listenerWasEnabledBeforePause = false;
        }

        StatusText = "已恢复";
    }

    /// <summary>关闭到托盘时按配置自动暂停</summary>
    public void PauseIfMinimizedToTray()
    {
        if (PauseWhenMinimizedToTray)
            EnterPause("已最小化到托盘，自动暂停");
    }

    /// <summary>锁屏时按配置自动暂停</summary>
    public void PauseIfLocked()
    {
        if (PauseWhenLocked)
            EnterPause("已锁屏，自动暂停");
    }

    // ── 点击模式变化时通知衍生属性 ──

    partial void OnSelectedClickModeChanged(ClickMode value)
    {
        OnPropertyChanged(nameof(SelectedClickModeDisplay));
        DebouncedSaveSettings();
    }

    // ── 间隔模式变化时通知衍生属性 ──

    partial void OnSelectedIntervalModeChanged(ClickIntervalMode value)
    {
        OnPropertyChanged(nameof(IsMsMode));
        OnPropertyChanged(nameof(IsCpsMode));
        OnPropertyChanged(nameof(IsHoldMode));
        DebouncedSaveSettings();
    }

    // ── 键盘触发属性变化时同步到服务 ──

    partial void OnKeyboardTriggerEnabledChanged(bool value)
    {
        if (value)
            _keyboardTriggerService.Enable();
        else
            _keyboardTriggerService.Disable();

        DebouncedSaveSettings();

        OnPropertyChanged(nameof(KeyboardListenerStateText));
        OnPropertyChanged(nameof(KeyboardTriggerToggleText));
        OnPropertyChanged(nameof(GlobalStateHint));
        OnPropertyChanged(nameof(TrayToolTip));
    }

    partial void OnIsPausedChanged(bool value)
    {
        OnPropertyChanged(nameof(GlobalStateText));
        OnPropertyChanged(nameof(GlobalStateHint));
        OnPropertyChanged(nameof(PauseAllButtonText));
        OnPropertyChanged(nameof(CanRecordTriggerKey));
        OnPropertyChanged(nameof(TrayToolTip));
    }

    partial void OnAutoResumeListeningOnStartupChanged(bool value) => DebouncedSaveSettings();

    partial void OnTriggerWindowFilterEnabledChanged(bool value)
    {
        ApplyTriggerFilter();
        DebouncedSaveSettings();
    }

    partial void OnPauseWhenMinimizedToTrayChanged(bool value) => DebouncedSaveSettings();

    partial void OnPauseWhenLockedChanged(bool value) => DebouncedSaveSettings();

    /// <summary>将当前前台过滤配置同步给键盘钩子服务</summary>
    private void ApplyTriggerFilter()
    {
        _keyboardTriggerService.SetForegroundProcessFilter(
            TriggerWindowFilterEnabled, TriggerWindowProcesses);
    }

    /// <summary>显示子窗口并保持引用，关闭后自动移除</summary>
    private void ShowChildWindow(Window window)
    {
        window.Owner = Application.Current.MainWindow;
        window.Closed += (_, _) => _childWindows.Remove(window);
        _childWindows.Add(window);
        window.Show();
    }

    partial void OnNewTaskDelayValueChanged(int value)
    {
        OnPropertyChanged(nameof(NewTaskDelayDisplay));
    }

    partial void OnSelectedTimeUnitIndexChanged(int value)
    {
        OnPropertyChanged(nameof(NewTaskDelayDisplay));
    }

    // ── 私有辅助方法 ──

    /// <summary>启动连点循环</summary>
    private async Task StartClickingAsync()
    {
        _appCts = new CancellationTokenSource();
        IsRunning = true;
        StatusText = "运行中";

        var clickCycle = CreateClickCycle();
        int intervalMs = GetEffectiveDelayMs();
        await _clickScheduler.StartAsync(clickCycle, intervalMs, _appCts.Token);
    }

    /// <summary>停止连点循环</summary>
    private void StopClicking()
    {
        _appCts?.Cancel();
        _clickScheduler.Stop();
        _appCts?.Dispose();
        _appCts = null;
        IsRunning = false;
        StatusText = "已停止";
    }

    /// <summary>构建单次点击周期函数 — 根据当前配置组装移动+点击+等待逻辑</summary>
    private Func<CancellationToken, Task> CreateClickCycle()
    {
        var clickMode = SelectedClickMode;
        var intervalMode = SelectedIntervalMode;
        int holdMs = HoldDurationMs;
        int delayMs = GetEffectiveDelayMs();
        int doubleClickGapMs = (int)AutoClicker.App.Interop.NativeMethods.GetDoubleClickTime();
        bool fixedPos = UseFixedPosition;
        int tgtX = TargetX;
        int tgtY = TargetY;

        return async (ct) =>
        {
            // 固定坐标模式：先移动鼠标到目标位置
            if (fixedPos)
                Application.Current.Dispatcher.Invoke(() =>
                    _mouseSimulator.MoveTo(tgtX, tgtY));

            // 根据间隔模式执行点击
            if (intervalMode == ClickIntervalMode.HoldDuration)
            {
                // 长按：按下 → 保持 → 释放
                Application.Current.Dispatcher.Invoke(() =>
                    _mouseSimulator.Down(clickMode));
                await WaitAsync(holdMs, ct);
                Application.Current.Dispatcher.Invoke(() =>
                    _mouseSimulator.Up(clickMode));
            }
            else if (clickMode == ClickMode.Double)
            {
                // 双击：两组快速按下/释放
                Application.Current.Dispatcher.Invoke(() =>
                    _mouseSimulator.Click(ClickMode.Single));
                await Task.Delay(doubleClickGapMs, ct);
                Application.Current.Dispatcher.Invoke(() =>
                    _mouseSimulator.Click(ClickMode.Single));
            }
            else
            {
                // 普通单击
                Application.Current.Dispatcher.Invoke(() =>
                    _mouseSimulator.Click(clickMode));
            }
        };
    }

    /// <summary>根据间隔模式计算有效延迟毫秒数</summary>
    private int GetEffectiveDelayMs()
    {
        return SelectedIntervalMode switch
        {
            ClickIntervalMode.Ms => Math.Clamp(IntervalMs, MinMs, MaxMs),
            ClickIntervalMode.Cps => 1000 / Math.Clamp(Cps, MinCps, MaxCps),
            ClickIntervalMode.HoldDuration => 1, // 长按模式：间隔最小，保持时间主导节奏
            _ => 100
        };
    }

    /// <summary>
    /// 高精度等待 — 用 Stopwatch 绝对时点计时，不受 CPU 频率波动影响
    /// </summary>
    private static async Task WaitAsync(int milliseconds, CancellationToken ct)
    {
        if (milliseconds <= 0) return;

        double ticksPerMs = Stopwatch.Frequency / 1000.0;

        if (milliseconds < 15)
        {
            long targetTicks = Stopwatch.GetTimestamp() + (long)(milliseconds * ticksPerMs);
            while (Stopwatch.GetTimestamp() < targetTicks && !ct.IsCancellationRequested)
            {
                long remaining = targetTicks - Stopwatch.GetTimestamp();
                if (remaining > (long)(2 * ticksPerMs))
                    Thread.Sleep(0);        // 剩余 >2ms 让出 CPU 时间片
                else
                    Thread.SpinWait(50);     // 最后 2ms 内自旋
            }
        }
        else
        {
            await Task.Delay(milliseconds, ct);
        }
    }

    /// <summary>热键按下事件处理</summary>
    private void OnHotkeyPressed(object? sender, int id)
    {
        // 录键期间忽略所有全局热键，确保按键能被干净地录制（F6–F10 会被提示拒绝）
        if (IsListeningForKey)
            return;

        // F7：总暂停 / 恢复（任何状态下都可用）
        if (id == HotkeyStopId)
        {
            TogglePause();
            return;
        }

        // 暂停期间只有 F7 生效
        if (IsPaused)
        {
            StatusText = "已暂停，请按 F7 恢复";
            return;
        }

        // F6：开始/停止切换（与主按钮一致）
        if (id == HotkeyStartId)
            _ = ToggleClicking();
        else if (id == HotkeyRecordId && !IsPlaying)
            ToggleRecording();
        else if (id == HotkeyPlayId && HasRecording && !IsRecording && !IsPlaying && !IsLoopPlaying)
            _ = PlayRecording();
        else if (id == HotkeyLoopPlayId && HasRecording && !IsRecording)
            _ = LoopPlayRecording();
    }

    /// <summary>从持久化文件加载上次录制</summary>
    private void LoadRecording()
    {
        if (!File.Exists(RecordingPath))
            return;

        try
        {
            var json = File.ReadAllText(RecordingPath);
            _currentRecording = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.RecordingSession);
            if (_currentRecording is not null && _currentRecording.Events.Count > 0)
            {
                HasRecording = true;
                RecordingEventCount = _currentRecording.Events.Count;
                RecordingDuration = $"{_currentRecording.TotalDurationMs / 1000:F1} 秒";
            }
        }
        catch { }
    }

    /// <summary>从持久化文件加载设置</summary>
    private void LoadSettings()
    {
        _settingsService.Load();
        var s = _settingsService.Settings;

        SelectedClickMode = s.ClickMode;
        SelectedIntervalMode = s.IntervalMode;
        IntervalMs = s.IntervalMs;
        Cps = s.Cps;
        HoldDurationMs = s.HoldDurationMs;
        UseFixedPosition = s.UseFixedPosition;
        TargetX = s.TargetX;
        TargetY = s.TargetY;
        RepeatCount = s.RepeatCount > 0 ? s.RepeatCount : 1;

        // ── 键盘触发 / 总开关设置 ──
        AutoResumeListeningOnStartup = s.AutoResumeListeningOnStartup;
        TriggerWindowFilterEnabled = s.TriggerWindowFilterEnabled;

        TriggerWindowProcesses.Clear();
        if (s.TriggerWindowProcesses is { Count: > 0 })
        {
            foreach (var process in s.TriggerWindowProcesses)
            {
                if (!string.IsNullOrWhiteSpace(process))
                    TriggerWindowProcesses.Add(process);
            }
        }

        PauseWhenMinimizedToTray = s.PauseWhenMinimizedToTray;
        PauseWhenLocked = s.PauseWhenLocked;

        // 先应用前台过滤，再决定是否启用监听
        ApplyTriggerFilter();

        // 安全策略：默认启动即暂停；只有勾选"启动时自动恢复监听"才恢复上次的监听状态
        IsPaused = !s.AutoResumeListeningOnStartup;
        KeyboardTriggerEnabled = s.AutoResumeListeningOnStartup && s.KeyboardTriggerEnabled;
        StatusText = IsPaused ? "已暂停（按 F7 恢复）" : "已停止";

        // 加载快捷键触发绑定列表（含旧版单键迁移）
        var triggerBindings = s.TriggerBindings ?? new List<TriggerBinding>();
        if (triggerBindings.Count == 0 && s.TriggerVkCode > 0)
        {
            // 旧版单键绑定 → 新版多绑定列表迁移
            triggerBindings.Add(new TriggerBinding
            {
                VkCode = (uint)s.TriggerVkCode,
                TargetX = s.TargetX,
                TargetY = s.TargetY,
                ClickMode = s.ClickMode
            });
            s.TriggerVkCode = 0; // 清除旧字段，防止重复迁移
        }
        _keyboardTriggerService.SetBindings(triggerBindings);
        OnPropertyChanged(nameof(TriggerBindings));
        OnPropertyChanged(nameof(TriggerKeyDisplay));

        // 加载定时任务列表
        _scheduledTaskService.Tasks.Clear();
        if (s.ScheduledTasks is { Count: > 0 })
        {
            foreach (var task in s.ScheduledTasks)
                _scheduledTaskService.Tasks.Add(task);
        }
    }

    private int _recordingCount;

    /// <summary>录制事件捕获回调 — 更新实时计数和时长（钩子线程回调，需调度到 UI 线程）</summary>
    private void OnEventCaptured(object? sender, RecordedMouseEvent e)
    {
        // 钩子回调在 UI 线程上直接执行，安全更新 UI
        _recordingCount++;
        RecordingEventCount = _recordingCount;
        RecordingDuration = $"{e.TimestampMs / 1000:F1} 秒";
    }

    /// <summary>录制停止回调（手动停止或达到上限自动停止）— 同步 UI 状态并保存录制</summary>
    private void OnRecordingStopped(object? sender, RecordingSession? session)
    {
        IsRecording = false;

        if (session is { Events.Count: > 0 })
        {
            _currentRecording = session;
            HasRecording = true;
            RecordingEventCount = session.Events.Count;
            RecordingDuration = $"{session.TotalDurationMs / 1000:F1} 秒";
            SaveRecordingToFile(session);
        }
    }

    /// <summary>将录制数据保存到 JSON 文件</summary>
    private void SaveRecordingToFile(RecordingSession session)
    {
        var dir = Path.GetDirectoryName(RecordingPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(session, AppJsonSerializerContext.Default.RecordingSession);
        File.WriteAllText(RecordingPath, json);
    }

    /// <summary>回放完成回调</summary>
    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        IsPlaying = false;
    }

    /// <summary>键盘触发键按下回调 — 直接向目标窗口投递点击消息，不移动光标</summary>
    private void OnTriggerKeyPressed(object? sender, TriggerBinding binding)
    {
        if (_isPickingCoordinate) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            _mouseSimulator.PostClickAt(binding.TargetX, binding.TargetY, binding.ClickMode);
        });
    }

    // ── 定时任务事件处理 ──

    private void OnScheduledTaskStarted(object? sender, int index)
    {
        var task = _scheduledTaskService.Tasks[index];
        ScheduledStatusText = $"执行中: {index + 1}/{_scheduledTaskService.Tasks.Count} — {(string.IsNullOrEmpty(task.Label) ? "未命名" : task.Label)}";
    }

    private void OnScheduledTaskCompleted(object? sender, int index)
    {
        ScheduledStatusText = $"已完成: {index + 1}/{_scheduledTaskService.Tasks.Count}";
    }

    private void OnScheduledAllCompleted(object? sender, EventArgs e)
    {
        IsScheduledRunning = false;
        ScheduledStatusText = "全部完成";
    }

    private void OnScheduledStopped(object? sender, EventArgs e)
    {
        IsScheduledRunning = false;
        ScheduledStatusText = "已停止";
    }

    /// <summary>防抖保存 — 设置变更 500ms 后自动写入磁盘，频繁修改只写一次</summary>
    private void DebouncedSaveSettings()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts = new CancellationTokenSource();
        var token = _saveDebounceCts.Token;
        _ = Task.Delay(500, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested)
                Application.Current.Dispatcher.Invoke(SaveSettings);
        }, TaskContinuationOptions.NotOnCanceled);
    }

    /// <summary>将当前设置保存到持久化文件</summary>
    private void SaveSettings()
    {
        var s = _settingsService.Settings;
        s.ClickMode = SelectedClickMode;
        s.IntervalMode = SelectedIntervalMode;
        s.IntervalMs = IntervalMs;
        s.Cps = Cps;
        s.HoldDurationMs = HoldDurationMs;
        s.UseFixedPosition = UseFixedPosition;
        s.TargetX = TargetX;
        s.TargetY = TargetY;
        s.RepeatCount = RepeatCount;

        // 暂停状态下不覆盖"上次监听偏好"：临时停用不代表用户不想在下次启动时恢复
        if (!IsPaused)
            s.KeyboardTriggerEnabled = KeyboardTriggerEnabled;

        s.AutoResumeListeningOnStartup = AutoResumeListeningOnStartup;
        s.TriggerWindowFilterEnabled = TriggerWindowFilterEnabled;
        s.TriggerWindowProcesses = TriggerWindowProcesses.ToList();
        s.PauseWhenMinimizedToTray = PauseWhenMinimizedToTray;
        s.PauseWhenLocked = PauseWhenLocked;
        s.TriggerBindings = new List<TriggerBinding>(_keyboardTriggerService.Bindings);
        s.ScheduledTasks = new List<ScheduledClickTask>(_scheduledTaskService.Tasks);
        _settingsService.Save();
    }
}
