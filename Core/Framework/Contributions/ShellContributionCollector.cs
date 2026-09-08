using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Abstractions.Menus;

namespace DigitalWorkstation.Core.Framework.Contributions;

/// <summary>
///     贡献收集器：从容器收集模块注册的 shell 贡献，按元数据排序后供 shell 渲染
/// </summary>
public class ShellContributionCollector(IContainerProvider containerProvider)
{
    /// <summary>
    ///     收集指定 <paramref name="placement" /> 的全部导航项，按 <see cref="INavigationItemContribution.Order" /> 升序
    /// </summary>
    public IReadOnlyList<INavigationItemContribution> GetNavigationItems(NavigationItemPlacement placement)
    {
        return containerProvider.Resolve<IEnumerable<INavigationItemContribution>>()
            .Where(item => item.Placement == placement)
            .OrderBy(item => item.Order)
            .ToArray();
    }
    /// <summary>
    ///     收集模块贡献的全部 MainContent 主视图
    /// </summary>
    public IReadOnlyList<IMainViewContribution> GetMainViews()
    {
        return containerProvider.Resolve<IEnumerable<IMainViewContribution>>().ToArray();
    }
    /// <summary>
    ///     收集指定 <paramref name="panel" /> 的全部面板 tab，按 <see cref="IPanelTabContribution.Order" /> 升序
    /// </summary>
    public IReadOnlyList<IPanelTabContribution> GetPanelTabs(PanelPlacement panel)
    {
        return containerProvider.Resolve<IEnumerable<IPanelTabContribution>>()
            .Where(tab => tab.Panel == panel)
            .OrderBy(tab => tab.Order)
            .ToArray();
    }
    ///     收集全部菜单贡献（不过滤不排序；分组排序与建树由 <see cref="Menus.MenuTreeBuilder" /> 负责，ADR-0001）
    /// </summary>
    public IReadOnlyList<IMenuItemContribution> GetMenuItems()
    {
        return containerProvider.Resolve<IEnumerable<IMenuItemContribution>>().ToArray();
    }
    /// <summary>
    ///     收集全部状态栏项，按 <see cref="IStatusBarItemContribution.Order" /> 升序
    /// </summary>
    public IReadOnlyList<IStatusBarItemContribution> GetStatusBarItems()
    {
        return containerProvider.Resolve<IEnumerable<IStatusBarItemContribution>>()
            .OrderBy(item => item.Order)
            .ToArray();
    }
}
