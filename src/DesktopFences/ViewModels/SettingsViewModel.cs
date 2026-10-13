using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopFences.Models;

namespace DesktopFences.ViewModels;

/// <summary>
/// 设置窗口视图模型：主题选择、开机自启开关、以及新建栅栏等快捷操作。
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly MainViewModel _main;

    public SettingsViewModel(MainViewModel main)
    {
        _main = main;
        _isLight = main.Settings.Theme == AppTheme.Light;
        _isDark = main.Settings.Theme == AppTheme.Dark;
        _isSystem = main.Settings.Theme == AppTheme.System;
        _autoStart = main.Settings.AutoStart;
        _opacity = main.GetCurrentOpacity();
        _showDesktopMenu = main.Settings.ShowDesktopMenu;
    }

    // 三选一的主题单选
    [ObservableProperty] private bool _isLight;
    [ObservableProperty] private bool _isDark;
    [ObservableProperty] private bool _isSystem;

    [ObservableProperty] private bool _autoStart;

    /// <summary>栅栏背景透明度 0.2 ~ 1.0。</summary>
    [ObservableProperty] private double _opacity;

    partial void OnOpacityChanged(double value)
    {
        _main.SetOpacityForAll(value);
    }

    partial void OnIsLightChanged(bool value)
    {
        if (value) _main.SetTheme(AppTheme.Light);
    }

    partial void OnIsDarkChanged(bool value)
    {
        if (value) _main.SetTheme(AppTheme.Dark);
    }

    partial void OnIsSystemChanged(bool value)
    {
        if (value) _main.SetTheme(AppTheme.System);
    }

    partial void OnAutoStartChanged(bool value)
    {
        _main.SetAutoStart(value);
        // 以注册表实际结果回写，避免策略限制导致不一致
        _autoStart = _main.Settings.AutoStart;
        OnPropertyChanged(nameof(AutoStart));
    }

    /// <summary>是否在桌面右键菜单显示「新建栅栏」。</summary>
    [ObservableProperty] private bool _showDesktopMenu;

    partial void OnShowDesktopMenuChanged(bool value)
    {
        _main.SetShowDesktopMenu(value);
    }

    /// <summary>新建栅栏。</summary>
    [RelayCommand]
    private void NewFence() => _main.CreateFence();

    /// <summary>显示/隐藏全部栅栏。</summary>
    [RelayCommand]
    private void ToggleVisible() => _main.ToggleAllVisible();
}
