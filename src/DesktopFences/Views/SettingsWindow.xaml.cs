using System.Windows;
using DesktopFences.ViewModels;

namespace DesktopFences.Views;

/// <summary>设置窗口。</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
