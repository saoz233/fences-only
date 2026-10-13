using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace DesktopFences.Services;

/// <summary>
/// 桌面背景右键菜单注册服务：在「桌面空白处右键」菜单中添加「新建栅栏」项。
/// 写入当前用户注册表（HKCU\Software\Classes\DesktopBackground\Shell），无需管理员权限。
/// 点击该项会以 <c>--new-fence</c> 参数启动程序，由正在运行的实例在鼠标处新建栅栏。
/// </summary>
public static class ShellMenuService
{
    // 桌面背景（壁纸）右键菜单的注册位置
    private const string KeyPath =
        @"Software\Classes\DesktopBackground\Shell\DesktopFences";
    private const string MenuText = "新建栅栏";

    /// <summary>是否已注册到桌面右键菜单。</summary>
    public static bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);
            return key is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>注册「新建栅栏」到桌面右键菜单（幂等）。</summary>
    public static void Register()
    {
        try
        {
            string exe = GetExecutablePath();

            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            if (key is null) return;
            key.SetValue(null, MenuText);              // 菜单显示文字
            key.SetValue("Icon", exe);                 // 菜单图标取程序图标
            // 在资源管理器未选中项时也显示（桌面背景菜单）
            key.SetValue("Position", "Top");

            using var cmd = key.CreateSubKey("command");
            cmd?.SetValue(null, $"\"{exe}\" --new-fence");
        }
        catch
        {
            // 写注册表失败（如策略限制）静默忽略
        }
    }

    /// <summary>从桌面右键菜单移除「新建栅栏」。</summary>
    public static void Unregister()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(KeyPath, throwOnMissingSubKey: false);
        }
        catch
        {
            // 忽略
        }
    }

    /// <summary>根据设置开关同步注册状态。</summary>
    public static void Sync(bool enabled)
    {
        if (enabled) Register();
        else Unregister();
    }

    private static string GetExecutablePath()
    {
        string? path = Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(path))
            return path!;
        return Environment.ProcessPath ?? AppContext.BaseDirectory;
    }
}
