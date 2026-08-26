using System.IO;
using System.Text.Json;
using AutoClicker.App.Models;
using AutoClicker.App.Serialization;
using AutoClicker.App.Services.Interfaces;

namespace AutoClicker.App.Services.Implementation;

/// <summary>
/// 配置持久化服务 — 将 AppSettings 序列化为 JSON 存储到 %LocalAppData%
/// </summary>
public class SettingsService : ISettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AutoClicker",
        "settings.json");

    /// <summary>设置备份文件路径（主文件损坏时用于恢复）</summary>
    private static readonly string BackupPath = SettingsPath + ".bak";

    /// <inheritdoc />
    public AppSettings Settings { get; private set; } = new();

    /// <inheritdoc />
    public void Save()
    {
        var dir = Path.GetDirectoryName(SettingsPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        // 保存前先备份旧文件（防止新文件写入失败/损坏时丢失配置）
        if (File.Exists(SettingsPath))
            File.Copy(SettingsPath, BackupPath, overwrite: true);

        var json = JsonSerializer.Serialize(Settings, AppJsonSerializerContext.Default.AppSettings);
        File.WriteAllText(SettingsPath, json);
    }

    /// <inheritdoc />
    public void Load()
    {
        if (!File.Exists(SettingsPath))
            return;

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var loaded = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.AppSettings);
            if (loaded is not null)
                Settings = loaded;
        }
        catch
        {
            // 主文件损坏时尝试从备份恢复
            if (File.Exists(BackupPath))
            {
                try
                {
                    var backupJson = File.ReadAllText(BackupPath);
                    var loaded = JsonSerializer.Deserialize(backupJson, AppJsonSerializerContext.Default.AppSettings);
                    if (loaded is not null)
                    {
                        Settings = loaded;
                        return;
                    }
                }
                catch { }
            }

            // 备份也失败时使用默认配置
            Settings = new AppSettings();
        }
    }
}
