using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopFences.Models;
using DesktopFences.Services;

namespace DesktopFences.ViewModels;

/// <summary>
/// 单个栅栏容器的视图模型：驱动标题、折叠、透明度、图标集合及相关命令。
/// </summary>
public partial class FenceViewModel : ObservableObject
{
    /// <summary>底层数据模型。</summary>
    public FenceModel Model { get; }

    /// <summary>容器内图标集合。</summary>
    public ObservableCollection<FenceItemViewModel> Items { get; } = new();

    /// <summary>状态变化需要持久化时触发。</summary>
    public event Action? Changed;

    /// <summary>请求删除本容器时触发。</summary>
    public event Action<FenceViewModel>? CloseRequested;

    /// <summary>是否处于标题编辑态（双击标题进入）。</summary>
    [ObservableProperty]
    private bool _isEditingTitle;

    public FenceViewModel(FenceModel model)
    {
        Model = model;
        foreach (var item in model.Items)
            Items.Add(new FenceItemViewModel(item));
    }

    // ===== 绑定属性（直接映射到 Model 并触发持久化） =====

    public string Title
    {
        get => Model.Title;
        set
        {
            if (Model.Title == value) return;
            Model.Title = value;
            OnPropertyChanged();
            Changed?.Invoke();
        }
    }

    public bool IsRolledUp
    {
        get => Model.IsRolledUp;
        set
        {
            if (Model.IsRolledUp == value) return;
            Model.IsRolledUp = value;
            OnPropertyChanged();
            Changed?.Invoke();
        }
    }

    public double FenceOpacity
    {
        get => Model.Opacity;
        set
        {
            if (Math.Abs(Model.Opacity - value) < 0.001) return;
            Model.Opacity = value;
            OnPropertyChanged();
            Changed?.Invoke();
        }
    }

    // ===== 命令 =====

    /// <summary>双击标题栏：折叠/展开。</summary>
    [RelayCommand]
    private void ToggleRollUp() => IsRolledUp = !IsRolledUp;

    /// <summary>进入标题编辑态。</summary>
    [RelayCommand]
    private void BeginRename() => IsEditingTitle = true;

    /// <summary>退出标题编辑态并持久化。</summary>
    [RelayCommand]
    private void EndRename()
    {
        IsEditingTitle = false;
        Changed?.Invoke();
    }

    /// <summary>双击图标：打开目标，目标丢失则提示。</summary>
    [RelayCommand]
    private void OpenItem(FenceItemViewModel? item)
    {
        if (item is null) return;
        bool ok = ShortcutService.Open(item.Model);
        if (!ok)
        {
            MessageBox.Show($"无法打开「{item.Name}」，目标可能已被移动或删除。",
                "桌面栅栏", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>从容器移除某图标项（不删除磁盘文件）。</summary>
    [RelayCommand]
    private void RemoveItem(FenceItemViewModel? item)
    {
        if (item is null) return;
        Items.Remove(item);
        Model.Items.Remove(item.Model);
        Changed?.Invoke();
    }

    /// <summary>删除整个容器。</summary>
    [RelayCommand]
    private void DeleteFence()
    {
        var r = MessageBox.Show($"确定删除栅栏「{Title}」吗？（仅移除容器，不删除文件）",
            "桌面栅栏", MessageBoxButton.OKCancel, MessageBoxImage.Question);
        if (r == MessageBoxResult.OK)
            CloseRequested?.Invoke(this);
    }

    // ===== 供外部调用的方法 =====

    /// <summary>按路径添加图标项（去重）。返回是否新增。</summary>
    public bool AddItemByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (Model.Items.Any(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase)))
            return false;

        var model = ShortcutService.CreateItem(path);
        Model.Items.Add(model);
        Items.Add(new FenceItemViewModel(model));
        Changed?.Invoke();
        return true;
    }

    /// <summary>移除指定图标项（供跨容器移动时调用）。</summary>
    public void RemoveItemModel(FenceItemViewModel item)
    {
        Items.Remove(item);
        Model.Items.Remove(item.Model);
        Changed?.Invoke();
    }

    /// <summary>外部主动触发一次持久化（如拖动/缩放结束）。</summary>
    public void NotifyChanged() => Changed?.Invoke();

    /// <summary>更新位置尺寸（窗口移动/缩放后同步到模型）。</summary>
    public void UpdateBounds(double left, double top, double width, double height)
    {
        Model.Left = left;
        Model.Top = top;
        Model.Width = width;
        if (!IsRolledUp) // 折叠态高度不覆盖记录
            Model.Height = height;
        Changed?.Invoke();
    }
}
