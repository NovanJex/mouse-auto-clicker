using System.Collections.Concurrent;
using System.Windows.Input;
using AutoClicker.App.Interop;
using AutoClicker.App.Models;
using AutoClicker.App.Services.Interfaces;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 全局热键服务 — 通过 RegisterHotKey/UnregisterHotKey 管理系统级热键
/// </summary>
public class HotkeyService : IHotkeyService, IDisposable
{
    private IntPtr _handle;
    private readonly ConcurrentDictionary<int, HotkeyBinding> _registered = new();

    /// <inheritdoc />
    public IntPtr Handle
    {
        get => _handle;
        set
        {
            _handle = value;
            ReregisterAll();
        }
    }

    /// <inheritdoc />
    public event EventHandler<int>? HotkeyPressed;

    /// <inheritdoc />
    public bool Register(HotkeyBinding binding)
    {
        if (_handle == IntPtr.Zero)
            return false;

        uint modifiers = MapModifiers(binding.Modifiers);
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(binding.Key);

        bool result = NativeMethods.RegisterHotKey(_handle, binding.Id, modifiers, vk);
        if (result)
            _registered[binding.Id] = binding;

        return result;
    }

    /// <inheritdoc />
    public void Unregister(int id)
    {
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_handle, id);
            _registered.TryRemove(id, out _);
        }
    }

    /// <inheritdoc />
    public void UnregisterAll()
    {
        foreach (var id in _registered.Keys)
        {
            if (_handle != IntPtr.Zero)
                NativeMethods.UnregisterHotKey(_handle, id);
        }
        _registered.Clear();
    }

    /// <inheritdoc />
    public void ReregisterAll()
    {
        foreach (var kvp in _registered)
        {
            uint modifiers = MapModifiers(kvp.Value.Modifiers);
            uint vk = (uint)KeyInterop.VirtualKeyFromKey(kvp.Value.Key);
            NativeMethods.RegisterHotKey(_handle, kvp.Key, modifiers, vk);
        }
    }

    /// <summary>
    /// WndProc 消息处理 — 拦截 WM_HOTKEY 并触发 HotkeyPressed 事件
    /// </summary>
    public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Constants.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            HotkeyPressed?.Invoke(this, id);
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>WPF ModifierKeys 转换为 Win32 修饰键标志</summary>
    private static uint MapModifiers(ModifierKeys modifiers)
    {
        uint mod = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt)) mod |= Win32Constants.MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control)) mod |= Win32Constants.MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift)) mod |= Win32Constants.MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows)) mod |= Win32Constants.MOD_WIN;
        mod |= Win32Constants.MOD_NOREPEAT; // 禁止热键自动重复
        return mod;
    }

    public void Dispose()
    {
        UnregisterAll();
    }
}
