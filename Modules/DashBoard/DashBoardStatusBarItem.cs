using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.DashBoard;

/// <summary>
///     DashBoard 贡献给状态栏的演示条目（验证模块到状态栏的贡献通路），
///     Order(20) 排在 shell 预置"就绪"(10) 之后
/// </summary>
public class DashBoardStatusBarItem : IStatusBarItemContribution
{
    public string Id => "dashboard.status";

    public string Title => Language.DashBoardNavigationTitle;

    public string IconPath => Icons.DashBoard;

    public int Order => 20;
}
