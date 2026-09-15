using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Resources;

namespace DigitalWorkstation.Workstation.Menus;

/// <summary>
///     shell 预置的文件菜单项。"退出"归入 Application 组（GroupOrder 1000）保持在文件菜单末尾，
///     模块贡献的组排在其前
/// </summary>
[MenuGroup("shell.file", typeof(WorkstationResources), nameof(WorkstationResources.MenuFileTitle), Group = "Application", GroupOrder = 1000, Order = 100)]
public class FileMenus
{
    /// <summary>
    ///     关闭整个应用
    /// </summary>
    [MenuItem(typeof(WorkstationResources), nameof(WorkstationResources.MenuExitTitle), Order = 100, Icon = Icons.Exit)]
    public void Exit()
    {
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }
}
