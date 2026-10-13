using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;
using DesktopFences.Models;
using DesktopFences.Services;

namespace DesktopFences.ViewModels;

/// <summary>
/// 容器内单个图标项的视图模型：封装数据模型并按需加载图标位图。
/// </summary>
public partial class FenceItemViewModel : ObservableObject
{
    /// <summary>底层数据模型。</summary>
    public FenceItemModel Model { get; }

    /// <summary>展示名称。</summary>
    public string Name => Model.Name;

    /// <summary>目标路径。</summary>
    public string Path => Model.Path;

    /// <summary>图标位图（可能为 null，由界面提供占位）。</summary>
    [ObservableProperty]
    private BitmapSource? _icon;

    public FenceItemViewModel(FenceItemModel model)
    {
        Model = model;
        LoadIcon();
    }

    /// <summary>提取/刷新图标。</summary>
    public void LoadIcon()
    {
        Icon = IconService.GetIcon(Model.Path);
    }
}
