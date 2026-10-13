using System;
using System.Windows;
using DesktopFences.Helpers;

namespace DesktopFences.Services;

/// <summary>
/// 全局桌面鼠标钩子服务：安装低级鼠标钩子 (WH_MOUSE_LL)，
/// 检测在桌面空白处的双击，触发回调（用于一键显隐全部栅栏）。
/// 钩子回调中仅做轻量判断并立即返回，避免阻塞系统输入。
/// </summary>
public class DesktopHookService : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private Win32Interop.LowLevelMouseProc? _proc; // 保持委托引用，防止被 GC
    private readonly Action _onDesktopDoubleClick;

    // 双击判定（钩子层自行判断，不依赖窗口消息）
    private int _lastDownTime;
    private int _lastX, _lastY;
    private const int DoubleClickMs = 400;
    private const int MovePixels = 6;

    public DesktopHookService(Action onDesktopDoubleClick)
    {
        _onDesktopDoubleClick = onDesktopDoubleClick;
    }

    /// <summary>安装钩子。</summary>
    public void Start()
    {
        if (_hookId != IntPtr.Zero) return;

        _proc = HookCallback;
        IntPtr hMod = Win32Interop.GetModuleHandle(null);
        _hookId = Win32Interop.SetWindowsHookEx(Win32Interop.WH_MOUSE_LL, _proc, hMod, 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (int)wParam == Win32Interop.WM_LBUTTONDOWN)
        {
            var data = System.Runtime.InteropServices.Marshal
                .PtrToStructure<Win32Interop.MSLLHOOKSTRUCT>(lParam);

            int now = Environment.TickCount;
            bool isDouble = (now - _lastDownTime) <= DoubleClickMs
                            && Math.Abs(data.pt.x - _lastX) <= MovePixels
                            && Math.Abs(data.pt.y - _lastY) <= MovePixels;

            if (isDouble && Win32Interop.IsPointOnDesktop(data.pt.x, data.pt.y))
            {
                // 命中桌面空白双击：切回 UI 线程执行回调
                _lastDownTime = 0;
                Application.Current?.Dispatcher.BeginInvoke(_onDesktopDoubleClick);
            }
            else
            {
                _lastDownTime = now;
                _lastX = data.pt.x;
                _lastY = data.pt.y;
            }
        }

        return Win32Interop.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            Win32Interop.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        _proc = null;
    }
}
