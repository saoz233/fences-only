using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using DesktopFences.Helpers;
using DesktopFences.Services;
using DesktopFences.ViewModels;

namespace DesktopFences.Views;

/// <summary>
/// 栅栏容器窗口：无边框半透明窗，支持拖动、缩放、折叠、重命名、图标拖放。
/// </summary>
public partial class FenceWindow : Window
{
    private FenceViewModel? _vm;
    private bool _allowClose;
    private const double TitleBarHeight = 34; // 含边框，折叠时的窗口高度

    public FenceWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Closing += OnClosing;
    }

    /// <summary>绑定视图模型并恢复位置/尺寸与折叠状态。</summary>
    public void BindViewModel(FenceViewModel vm)
    {
        _vm = vm;
        DataContext = vm;

        Left = vm.Model.Left;
        Top = vm.Model.Top;
        Width = vm.Model.Width;
        Height = vm.IsRolledUp ? TitleBarHeight : vm.Model.Height;

        vm.PropertyChanged += Vm_PropertyChanged;
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_vm is null) return;

        switch (e.PropertyName)
        {
            case nameof(FenceViewModel.IsRolledUp):
                ApplyRollup(_vm.IsRolledUp);
                break;
            case nameof(FenceViewModel.FenceOpacity):
                ApplyBackground();
                break;
        }
    }

    private void ApplyRollup(bool rolledUp)
    {
        if (rolledUp)
        {
            Height = TitleBarHeight;
        }
        else
        {
            Height = _vm?.Model.Height ?? 240;
        }
    }

    // ===== 外观：工具窗 + 圆角 + 半透明背景 + 置底 =====

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        Win32Interop.MakeToolWindow(hwnd);
        Win32Interop.EnableRoundedCorners(hwnd);
        ApplyBackground();
        Win32Interop.SendToBottom(hwnd);
    }

    /// <summary>
    /// 根据当前主题与透明度设置容器背景色（半透明）。
    /// 在主题或透明度变化时调用以实时刷新。
    /// </summary>
    public void ApplyBackground()
    {
        double op = _vm?.FenceOpacity ?? 0.85;
        byte a = (byte)Math.Clamp(op * 255, 51, 255);

        Color c = ThemeService.IsDark
            ? Color.FromArgb(a, 0x1F, 0x1F, 0x1F)
            : Color.FromArgb(a, 0xF2, 0xF3, 0xF5);

        var brush = new SolidColorBrush(c);
        brush.Freeze();
        RootBorder.Background = brush;
    }

    // ===== 标题栏：拖动 / 折叠 =====

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm is null) return;

        if (e.ClickCount == 2)
        {
            // 双击标题栏空白：折叠/展开
            _vm.ToggleRollUpCommand.Execute(null);
            return;
        }

        try
        {
            DragMove();
        }
        catch { /* 释放过快可能抛异常，忽略 */ }
        finally
        {
            // 拖动结束同步位置
            _vm.UpdateBounds(Left, Top, Width, Height);
        }
    }

    private void TitleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm is null) return;
        if (e.ClickCount == 2)
        {
            // 双击标题文字：进入重命名（阻止冒泡到折叠）
            e.Handled = true;
            _vm.BeginRenameCommand.Execute(null);
            TitleEdit.Focus();
            TitleEdit.SelectAll();
        }
    }

    private void TitleEdit_KeyDown(object sender, KeyEventArgs e)
    {
        if (_vm is null) return;
        if (e.Key == Key.Enter)
        {
            _vm.EndRenameCommand.Execute(null);
            Keyboard.ClearFocus();
        }
        else if (e.Key == Key.Escape)
        {
            _vm.EndRenameCommand.Execute(null);
        }
    }

    private void TitleEdit_LostFocus(object sender, RoutedEventArgs e)
    {
        _vm?.EndRenameCommand.Execute(null);
    }

    // ===== 缩放 =====

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (_vm is null || _vm.IsRolledUp) return;

        double newWidth = Math.Max(160, Width + e.HorizontalChange);
        double newHeight = Math.Max(120, Height + e.VerticalChange);
        Width = newWidth;
        Height = newHeight;
        _vm.UpdateBounds(Left, Top, Width, Height);
    }

    // ===== 拖放：接收桌面/资源管理器拖入的文件，及容器间互拖 =====

    private Point _dragStart;
    private FenceItemViewModel? _dragCandidate;

    private void Item_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        _dragCandidate = (sender as FrameworkElement)?.DataContext as FenceItemViewModel;
    }

    private void Item_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragCandidate is null || _vm is null)
            return;

        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var data = new DataObject();
        // 进程内格式：用于容器间移动
        data.SetData(FenceDragFormats.InternalItem,
            new FenceItemDragData { Source = _vm, Item = _dragCandidate });
        // 同时提供 FileDrop：拖到资源管理器/桌面时可用
        data.SetData(DataFormats.FileDrop, new[] { _dragCandidate.Path });

        DragDrop.DoDragDrop((DependencyObject)sender, data,
            DragDropEffects.Move | DragDropEffects.Copy);
        _dragCandidate = null;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(FenceDragFormats.InternalItem))
            e.Effects = DragDropEffects.Move;
        else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (_vm is null) return;

        // 1) 容器间移动（进程内格式优先）
        if (e.Data.GetDataPresent(FenceDragFormats.InternalItem))
        {
            if (e.Data.GetData(FenceDragFormats.InternalItem) is FenceItemDragData payload
                && !ReferenceEquals(payload.Source, _vm))
            {
                if (_vm.AddItemByPath(payload.Item.Path))
                    payload.Source.RemoveItemModel(payload.Item);
            }
            e.Handled = true;
            return;
        }

        // 2) 从桌面/资源管理器拖入文件
        if (e.Data.GetDataPresent(DataFormats.FileDrop)
            && e.Data.GetData(DataFormats.FileDrop) is string[] paths)
        {
            foreach (var p in paths)
                _vm.AddItemByPath(p);
        }
        e.Handled = true;
    }

    // ===== 关闭控制：仅允许通过管理器真正关闭 =====

    /// <summary>由窗口管理器调用的真正关闭。</summary>
    public void CloseForever()
    {
        _allowClose = true;
        if (_vm is not null)
            _vm.PropertyChanged -= Vm_PropertyChanged;
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // 阻止意外关闭（无标题栏按钮可触发，此处为双保险）
        if (!_allowClose)
            e.Cancel = true;
    }
}
