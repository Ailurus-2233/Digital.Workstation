using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.DashBoard.Views.Windows;

namespace DigitalWorkstation.DashBoard;

/// <summary>
///     DashBoard 贡献给文件菜单的项（General 组，位于 shell 预置"退出"所属 Application 组之前）
/// </summary>
[MenuGroup("MenuFileTitle", Group = "General", GroupOrder = 100)]
public class DashBoardMenus(IWindowManager windowManager)
{
    /// <summary>
    ///     重新显示启动台窗口
    /// </summary>
    [MenuItem("DashBoardOpenWindowMenuTitle", Order = 100, Icon = Icons.DashBoard)]
    public void OpenDashBoard()
    {
        windowManager.ShowWindow<DashBoardWindow>();
    }
}
