using System.Collections.Generic;

namespace DesktopFences.Models;

/// <summary>应用主题模式。</summary>
public enum AppTheme
{
    /// <summary>浅色。</summary>
    Light,

    /// <summary>深色。</summary>
    Dark,

    /// <summary>跟随系统。</summary>
    System
}

/// <summary>
/// 全局应用设置（可序列化，整体落盘为 config.json）。
/// </summary>
public class AppSettings
{
    /// <summary>配置版本号，便于后续迁移。</summary>
    public int Version { get; set; } = 1;

    /// <summary>主题模式。</summary>
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>是否开机自启动（与注册表状态同步）。</summary>
    public bool AutoStart { get; set; }

    /// <summary>栅栏当前是否全部可见（双击桌面切换）。</summary>
    public bool FencesVisible { get; set; } = true;

    /// <summary>所有栅栏容器。</summary>
    public List<FenceModel> Fences { get; set; } = new();
}
