using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using DesktopFences.Models;

namespace DesktopFences.Services;

/// <summary>
/// 配置读写服务：负责将 <see cref="AppSettings"/> 持久化到
/// <c>%AppData%\DesktopFences\config.json</c>，支持防抖保存与异常容错。
/// </summary>
public class ConfigService : IDisposable
{
    private readonly string _dir;
    private readonly string _file;
    private readonly JsonSerializerOptions _jsonOptions;

    // 防抖：拖动/缩放过程中频繁触发保存时，延迟合并为一次写盘
    private Timer? _debounceTimer;
    private AppSettings? _pending;
    private readonly object _lock = new();
    private const int DebounceMs = 800;

    public ConfigService()
    {
        _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopFences");
        _file = Path.Combine(_dir, "config.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>加载配置；文件缺失或损坏时回退为默认配置。</summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_file))
                return new AppSettings();

            string json = File.ReadAllText(_file);
            if (string.IsNullOrWhiteSpace(json))
                return new AppSettings();

            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            return settings ?? new AppSettings();
        }
        catch
        {
            // 配置损坏时备份并返回默认，避免启动崩溃
            TryBackupCorrupted();
            return new AppSettings();
        }
    }

    /// <summary>防抖保存：短时间内多次调用只写盘一次。</summary>
    public void SaveDebounced(AppSettings settings)
    {
        lock (_lock)
        {
            _pending = settings;
            _debounceTimer ??= new Timer(_ => FlushPending(), null, Timeout.Infinite, Timeout.Infinite);
            _debounceTimer.Change(DebounceMs, Timeout.Infinite);
        }
    }

    /// <summary>立即保存（退出或关键操作时调用）。</summary>
    public void SaveNow(AppSettings settings)
    {
        lock (_lock)
        {
            _pending = settings;
            FlushPending();
        }
    }

    private void FlushPending()
    {
        AppSettings? toSave;
        lock (_lock) { toSave = _pending; }
        if (toSave is null) return;

        try
        {
            Directory.CreateDirectory(_dir);
            string json = JsonSerializer.Serialize(toSave, _jsonOptions);
            // 先写临时文件再替换，避免写一半损坏
            string tmp = _file + ".tmp";
            File.WriteAllText(tmp, json);
            File.Copy(tmp, _file, overwrite: true);
            File.Delete(tmp);
        }
        catch
        {
            // 写盘失败静默忽略，下次再试
        }
    }

    private void TryBackupCorrupted()
    {
        try
        {
            if (File.Exists(_file))
                File.Copy(_file, _file + ".bak", overwrite: true);
        }
        catch { /* 忽略 */ }
    }

    public void Dispose()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }
}
