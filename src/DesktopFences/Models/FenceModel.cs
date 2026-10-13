using System.Collections.Generic;

namespace DesktopFences.Models;

/// <summary>
/// 单个栅栏容器的数据模型（可序列化）。
/// 记录位置、尺寸、外观与所含图标项。
/// </summary>
public class FenceModel
{
    /// <summary>唯一标识。</summary>
    public string Id { get; set; } = System.Guid.NewGuid().ToString("N");

    /// <summary>标题。</summary>
    public string Title { get; set; } = "新建栅栏";

    /// <summary>窗口左上角 X（屏幕坐标，DIP）。</summary>
    public double Left { get; set; } = 200;

    /// <summary>窗口左上角 Y。</summary>
    public double Top { get; set; } = 200;

    /// <summary>宽度。</summary>
    public double Width { get; set; } = 300;

    /// <summary>展开状态下的高度。</summary>
    public double Height { get; set; } = 240;

    /// <summary>是否已折叠（仅显示标题栏）。</summary>
    public bool IsRolledUp { get; set; }

    /// <summary>背景不透明度 0.2 ~ 1.0。</summary>
    public double Opacity { get; set; } = 0.85;

    /// <summary>
    /// 自定义背景色（#AARRGGBB 或 #RRGGBB），为空则使用主题默认色。
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>所含图标项。</summary>
    public List<FenceItemModel> Items { get; set; } = new();
}
