using System;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;
using DesktopFences.ViewModels;

namespace DesktopFences.Helpers;

/// <summary>
/// 系统托盘图标与右键菜单构建（基于框架自带的 WinForms NotifyIcon，无第三方依赖）。
/// 全部使用完全限定/别名引用，避免与 WPF 类型冲突。
/// </summary>
internal static class TrayMenu
{
    /// <summary>创建并返回托盘图标（含右键菜单与双击显隐）。</summary>
    public static WinForms.NotifyIcon Create(MainViewModel main)
    {
        var tray = new WinForms.NotifyIcon
        {
            Text = "桌面栅栏",
            Icon = CreateIcon(),
            Visible = true,
            ContextMenuStrip = BuildMenu(main)
        };

        // 托盘双击：显隐全部栅栏
        tray.DoubleClick += (_, _) => main.ToggleAllVisible();

        return tray;
    }

    private static WinForms.ContextMenuStrip BuildMenu(MainViewModel main)
    {
        var menu = new WinForms.ContextMenuStrip();

        menu.Items.Add("显示 / 隐藏全部", null, (_, _) => main.ToggleAllVisible());
        menu.Items.Add("新建栅栏", null, (_, _) => main.CreateFence());
        menu.Items.Add("设置…", null, (_, _) => main.OpenSettings());
        menu.Items.Add(new WinForms.ToolStripSeparator());

        var autoStart = new WinForms.ToolStripMenuItem("开机自动启动")
        {
            CheckOnClick = true
        };
        autoStart.Click += (_, _) => main.SetAutoStart(!main.Settings.AutoStart);
        menu.Items.Add(autoStart);

        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => main.ExitApp());

        // 打开菜单时刷新自启勾选状态
        menu.Opening += (_, _) => autoStart.Checked = main.Settings.AutoStart;

        return menu;
    }

    /// <summary>程序化绘制一个简单的托盘图标（无需外部 ico 文件）。</summary>
    private static Drawing.Icon CreateIcon()
    {
        using var bmp = new Drawing.Bitmap(32, 32);
        using (var g = Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Drawing.Color.Transparent);

            using var accent = new Drawing.SolidBrush(Drawing.Color.FromArgb(0x00, 0x67, 0xC0));
            g.FillRectangle(accent, new Drawing.Rectangle(2, 2, 28, 28));

            using var white = new Drawing.SolidBrush(Drawing.Color.White);
            g.FillRectangle(white, new Drawing.Rectangle(7, 8, 7, 7));
            g.FillRectangle(white, new Drawing.Rectangle(18, 8, 7, 7));
            g.FillRectangle(white, new Drawing.Rectangle(7, 18, 18, 6));
        }

        // 从位图句柄创建图标
        IntPtr hIcon = bmp.GetHicon();
        return (Drawing.Icon)Drawing.Icon.FromHandle(hIcon).Clone();
    }
}
