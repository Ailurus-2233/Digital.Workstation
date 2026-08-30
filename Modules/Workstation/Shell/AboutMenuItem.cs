using System.Windows.Input;
using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation.Shell;

/// <summary>
///     shell 预置的帮助菜单"关于"项：弹出关于对话框
/// </summary>
public class AboutMenuItem : IMenuItemContribution
{
    public AboutMenuItem(IWindowManager windowManager)
    {
        Command = new DelegateCommand(windowManager.ShowDialog<AboutWindow>);
    }

    public string Id => "shell.menu.about";

    public string Title => Language.MenuAboutTitle;

    public string IconPath => Icons.About;

    public int Order => 10;

    public MenuPlacement Menu => MenuPlacement.Help;

    public ICommand Command { get; }
}
