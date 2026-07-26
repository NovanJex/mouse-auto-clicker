<p align="center">
  <img src="src/AutoClicker.App/Resources/Icons/app.ico" width="80" alt="Logo">
</p>

<h1 align="center">MouseAutoClicker (鼠标连点器)</h1>

<p align="center">
  <img src="https://img.shields.io/badge/version-1.2.3-blue" alt="Version">
  <img src="https://img.shields.io/badge/.NET-8.0-purple" alt=".NET">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-lightgrey" alt="Platform">
  <img src="https://img.shields.io/badge/license-MIT-green" alt="License">
  <img src="https://img.shields.io/badge/language-C%23%2012-orange" alt="Language">
</p>

<p align="center">
  <a href="README.md">中文</a> | <b>English</b>
</p>

---

A Windows desktop auto-clicker with multi-key trigger bindings, cursorless clicking, macro recording/playback, scheduled tasks, global hotkeys, and system tray integration.

Built with **C# 12 / .NET 8 + WPF** following the **MVVM architecture**, with **zero third-party UI library dependencies**.

---

## 📥 Download

Go to [Releases](https://github.com/NovanJex/mouse-auto-clicker/releases) to download the latest version:

| File | Type | Size |
| ---- | ---- | ---- |
| `MouseAutoClicker_v1.2.3.exe` | Self-contained (no runtime needed) | ~155 MB |
| `MouseAutoClicker_Setup_v1.2.3.exe` | Inno Setup installer (auto-installs .NET 8) | ~2.3 MB |

> **Recommended**: First-time users should use the **installer** (`Setup_`), which automatically detects and installs .NET 8 Desktop Runtime.

## ✨ Features

### Auto-Clicking

- **Click modes**: Left single click / Left double click / Right click / Middle (scroll wheel) click
- **Position modes**: Follow cursor / Fixed screen coordinates (fullscreen overlay picker)
- **Interval modes**: Milliseconds / CPS (clicks per second) / Hold duration
- **Global hotkeys**: F6 to start, F7 to stop (system-level, works in background)
- **High-precision timing**: SpinWait for <15ms, Task.Delay for >=15ms
- **Multi-monitor support**: 0~65535 normalized coordinate mapping across virtual desktop

### Multi-Key Trigger Bindings (v1.2.0)

- **Multiple keys → multiple targets**: Bind different keyboard keys to different screen coordinates + click modes
- **Binding management**: TriggerBindListView popup for centralized management with add/delete
- **Cursorless clicking**: Clicks delivered via `WindowFromPoint` + `PostMessage` directly to target window, physical cursor never moves
- **One-click key recording**: Press "Record Key" then press a key to bind it
- **Global scope**: `WH_KEYBOARD_LL` low-level keyboard hook, works while app is in background
- **Anti-mistrigger protection**: Trigger suppressed while coordinate picker is active; injected events and auto-repeat automatically filtered

### Scheduled Click Tasks (v1.2.0)

- **Named tasks**: Label each scheduled task for easy identification
- **Flexible timing**: Support seconds/minutes/hours
- **Cursorless execution**: Clicks at fixed coordinates without moving the cursor
- **Task management**: PlanListView popup for managing all tasks
- **Independent execution**: Sequential executor runs independently from manual clicking

### Mouse Macro Recording & Playback

- **Global recording**: F8 to start/stop, `WH_MOUSE_LL` hook captures system-level mouse events
- **Single playback**: F9 to replay with exact original timing and screen coordinates
- **Loop playback**: F10 to loop, configurable 1–999 repetitions, cancellable mid-playback
- **Event filtering**: Injected events (SendInput) automatically filtered to prevent feedback loops
- **Persistence**: Recording saved as JSON at `%LocalAppData%/AutoClicker/recording.json`

### System Integration

- **System tray**: Tray icon with tooltip and right-click menu (Show / Exit)
- **Close behavior**: Confirmation dialog on close with "Exit" / "Minimize to tray" / "Remember choice"
- **Settings persistence**: Auto-save to `%LocalAppData%/AutoClicker/settings.json` (System.Text.Json source generator, zero reflection)
- **Single instance**: Mutex prevents multiple instances
- **Session recovery**: Hotkeys auto-re-register after lock/sleep via SessionSwitch

## ⌨️ Hotkeys

| Key | Action |
|-----|--------|
| `F6` | Start / Stop auto-clicking |
| `F7` | Reserved |
| `F8` | Start/Stop macro recording |
| `F9` | Play recording (once) / Stop playback |
| `F10` | Loop playback / Stop loop |
| `ESC` | Cancel coordinate picker / Close dialog |
| `Custom` | Keyboard trigger click (multi-key binding via popup) |

## 🛠️ Tech Stack

| Technology | Description |
|------------|-------------|
| C# 12 / .NET 8 + WPF | Language & framework |
| CommunityToolkit.Mvvm 8.x | MVVM source generators |
| Microsoft.Extensions.DependencyInjection | Dependency injection |
| System.Text.Json source gen | Zero-reflection JSON serialization |
| Win32 P/Invoke | SendInput / RegisterHotKey / Shell_NotifyIcon / SetWindowsHookEx / WH_KEYBOARD_LL / WH_MOUSE_LL / PostMessage / WindowFromPoint |

**Zero third-party UI libraries** — system tray, global hooks, and message delivery all via native Win32 API.

## 🚀 Build & Run

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (≥ 8.0.421)
- Windows 10/11 x64

### Build

```bash
git clone https://github.com/NovanJex/mouse-auto-clicker.git
cd mouse-auto-clicker
dotnet build -c Release
```

### Publish

```bash
# Framework-dependent single file (requires .NET 8 Desktop Runtime, ~700 KB)
dotnet publish src/AutoClicker.App/AutoClicker.App.csproj -c Release -o ./publish/

# Self-contained single file (no runtime needed, ~155 MB)
dotnet publish src/AutoClicker.App/AutoClicker.App.csproj -c Release -o ./publish/ -p:SelfContained=true

# Inno Setup installer
ISCC.exe installer.iss
```

## 📝 Version History

| Version | Date | Highlights |
| ------- | ---- | ---------- |
| v1.2.3 | 2025-07-26 | Auto-save settings, fix self-contained launch |
| v1.2.2 | 2025-07-22 | Fix click timing drift, English release filenames |
| v1.2.1 | 2025-07-07 | Cursorless clicking (PostClickAt), installer |
| v1.2.0 | 2025-07-06 | Multi-key triggers, scheduled tasks |
| v1.1.0 | 2025-06-21 | Macro recording/playback, single key trigger |
| v1.0.0 | 2025-05-22 | Core clicking, tray, settings, F6-F10 hotkeys |

Full changelog: [CHANGELOG.md](CHANGELOG.md)

## 🤝 Contributing

Issues and Pull Requests are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md)

## 📄 License

This project is licensed under the [MIT License](LICENSE) — free to use, modify, and distribute.

---

## ⭐ Star History

[![Star History Chart](https://api.star-history.com/svg?repos=NovanJex/mouse-auto-clicker&type=Date)](https://star-history.com/#NovanJex/mouse-auto-clicker&Date)
