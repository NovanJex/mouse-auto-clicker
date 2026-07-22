# 更新日志

本文档记录鼠标连点器所有重要变更。

## [v1.2.2] - 2025-07-22

### 修复
- **定时漂移修复**：连点器长时间运行后越跑越快的问题已修复
  - 调度器改为固定间隔周期校正（每个周期从起点开始计时，等待剩余时长）
  - 高精度等待改用 `Stopwatch.GetTimestamp` 绝对时点比较，彻底消除 CPU 频率波动影响

### 改进
- 发布文件名统一使用英文 `MouseAutoClicker_` 前缀（GitHub Release 兼容）
- 自包含版本优化（`InvariantGlobalization` + `UseSystemResourceKeys`）
- README 下载说明更新为三版本

## [v1.2.1] - 2025-07-07

### 新增
- **无光标点击**：通过 `WindowFromPoint` + `PostMessage` 直接投递鼠标消息，物理光标完全不移动，根除闪烁/瞬移问题
- **Inno Setup 安装包**：支持中文安装界面，自动检测并下载安装 .NET 8 Desktop Runtime

### 改进
- 键位绑定系统完善：弹出管理窗口（TriggerBindListView）支持查看和删除所有绑定
- 优化钩子性能：注入事件过滤 + 自动重复抑制
- 安装包 LZMA2/ultra64 极限压缩

### 修复
- 修复键盘触发时光标可见移动的问题（PostClickAt 方案彻底解决）

---

## [v1.2.0] - 2025-07-06

### 新增
- **多键位绑定触发**：不同键盘按键绑定到不同屏幕坐标 + 不同点击模式
- **WH_KEYBOARD_LL 全局键盘钩子**：系统级按键监听，后台也能触发
- **定时点击任务**：创建命名任务，支持秒/分/小时延迟，顺序执行
- **TriggerBindListView 弹窗**：管理键位绑定列表
- **PlanListView 弹窗**：管理定时任务列表
- 按键录制功能：临时钩子捕获任意键盘按键

### 新增模型
- `TriggerBinding`：键位触发绑定数据模型
- `ScheduledClickTask`：定时任务数据模型
- `TimeUnit`：时间单位枚举

---

## [v1.1.0] - 2025-06-21

### 新增
- **鼠标宏录制**：F8 全局录制（WH_MOUSE_LL），高精度时间戳
- **鼠标宏回放**：F9 单次回放，F10 循环回放（1-999 次）
- **录制持久化**：录制数据保存为 JSON 到 `%LocalAppData%/AutoClicker/recording.json`
- **循环播放次数输入**：仅允许纯数字，非法输入自动恢复
- **键盘触发点击**：自定义按键 + 固定坐标模式，按下即点击
- **WH_KEYBOARD_LL 钩子**（初版，单一按键触发）

### 新增服务
- `MouseRecordingService`：WH_MOUSE_LL 录制引擎
- `RecordingPlayerService`：时间戳精确回放引擎
- `KeyboardTriggerService`：键盘钩子服务

---

## [v1.0.0] - 2025-05-22

### 初始发布
- **自动连点引擎**：左键/右键/中键/双击模式，毫秒/CPS/长按间隔
- **坐标选取器**：全屏半透明覆盖层，点击获取屏幕坐标
- **全局热键**：F6 开始、F7 停止（RegisterHotKey + MOD_NOREPEAT）
- **系统托盘**：Shell_NotifyIcon 原生托盘，右键菜单，关闭到托盘
- **配置持久化**：System.Text.Json 源生成器，保存到 %LocalAppData%
- **单实例检测**：Mutex 防止多开
- **会话恢复**：锁屏/休眠后自动重新注册热键
- **高精度定时**：timeBeginPeriod(1) + SpinWait/Task.Delay 自适应
- **多显示器支持**：0-65535 归一化坐标映射虚拟桌面
