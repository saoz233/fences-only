using System;
using System.Threading;
using System.Windows;
using DesktopFences.Services;
using DesktopFences.ViewModels;
using DesktopFences.Helpers;

namespace DesktopFences;

/// <summary>
/// 应用启动入口（自写入口，避免 WPF ApplicationDefinition 自动生成 Main 造成重复入口）。
/// 负责单实例控制、资源字典加载、托盘初始化、恢复栅栏布局、主题与全局钩子。
/// </summary>
public class App : Application
{
    private static Mutex? _mutex;

    /// <summary>全局主视图模型（容器集合与全局状态）。</summary>
    public static MainViewModel MainVM { get; private set; } = null!;

    private System.Windows.Forms.NotifyIcon? _tray;

    /// <summary>程序入口。</summary>
    [STAThread]
    public static int Main(string[] args)
    {
        var app = new App();
        app.InitializeResources();
        return app.Run();
    }

    /// <summary>在代码中加载主题与样式资源字典（替代 App.xaml 的合并字典）。</summary>
    private void InitializeResources()
    {
        // 约定：索引 0 为主题色板（可被 ThemeService 替换），索引 1 为通用样式
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("Themes/Light.xaml", UriKind.Relative)
        });
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("Themes/Styles.xaml", UriKind.Relative)
        });
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1) 单实例：命名 Mutex 防止多开
        _mutex = new Mutex(true, "DesktopFences_SingleInstance_Mutex_7F3A", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("桌面栅栏已在运行中。", "桌面栅栏",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // 2) 没有主窗口，退出模式改为显式退出（仅托盘退出时关闭）
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 3) 加载配置并构建主视图模型
        MainVM = new MainViewModel();
        MainVM.Initialize();

        // 4) 应用主题（浅/深色，或跟随系统）
        ThemeService.Apply(MainVM.Settings.Theme);

        // 5) 初始化系统托盘图标与右键菜单
        _tray = TrayMenu.Create(MainVM);

        // 6) 恢复上次保存的栅栏窗口布局
        MainVM.RestoreFences();

        // 7) 启动全局鼠标钩子（检测桌面空白双击 => 显隐全部）
        MainVM.StartDesktopHook();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 退出前持久化并释放所有非托管资源
        try { MainVM?.SaveNow(); } catch { /* 忽略退出保存异常 */ }
        MainVM?.Dispose();
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }

        if (_mutex is not null)
        {
            try { _mutex.ReleaseMutex(); } catch { /* 可能未持有 */ }
            _mutex.Dispose();
        }

        base.OnExit(e);
    }
}
