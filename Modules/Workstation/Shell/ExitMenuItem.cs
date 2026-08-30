using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Shell;

/// <summary>
///     shell 预置的文件菜单"退出"项：关闭整个应用。
///     Order 取大值保持在文件菜单末尾，模块贡献项排在其前
/// </summary>
public class ExitMenuItem : IMenuItemContribution
{
    public string Id => "shell.menu.exit";

    public string Title => Language.MenuExitTitle;

    public string IconPath => Icons.Exit;

    public int Order => 100;

    public MenuPlacement Menu => MenuPlacement.File;

    public ICommand Command { get; } = new DelegateCommand(() =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown());
}
