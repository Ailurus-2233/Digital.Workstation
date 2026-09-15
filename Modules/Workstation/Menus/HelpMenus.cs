using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Views;
using DigitalWorkstation.Workstation.Resources;

namespace DigitalWorkstation.Workstation.Menus;

/// <summary>
///     shell 预置的帮助菜单项
/// </summary>
[MenuGroup("shell.help", typeof(WorkstationResources), nameof(WorkstationResources.MenuHelpTitle), Order = 300)]
public class HelpMenus(IWindowManager windowManager)
{
    /// <summary>
    ///     弹出关于对话框
    /// </summary>
    [MenuItem(typeof(WorkstationResources), nameof(WorkstationResources.MenuAboutTitle), Order = 100, Icon = Icons.About)]
    public void About()
    {
        windowManager.ShowDialog<AboutWindow>();
    }
}
