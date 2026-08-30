using System.Windows.Input;
using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.DashBoard.Views.Windows;

namespace DigitalWorkstation.DashBoard;

/// <summary>
///     DashBoard 贡献给文件菜单的"打开启动台"项：重新显示启动台窗口。
///     Order(10) 小于 shell 预置"退出"(100)，验证模块项与 shell 项按 Order 统一排序
/// </summary>
public class OpenDashBoardMenuItem : IMenuItemContribution
{
    public OpenDashBoardMenuItem(IWindowManager windowManager)
    {
        Command = new DelegateCommand(windowManager.ShowWindow<DashBoardWindow>);
    }

    public string Id => "dashboard.menu.open";

    public string Title => Language.DashBoardOpenWindowMenuTitle;

    public string IconPath => Icons.DashBoard;

    public int Order => 10;

    public MenuPlacement Menu => MenuPlacement.File;

    public ICommand Command { get; }
}
