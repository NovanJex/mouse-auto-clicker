using AutoClicker.App.Models;

namespace AutoClicker.App.Services.Interfaces;

/// <summary>
/// 配置持久化服务接口 — 基于 JSON 的读写
/// </summary>
public interface ISettingsService
{
    /// <summary>当前配置</summary>
    AppSettings Settings { get; }

    /// <summary>保存配置到文件</summary>
    void Save();

    /// <summary>从文件加载配置</summary>
    void Load();
}
