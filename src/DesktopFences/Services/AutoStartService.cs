using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace DesktopFences.Services;

/// <summary>
/// 开机自启动服务：通过注册表
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> 写入/移除启动项。
/// 使用 HKCU 无需管理员权限。
/// </summary>
public static class AutoStartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopFences";

    /// <summary>当前是否已开启开机自启。</summary>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            var val = key?.GetValue(ValueName) as string;
            return !string.IsNullOrEmpty(val);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>设置开机自启开关。</summary>
    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (key is null) return;

            if (enabled)
            {
                string exe = GetExecutablePath();
                // 加引号以兼容带空格的路径
                key.SetValue(ValueName, $"\"{exe}\"");
            }
            else
            {
                if (key.GetValue(ValueName) is not null)
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // 写注册表失败时静默忽略（例如组策略限制）
        }
    }

    private static string GetExecutablePath()
    {
        // 优先取主模块路径（单文件发布时指向真实 exe）
        string? path = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(path))
            return path!;

        return Environment.ProcessPath ?? AppContext.BaseDirectory;
    }
}
