using System;
using System.Diagnostics;
using System.IO;
using DesktopFences.Models;

namespace DesktopFences.Services;

/// <summary>
/// 快捷方式/目标处理服务：解析项目类型、启动目标，目标丢失时优雅处理。
/// </summary>
public static class ShortcutService
{
    /// <summary>根据路径推断项目类型。</summary>
    public static FenceItemType DetectType(string path)
    {
        if (Directory.Exists(path))
            return FenceItemType.Folder;

        string ext = Path.GetExtension(path);
        if (string.Equals(ext, ".lnk", StringComparison.OrdinalIgnoreCase))
            return FenceItemType.Shortcut;

        return FenceItemType.File;
    }

    /// <summary>取展示名称（去扩展名；.lnk 去掉 .lnk 后缀）。</summary>
    public static string GetDisplayName(string path)
    {
        try
        {
            string name = Path.GetFileName(path);
            string ext = Path.GetExtension(name);
            if (string.Equals(ext, ".lnk", StringComparison.OrdinalIgnoreCase))
                name = Path.GetFileNameWithoutExtension(name);
            else if (!Directory.Exists(path))
                name = Path.GetFileNameWithoutExtension(name);
            return string.IsNullOrEmpty(name) ? path : name;
        }
        catch
        {
            return path;
        }
    }

    /// <summary>由路径构建图标项模型。</summary>
    public static FenceItemModel CreateItem(string path)
    {
        return new FenceItemModel
        {
            Path = path,
            Name = GetDisplayName(path),
            Type = DetectType(path)
        };
    }

    /// <summary>
    /// 打开目标（快捷方式/文件/文件夹）。目标不存在时返回 false，由调用方提示。
    /// </summary>
    public static bool Open(FenceItemModel item)
    {
        try
        {
            // 对快捷方式本身存在即可启动；普通文件/文件夹需存在
            bool exists = File.Exists(item.Path) || Directory.Exists(item.Path);
            if (!exists)
                return false;

            var psi = new ProcessStartInfo
            {
                FileName = item.Path,
                UseShellExecute = true // 由 Shell 处理 .lnk / 文件夹 / 关联程序
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
