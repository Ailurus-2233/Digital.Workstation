using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.DashBoard.Views;

namespace DigitalWorkstation.DashBoard;

/// <summary>
///     DashBoard 贡献给 BottomPanel 的演示面板 tab"任务"（验证模块到面板的贡献通路）
/// </summary>
public class DashBoardTasksPanelTab : IPanelTabContribution
{
    public string Id => "dashboard.tasks";

    public string Title => Language.DashBoardTasksTabTitle;

    public string IconPath => Icons.Tasks;

    // 介于 shell 预置"输出"(10) 与"日志"(20) 之间，验证按 Order 排序
    public int Order => 15;

    public PanelPlacement Panel => PanelPlacement.Bottom;

    public Type ContentViewType => typeof(DashBoardTasksView);
}
