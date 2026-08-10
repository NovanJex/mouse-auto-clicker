<p align="center">
  <img src="src/AutoClicker.App/Resources/Icons/app.ico" width="80" alt="Logo">
</p>

<h1 align="center">鼠标连点器 (MouseAutoClicker)</h1>

<p align="center">
  <b>中文</b> | <a href="README_EN.md">English</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-1.2.3-blue" alt="Version">
  <img src="https://img.shields.io/badge/.NET-8.0-purple" alt=".NET">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-lightgrey" alt="Platform">
  <img src="https://img.shields.io/badge/license-MIT-green" alt="License">
  <img src="https://img.shields.io/badge/language-C%23%2012-orange" alt="Language">
</p>

---

Windows PC 端鼠标自动连点器，支持自定义坐标点击、鼠标宏录制与回放、多键位绑定触发、定时点击任务、全局热键启停、多种点击模式和可调节的点击间隔。

基于 **C# 12 / .NET 8 + WPF**，遵循 **MVVM 架构模式**，**零第三方 UI 库依赖**。

---

## 📥 下载

前往 [Releases](https://github.com/NovanJex/mouse-auto-clicker/releases) 页面下载最新版本：

| 文件 | 类型 | 大小 |
| ---- | ---- | ---- |
| `MouseAutoClicker_v1.2.3.exe` | 自包含单文件（无需安装运行时，双击即用） | ~155 MB |
| `MouseAutoClicker_Setup_v1.2.3.exe` | Inno Setup 安装包（自动安装 .NET 8 运行时） | ~2.3 MB |

> **推荐**: 首次使用请下载 **安装包**（`Setup_`），自动检测并安装 .NET 8 Desktop Runtime。

## ✨ 功能特性

### 自动连点

- **多种点击模式**：左键单击 / 左键双击 / 右键单击 / 中键（滚轮）单击
- **坐标控制**：当前光标位置点击 / 固定屏幕坐标点击（全屏覆盖层选取）
- **间隔模式**：毫秒 / CPS（每秒点击数） / 长按持续
- **全局热键**：F6 开始连点，F7 停止连点（系统级，支持后台触发）
- **高精度定时**：<15ms 用 SpinWait 自旋，>=15ms 用 Task.Delay 异步
- **多显示器支持**：0~65535 归一化坐标映射虚拟桌面

### 多键位绑定触发（v1.2.0 新增）

- **多键位→多位置**：不同键盘按键绑定到不同屏幕坐标 + 不同点击模式
- **绑定管理弹窗**：TriggerBindListView 弹出窗口集中管理所有绑定，支持增删
- **无光标点击**：通过 `WindowFromPoint` + `PostMessage` 直接向目标窗口投递鼠标消息，物理光标完全不移动，根除闪烁/瞬移问题
- **一键录键**：点击"录键"按钮后按下目标键即可完成绑定
- **全局生效**：基于 `WH_KEYBOARD_LL` 底层键盘钩子，后台也能触发
- **防误触保护**：坐标选取覆盖层弹出时自动抑制触发；自动过滤注入事件和重复按下

### 定时点击任务（v1.2.0 新增）

- **命名任务**：为每个定时任务设置标签，方便识别
- **灵活定时**：支持秒/分/小时三种时间单位
- **固定坐标执行**：到时间后在指定坐标执行一次点击（无光标移动）
- **任务列表管理**：PlanListView 弹出窗口管理所有定时任务
- **独立执行**：Sequential 顺序执行器，与手动连点互不干扰

### 鼠标宏录制与回放

- **全局录制**：F8 开始/停止录制，通过 `WH_MOUSE_LL` 钩子捕获系统级鼠标事件
- **单次回放**：F9 播放录制，按原始时间戳和屏幕坐标精确重现点击
- **循环回放**：F10 循环播放，可自定义重复次数（1–999），支持中途停止
- **事件过滤**：自动过滤 SendInput 注入事件，避免回放反馈循环
- **录制持久化**：录制数据保存为 JSON（`%LocalAppData%/AutoClicker/recording.json`）

### 系统集成

- **系统托盘**：托盘图标常驻（Shell_NotifyIcon），右键菜单（显示窗口 / 退出）
- **关闭行为**：点击 X 弹出关闭行为对话框，选择"退出应用"或"最小化到托盘"
- **记住选择**：关闭行为支持"记住我的选择，下次不再提示"
- **配置持久化**：所有设置自动保存到 `%LocalAppData%/AutoClicker/settings.json`（System.Text.Json 源生成器，零反射）
- **单实例检测**：Mutex 防止多开
- **会话恢复**：锁屏/休眠后监听 SessionSwitch 自动重新注册热键

## ⌨️ 快捷键

| 按键 | 功能 |
|------|------|
| `F6` | 开始 / 停止自动连点 |
| `F7` | 保留 |
| `F8` | 开始/停止鼠标宏录制 |
| `F9` | 播放录制（单次） / 停止回放 |
| `F10` | 循环播放（按设定次数） / 停止循环 |
| `ESC` | 取消坐标选取 / 关闭对话框 |
| `自定义` | 键盘触发点击（支持多键位绑定，通过弹出窗口管理） |

## 🛠️ 技术栈

| 技术 | 说明 |
|------|------|
| C# 12 / .NET 8 + WPF | 语言与框架 |
| CommunityToolkit.Mvvm 8.x | MVVM 源生成器（[ObservableProperty], [RelayCommand]） |
| Microsoft.Extensions.DependencyInjection | 依赖注入容器 |
| System.Text.Json 源生成器 | 零反射 JSON 序列化 |
| Win32 P/Invoke | SendInput / RegisterHotKey / Shell_NotifyIcon / SetWindowsHookEx / WH_KEYBOARD_LL / WH_MOUSE_LL / PostMessage / WindowFromPoint |

**无第三方 UI 库依赖** — 系统托盘、全局钩子、消息投递均通过原生 Win32 API 实现。

## 📂 项目结构

```
AutoClicker/
├── AutoClicker.sln
├── global.json                                 # SDK 版本锁定 8.0.421
├── README.md
├── installer.iss                               # Inno Setup 安装脚本（含 .NET 8 运行时自动下载）
└── src/AutoClicker.App/
    ├── AutoClicker.App.csproj                  # net8.0-windows, win-x64, PublishSingleFile
    ├── App.xaml / App.xaml.cs                  # 启动入口 / DI 容器 / 全局样式 / 生命周期
    ├── app.manifest                            # PerMonitorV2 DPI / asInvoker / Windows 10+
    ├── Interop/                                # Win32 P/Invoke 封装（3 个文件）
    ├── Models/                                 # 数据模型（9 个文件）
    ├── Services/
    │   ├── Interfaces/                         # 10 个服务接口
    │   └── Implementation/                     # 9 个服务实现
    ├── ViewModels/                             # 核心 ViewModel（~850 行）
    ├── Views/                                  # 5 个窗口/弹窗
    └── Resources/Icons/
        └── app.ico
```

## 🚀 构建与运行

### 环境要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（≥ 8.0.421）
- Windows 10/11 x64

### 构建

```bash
git clone https://github.com/NovanJex/mouse-auto-clicker.git
cd auto-clicker
dotnet build -c Release
```

### 发布

```bash
# 框架依赖单文件（需安装 .NET 8 Desktop Runtime，约 700 KB）
dotnet publish -c Release -o ./publish/

# 自包含单文件（无需运行时，约 150 MB）
dotnet publish -c Release -o ./publish/ -p:SelfContained=true

# 打包安装程序（需要 Inno Setup）
ISCC.exe installer.iss
```

## 📝 版本历史

| 版本 | 日期 | 主要变更 |
| ---- | ---- | -------- |
| v1.2.3 | 2026-07-26 | 设置即时自动保存、修复自包含单文件启动崩溃 |
| v1.2.2 | 2026-07-22 | 修复定时漂移（越跑越快）、英文文件名、构建优化 |
| v1.2.1 | 2026-07-07 | 无光标点击（PostClickAt）、Inno Setup 安装包 |
| v1.2.0 | 2026-07-06 | 多键位绑定触发、定时点击任务 |
| v1.1.0 | 2026-06-21 | 鼠标宏录制/回放、单个键盘触发键 |
| v1.0.0 | 2026-05-22 | 基础连点、坐标选取、系统托盘、配置持久化 |

详见 [CHANGELOG.md](CHANGELOG.md)

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！详见 [CONTRIBUTING.md](CONTRIBUTING.md)

## 📄 开源协议

本项目采用 [MIT License](LICENSE) — 可自由使用、修改和分发。

---

## ⭐ Star History

如果你觉得这个项目有用，请给一个 Star ⭐ 支持一下！

[![Star History Chart](https://api.star-history.com/chart?repos=NovanJex/mouse-auto-clicker&type=date&legend=top-left&sealed_token=MJ66g8sESboWT3JvEMBJgeAEmsePLJbB7sWTdA9Illg6dPvB-JX1agP_3veX0F_Pwl6WX9nnUjiiwkDp0KEltUw5kkzlQxhPBF6xPeb3sRSOesd2P6iDJP_7MkN4wNObhV2AYoa_tIQgwokWGtv5164ErWQ4h2U2Nu7gkhDarFnNzl32eFLneWcrLh7A)](https://www.star-history.com/?repos=NovanJex%2Fmouse-auto-clicker&type=date&legend=top-left)
