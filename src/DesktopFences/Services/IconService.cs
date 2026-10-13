using System;
using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DesktopFences.Helpers;

namespace DesktopFences.Services;

/// <summary>
/// 图标提取服务：使用 Shell API (SHGetFileInfo) 提取文件/文件夹/快捷方式图标，
/// 并转换为 WPF 可用的 <see cref="ImageSource"/>，带内存缓存减少重复提取。
/// </summary>
public static class IconService
{
    // 以路径为键缓存已提取的图标
    private static readonly ConcurrentDictionary<string, BitmapSource?> Cache = new();

    /// <summary>获取指定路径的大图标；失败返回 null（调用方应有占位图兜底）。</summary>
    public static BitmapSource? GetIcon(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (Cache.TryGetValue(path, out var cached))
            return cached;

        BitmapSource? result = ExtractIcon(path);
        Cache[path] = result;
        return result;
    }

    /// <summary>清空缓存（目标文件变化时可调用）。</summary>
    public static void Invalidate(string path) => Cache.TryRemove(path, out _);

    private static BitmapSource? ExtractIcon(string path)
    {
        var shinfo = new Win32Interop.SHFILEINFO();
        uint flags = Win32Interop.SHGFI_ICON | Win32Interop.SHGFI_LARGEICON;

        // 若路径不存在，用文件属性方式仍可取到通用图标
        if (!File.Exists(path) && !Directory.Exists(path))
            flags |= Win32Interop.SHGFI_USEFILEATTRIBUTES;

        IntPtr hImg = Win32Interop.SHGetFileInfo(
            path, 0, ref shinfo,
            (uint)System.Runtime.InteropServices.Marshal.SizeOf(shinfo), flags);

        if (shinfo.hIcon == IntPtr.Zero)
            return null;

        try
        {
            var bmp = Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            bmp.Freeze(); // 跨线程安全
            return bmp;
        }
        catch
        {
            return null;
        }
        finally
        {
            // 释放非托管图标句柄，防止泄漏
            Win32Interop.DestroyIcon(shinfo.hIcon);
        }
    }
}
