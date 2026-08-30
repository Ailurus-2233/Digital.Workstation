using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.DashBoard.Views;

namespace DigitalWorkstation.DashBoard;

/// <summary>
///     DashBoard 贡献给 MainContent 的最近项目主视图（演示单视图整体替换）
/// </summary>
public class DashBoardRecentMainView : IMainViewContribution
{
    public const string ViewId = "dashboard.recent";

    public string Id => ViewId;

    public Type ViewType => typeof(DashBoardRecentView);
}
