using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.DashBoard.Views.Windows;

namespace DigitalWorkstation.DashBoard;

/// <summary>
///     DashBoard 贡献的命令（ADR-0005）：与文件菜单"打开启动台"同通路，验证模块侧命令扫描注册
/// </summary>
public class DashBoardCommands(IWindowManager windowManager)
{
    /// <summary>
    ///     重新显示启动台窗口
    /// </summary>
    [Command("DashBoardOpenWindowMenuTitle", Order = 500, Icon = Icons.DashBoard)]
    public void OpenDashBoard()
    {
        windowManager.ShowWindow<DashBoardWindow>();
    }
}
