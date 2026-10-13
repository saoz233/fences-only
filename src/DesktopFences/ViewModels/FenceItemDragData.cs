namespace DesktopFences.ViewModels;

/// <summary>容器间拖拽使用的进程内数据载体。</summary>
public sealed class FenceItemDragData
{
    /// <summary>拖拽源容器。</summary>
    public required FenceViewModel Source { get; init; }

    /// <summary>被拖拽的图标项。</summary>
    public required FenceItemViewModel Item { get; init; }
}

/// <summary>拖拽数据格式常量。</summary>
public static class FenceDragFormats
{
    /// <summary>进程内栅栏图标项格式名。</summary>
    public const string InternalItem = "DesktopFences.InternalItem";
}
