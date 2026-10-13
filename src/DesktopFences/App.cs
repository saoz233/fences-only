using System;
using System.Linq;
using System.Threading;
using System.Windows;
using DesktopFences.Services;
using DesktopFences.ViewModels;
using DesktopFences.Helpers;

namespace DesktopFences;

/// <summary>
/// 应用启动入口（自写入口，避免 WPF ApplicationDefinition 自动生成 Main 造成重复入口）。
/// 负责单实例控制、资源字典加载、托盘初始化、恢复栅栏布局、主题、全局钩子，
/// 以及响应桌面右键菜单的「新建栅栏」命令（通过命名事件做进程间通信）。
/// </summary>
public class App : Application
{
    private const string MutexName = "DesktopFences_SingleInstance_Mutex_7F3A";
    private const string NewFenceEventName = "DesktopFences_NewFence_Event_7F3A";
    private const string NewFenceArg = "--new-fence";

    private static Mutex? _mutex;
    private static EventWaitHandle? _newFenceEvent;
    private static RegisteredWaitHandle? _registeredWait;

    /// <summary>全局主视图模型（容器集合与全局状态）。</summary>
    public static MainViewModel MainVM { get; private set; } = null!;

    private System.Windows.Forms.NotifyIcon? _tray;

    /// <summary>程序入口。</summary>
    [STAThread]
    public static int Main(string[] args)
    {
        bool wantNewFence = args.Contains(NewFenceArg);

        // 单实例检测
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            // 已有实例在运行
            if (wantNewFence)
            {
                // 通知正在运行的实例新建栅栏，然后退出本进程
                try
                {
                    var ev = EventWaitHandle.OpenExisting(NewFenceEventName);
                    ev.Set();
                }
                catch { /* 打开失败则忽略 */ }
            }
            else
            {
                MessageBox.Show("桌面栅栏已在运行中。", "桌面栅栏",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return 0;
        }

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

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 加载配置并构建主视图模型
        MainVM = new MainViewModel();
        MainVM.Initialize();

        // 应用主题
        ThemeService.Apply(MainVM.Settings.Theme);

        // 托盘
        _tray = TrayMenu.Create(MainVM);

        // 恢复布局
        MainVM.RestoreFences();

        // 全局桌面双击钩子
        MainVM.StartDesktopHook();

        // 同步「桌面右键菜单」注册状态
        ShellMenuService.Sync(MainVM.Settings.ShowDesktopMenu);

        // 建立进程间通信：监听「新建栅栏」命名事件
        SetupNewFenceListener();

        // 若本次就是通过桌面右键菜单启动（之前无实例），直接在鼠标处新建一个
        if (Environment.GetCommandLineArgs().Contains(NewFenceArg))
        {
            Dispatcher.BeginInvoke(() => MainVM.CreateFenceAtCursor());
        }
    }

    /// <summary>创建命名事件并注册等待回调，收到信号时在鼠标处新建栅栏。</summary>
    private void SetupNewFenceListener()
    {
        _newFenceEvent = new EventWaitHandle(false, EventResetMode.AutoReset, NewFenceEventName);
        _registeredWait = ThreadPool.RegisterWaitForSingleObject(
            _newFenceEvent,
            (state, timedOut) =>
            {
                // 回调在线程池线程，切回 UI 线程操作窗口
                Dispatcher.BeginInvoke(() => MainVM?.CreateFenceAtCursor());
            },
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { MainVM?.SaveNow(); } catch { /* 忽略退出保存异常 */ }
        MainVM?.Dispose();

        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }

        _registeredWait?.Unregister(null);
        _newFenceEvent?.Dispose();

        if (_mutex is not null)
        {
            try { _mutex.ReleaseMutex(); } catch { /* 可能未持有 */ }
            _mutex.Dispose();
        }

        base.OnExit(e);
    }
}
