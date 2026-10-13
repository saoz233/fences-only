namespace DesktopFences.Models;

/// <summary>
/// 栅栏容器内单个图标项的数据模型（可序列化）。
/// 仅保存目标路径与展示信息，不保存图标位图本身（图标运行时按需提取）。
/// </summary>
public class FenceItemModel
{
    /// <summary>展示名称（默认取文件名，去扩展名）。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>目标完整路径（可为 .lnk、文件、文件夹）。</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>项目类型：Shortcut / File / Folder。</summary>
    public FenceItemType Type { get; set; } = FenceItemType.File;
}

/// <summary>图标项类型。</summary>
public enum FenceItemType
{
    /// <summary>快捷方式 (.lnk)。</summary>
    Shortcut,

    /// <summary>普通文件。</summary>
    File,

    /// <summary>文件夹。</summary>
    Folder
}
