using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private RecordingSession? _currentRecording;
    private bool _isPickingCoordinate;
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

    /// <summary>当前所有快捷键绑定的只读列表（来自服务层）</summary>
    public IReadOnlyList<TriggerBinding> TriggerBindings => _keyboardTriggerService.Bindings;

    /// <summary>触发键的可读显示名称（显示最新录制的按键，或"未绑定"）</summary>
    public string TriggerKeyDisplay =>
        _keyboardTriggerService.Bindings.Count > 0
            ? _keyboardTriggerService.Bindings[^1].KeyDisplay
            : "未绑定";

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
        if (IsPlaying) return;

        if (_recordingService.IsRecording)
        {
            _currentRecording = _recordingService.StopRecording();
            IsRecording = false;

            if (_currentRecording is not null && _currentRecording.Events.Count > 0)
            {
                HasRecording = true;
                RecordingEventCount = _currentRecording.Events.Count;
                RecordingDuration = $"{_currentRecording.TotalDurationMs / 1000:F1} 秒";
                SaveRecordingToFile(_currentRecording);
            }
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
        if (_playerService.IsPlaying)
        {
            _loopCts?.Cancel();
            _playerService.Stop();
            return;
        }

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
        KeyboardTriggerEnabled = !KeyboardTriggerEnabled;
    }

    /// <summary>录制触发按键 — 录键后使用当前坐标和点击模式创建绑定</summary>
    [RelayCommand]
    private async Task RecordTriggerKey()
    {
        if (!UseFixedPosition) return;

        IsListeningForKey = true;
        try
        {
            uint vkCode = await _keyboardTriggerService.RecordKeyAsync();
            if (vkCode > 0)
            {
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
            }
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
        var window = new TriggerBindListView(this) { Owner = Application.Current.MainWindow };
        window.Show();
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
    }

    /// <summary>开始执行定时任务</summary>
    [RelayCommand]
    private async Task StartScheduledTasks()
    {
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
        var window = new PlanListView(this) { Owner = Application.Current.MainWindow };
        window.Show();
    }

    // ── 公共 API ──

    /// <summary>注册全局热键 F6（开始）和 F7（停止）</summary>
    public void RegisterHotkeys(IntPtr handle)
    {
        _hotkeyService.Handle = handle;

        _hotkeyService.Register(new HotkeyBinding(Key.F6, ModifierKeys.None, HotkeyStartId));
        _hotkeyService.Register(new HotkeyBinding(Key.F7, ModifierKeys.None, HotkeyStopId));
        _hotkeyService.Register(new HotkeyBinding(Key.F8, ModifierKeys.None, HotkeyRecordId));
        _hotkeyService.Register(new HotkeyBinding(Key.F9, ModifierKeys.None, HotkeyPlayId));
        _hotkeyService.Register(new HotkeyBinding(Key.F10, ModifierKeys.None, HotkeyLoopPlayId));
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

    // ── 点击模式变化时通知衍生属性 ──

    partial void OnSelectedClickModeChanged(ClickMode value)
    {
        OnPropertyChanged(nameof(SelectedClickModeDisplay));
    }

    // ── 间隔模式变化时通知衍生属性 ──

    partial void OnSelectedIntervalModeChanged(ClickIntervalMode value)
    {
        OnPropertyChanged(nameof(IsMsMode));
        OnPropertyChanged(nameof(IsCpsMode));
        OnPropertyChanged(nameof(IsHoldMode));
    }

    // ── 键盘触发属性变化时同步到服务 ──

    partial void OnKeyboardTriggerEnabledChanged(bool value)
    {
        if (value)
            _keyboardTriggerService.Enable();
        else
            _keyboardTriggerService.Disable();
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
        await _clickScheduler.StartAsync(clickCycle, _appCts.Token);
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

            // 等待下一次循环
            await WaitAsync(delayMs, ct);
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
    /// 高精度等待 — &lt;15ms 使用 SpinWait 自旋，&gt;=15ms 使用 Task.Delay 异步延迟
    /// </summary>
    private static async Task WaitAsync(int milliseconds, CancellationToken ct)
    {
        if (milliseconds < 15)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < milliseconds && !ct.IsCancellationRequested)
                Thread.SpinWait(100);
        }
        else
        {
            await Task.Delay(milliseconds, ct);
        }
    }

    /// <summary>热键按下事件处理</summary>
    private void OnHotkeyPressed(object? sender, int id)
    {
        if (id == HotkeyStartId && !IsRunning)
            _ = StartClickingAsync();
        else if (id == HotkeyStopId && IsRunning)
            StopClicking();
        else if (id == HotkeyRecordId && !IsPlaying)
            ToggleRecording();
        else if (id == HotkeyPlayId && HasRecording && !IsRecording && !IsPlaying && !IsLoopPlaying)
            _ = PlayRecording();
        else if (id == HotkeyLoopPlayId && HasRecording && !IsRecording && !IsPlaying)
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
        KeyboardTriggerEnabled = s.KeyboardTriggerEnabled;

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
        s.KeyboardTriggerEnabled = KeyboardTriggerEnabled;
        s.TriggerBindings = new List<TriggerBinding>(_keyboardTriggerService.Bindings);
        s.ScheduledTasks = new List<ScheduledClickTask>(_scheduledTaskService.Tasks);
        _settingsService.Save();
    }
}
