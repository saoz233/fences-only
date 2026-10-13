using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopFences.Models;
using DesktopFences.Services;
using DesktopFences.Views;

namespace DesktopFences.ViewModels;

/// <summary>
/// 主视图模型兼窗口管理器：持有全局设置与所有栅栏，负责创建/销毁栅栏窗口、
/// 全局显隐、主题/自启协调以及持久化调度。
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ConfigService _config = new();
    private DesktopHookService? _hook;

    /// <summary>全局设置。</summary>
    public AppSettings Settings { get; private set; } = new();

    /// <summary>所有栅栏视图模型。</summary>
    public ObservableCollection<FenceViewModel> Fences { get; } = new();

    // 视图模型 -> 对应窗口
    private readonly Dictionary<FenceViewModel, FenceWindow> _windows = new();

    private SettingsWindow? _settingsWindow;

    /// <summary>加载配置。</summary>
    public void Initialize()
    {
        Settings = _config.Load();
        // 同步自启状态（以注册表为准，修正配置）
        Settings.AutoStart = AutoStartService.IsEnabled();
    }

    /// <summary>依据配置创建并显示所有栅栏窗口。</summary>
    public void RestoreFences()
    {
        // 若首次启动没有任何栅栏，创建一个示例栅栏
        if (Settings.Fences.Count == 0)
        {
            Settings.Fences.Add(new FenceModel
            {
                Title = "我的栅栏",
                Left = 120, Top = 120, Width = 320, Height = 260
            });
            SaveNow();
        }

        foreach (var model in Settings.Fences.ToList())
            CreateFenceWindow(model);

        ApplyGlobalVisibility(Settings.FencesVisible);
    }

    /// <summary>启动全局桌面双击钩子。</summary>
    public void StartDesktopHook()
    {
        _hook = new DesktopHookService(ToggleAllVisible);
        _hook.Start();
    }

    // ===== 栅栏生命周期 =====

    private FenceViewModel CreateFenceWindow(FenceModel model)
    {
        var vm = new FenceViewModel(model);
        vm.Changed += SaveDebounced;
        vm.CloseRequested += OnFenceCloseRequested;

        var window = new FenceWindow { DataContext = vm };
        window.BindViewModel(vm);

        _windows[vm] = window;
        Fences.Add(vm);

        if (Settings.FencesVisible)
            window.Show();

        return vm;
    }

    /// <summary>新建一个栅栏（托盘菜单调用）。</summary>
    public void CreateFence()
    {
        var model = new FenceModel
        {
            Title = "新建栅栏",
            Left = 160, Top = 160, Width = 300, Height = 240
        };
        Settings.Fences.Add(model);

        // 新建时确保可见
        if (!Settings.FencesVisible)
            ToggleAllVisible();

        var vm = CreateFenceWindow(model);
        _windows[vm].Show();
        SaveNow();
    }

    private void OnFenceCloseRequested(FenceViewModel vm)
    {
        if (_windows.TryGetValue(vm, out var window))
        {
            window.CloseForever();
            _windows.Remove(vm);
        }
        Fences.Remove(vm);
        Settings.Fences.Remove(vm.Model);
        SaveNow();
    }

    // ===== 全局显隐 =====

    /// <summary>切换所有栅栏可见性（桌面双击/托盘触发）。</summary>
    public void ToggleAllVisible()
    {
        Settings.FencesVisible = !Settings.FencesVisible;
        ApplyGlobalVisibility(Settings.FencesVisible);
        SaveNow();
    }

    private void ApplyGlobalVisibility(bool visible)
    {
        foreach (var window in _windows.Values)
        {
            if (visible) window.Show();
            else window.Hide();
        }
    }

    // ===== 主题 / 自启 =====

    /// <summary>切换主题并持久化。</summary>
    public void SetTheme(AppTheme theme)
    {
        Settings.Theme = theme;
        ThemeService.Apply(theme);
        // 代码设置的背景需手动刷新（文字/图标颜色走 DynamicResource 自动更新）
        foreach (var window in _windows.Values)
            window.ApplyBackground();
        SaveNow();
    }

    /// <summary>设置开机自启。</summary>
    public void SetAutoStart(bool enabled)
    {
        AutoStartService.SetEnabled(enabled);
        Settings.AutoStart = AutoStartService.IsEnabled();
        SaveNow();
    }

    /// <summary>为所有栅栏统一设置背景透明度。</summary>
    public void SetOpacityForAll(double opacity)
    {
        foreach (var fence in Fences)
            fence.FenceOpacity = opacity;
        SaveNow();
    }

    /// <summary>读取当前透明度（取第一个栅栏，默认 0.85）。</summary>
    public double GetCurrentOpacity()
        => Fences.FirstOrDefault()?.FenceOpacity ?? 0.85;

    // ===== 设置窗口 =====

    /// <summary>打开设置窗口（单例）。</summary>
    public void OpenSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(new SettingsViewModel(this));
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    // ===== 持久化 =====

    /// <summary>防抖保存。</summary>
    public void SaveDebounced() => _config.SaveDebounced(Settings);

    /// <summary>立即保存。</summary>
    public void SaveNow() => _config.SaveNow(Settings);

    /// <summary>退出应用。</summary>
    public void ExitApp()
    {
        SaveNow();
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        _hook?.Dispose();
        _config.Dispose();
        foreach (var window in _windows.Values)
            window.CloseForever();
        _windows.Clear();
    }
}
