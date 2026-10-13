using System.Threading;
using System.Windows;
using DesktopFences.Services;
using DesktopFences.ViewModels;
using DesktopFences.Helpers;

namespace DesktopFences;

/// <summary>
/// 应用启动入口：负责单实例控制、托盘初始化、加载配置、恢复栅栏布局、
/// 应用主题以及启动全局桌面鼠标钩子。
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex;

    /// <summary>全局主视图模型（容器集合与全局状态）。</summary>
    public static MainViewModel Main { get; private set; } = null!;

    private System.Windows.Forms.NotifyIcon? _tray;

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
        Main = new MainViewModel();
        Main.Initialize();

        // 4) 应用主题（浅/深色，或跟随系统）
        ThemeService.Apply(Main.Settings.Theme);

        // 5) 初始化系统托盘图标与右键菜单
        _tray = TrayMenu.Create(Main);

        // 6) 恢复上次保存的栅栏窗口布局
        Main.RestoreFences();

        // 7) 启动全局鼠标钩子（检测桌面空白双击 => 显隐全部）
        Main.StartDesktopHook();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 退出前持久化并释放所有非托管资源
        try { Main?.SaveNow(); } catch { /* 忽略退出保存异常 */ }
        Main?.Dispose();
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
