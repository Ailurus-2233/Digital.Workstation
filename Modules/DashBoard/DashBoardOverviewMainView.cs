using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.DashBoard.Views;

namespace DigitalWorkstation.DashBoard;

/// <summary>
///     DashBoard 贡献给 MainContent 的概览主视图（演示 SideBar 条目 → MainContent 通路）
/// </summary>
public class DashBoardOverviewMainView : IMainViewContribution
{
    public const string ViewId = "dashboard.overview";

    public string Id => ViewId;

    public Type ViewType => typeof(DashBoardOverviewView);
}
