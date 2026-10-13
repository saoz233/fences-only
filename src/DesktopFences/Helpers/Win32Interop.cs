using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopFences.Helpers;

/// <summary>
/// Win32 / DWM 原生互操作封装：窗口样式（工具窗/置底）、Win11 圆角、
/// 图标提取 (SHGetFileInfo)、低级全局鼠标钩子、鼠标坐标、桌面命中判断等。
/// </summary>
internal static class Win32Interop
{
    // ============ 窗口样式 ============

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOOLWINDOW = 0x00000080; // 不在任务栏/Alt-Tab 显示

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    /// <summary>将窗口标记为工具窗口（不在任务栏/Alt-Tab 显示）。
    /// 注意：不设置 WS_EX_NOACTIVATE，以保证重命名文本框能获得键盘焦点。</summary>
    public static void MakeToolWindow(IntPtr hWnd)
    {
        int ex = GetWindowLong(hWnd, GWL_EXSTYLE);
        ex |= WS_EX_TOOLWINDOW;
        SetWindowLong(hWnd, GWL_EXSTYLE, ex);
    }

    // 置底（贴近桌面层，但位于图标之上）
    private static readonly IntPtr HWND_BOTTOM = new(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    /// <summary>把窗口压到最底层（避免遮挡其它应用）。</summary>
    public static void SendToBottom(IntPtr hWnd)
    {
        SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
    }

    // ============ 圆角（Win11 DWM） ============

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    /// <summary>为窗口应用 Win11 圆角。</summary>
    public static void EnableRoundedCorners(IntPtr hwnd)
    {
        int pref = DWMWCP_ROUND;
        try { DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int)); }
        catch { /* 旧系统不支持则忽略 */ }
    }

    // ============ 图标提取 ============

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    public const uint SHGFI_ICON = 0x000000100;
    public const uint SHGFI_LARGEICON = 0x000000000;
    public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    // ============ 低级全局鼠标钩子 ============

    public const int WH_MOUSE_LL = 14;
    public const int WM_LBUTTONDOWN = 0x0201;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    // ============ 鼠标坐标 ============

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>
    /// 获取鼠标当前位置并换算为 WPF 的 DIP 坐标（除以系统 DPI 缩放）。
    /// </summary>
    public static (double x, double y) GetCursorDip()
    {
        if (!GetCursorPos(out var p))
            return (160, 160);

        double scale = GetSystemDpiScale();
        return (p.x / scale, p.y / scale);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    private const int LOGPIXELSX = 88;

    private static double GetSystemDpiScale()
    {
        IntPtr dc = GetDC(IntPtr.Zero);
        if (dc == IntPtr.Zero) return 1.0;
        try
        {
            int dpi = GetDeviceCaps(dc, LOGPIXELSX);
            return dpi <= 0 ? 1.0 : dpi / 96.0;
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, dc);
        }
    }

    // ============ 桌面命中判断 ============

    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(POINT p);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    /// <summary>
    /// 判断指定屏幕坐标处是否为桌面空白区域
    /// （命中 SysListView32 / SHELLDLL_DefView / WorkerW / Progman）。
    /// </summary>
    public static bool IsPointOnDesktop(int x, int y)
    {
        var pt = new POINT { x = x, y = y };
        IntPtr hWnd = WindowFromPoint(pt);
        if (hWnd == IntPtr.Zero) return false;

        var sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        string cls = sb.ToString();
        return cls is "SysListView32" or "SHELLDLL_DefView" or "WorkerW" or "Progman";
    }
}
