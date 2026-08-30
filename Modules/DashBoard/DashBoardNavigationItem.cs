using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.DashBoard.Views;

namespace DigitalWorkstation.DashBoard;

/// <summary>
///     DashBoard 贡献给 ActivityBar 的导航项（tracer bullet：验证模块到 shell 的贡献通路）
/// </summary>
public class DashBoardNavigationItem : INavigationItemContribution
{
    public string Id => "dashboard";

    public string Title => Language.DashBoardNavigationTitle;

    public string IconPath => Icons.DashBoard;

    public int Order => 0;

    public NavigationItemPlacement Placement => NavigationItemPlacement.Top;

    public Type ContentViewType => typeof(DashBoardNavigationView);
}
