using System;
using Microsoft.Win32;
using System.Windows;
using DesktopFences.Models;

namespace DesktopFences.Services;

/// <summary>
/// 主题服务：根据设置切换浅色/深色资源字典；System 模式读取系统主题。
/// </summary>
public static class ThemeService
{
    private const string LightUri = "Themes/Light.xaml";
    private const string DarkUri = "Themes/Dark.xaml";

    /// <summary>当前已解析的实际主题（Light/Dark）。</summary>
    public static bool IsDark { get; private set; }

    /// <summary>应用主题到全局资源。</summary>
    public static void Apply(AppTheme theme)
    {
        bool dark = theme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => IsSystemDark()
        };
        IsDark = dark;

        var app = Application.Current;
        if (app is null) return;

        // 替换合并字典中的主题项（约定第 0 项为主题色板）
        var dicts = app.Resources.MergedDictionaries;
        var newTheme = new ResourceDictionary
        {
            Source = new Uri(dark ? DarkUri : LightUri, UriKind.Relative)
        };

        if (dicts.Count > 0)
            dicts[0] = newTheme;
        else
            dicts.Add(newTheme);
    }

    /// <summary>读取系统是否为深色模式（注册表 AppsUseLightTheme=0 表示深色）。</summary>
    public static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            if (val is int i)
                return i == 0;
        }
        catch { /* 读取失败回退浅色 */ }
        return false;
    }
}
