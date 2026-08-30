using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.DashBoard.Views;

namespace DigitalWorkstation.DashBoard;

public class DashBoardModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<INavigationItemContribution, DashBoardNavigationItem>();
        containerRegistry.RegisterSingleton<IMainViewContribution, DashBoardOverviewMainView>();
        containerRegistry.RegisterSingleton<IMainViewContribution, DashBoardRecentMainView>();
        containerRegistry.RegisterSingleton<IPanelTabContribution, DashBoardTasksPanelTab>();
        containerRegistry.RegisterSingleton<IMenuItemContribution, OpenDashBoardMenuItem>();
        containerRegistry.RegisterSingleton<IToolBarItemContribution, DashBoardOverviewToolBarItem>();
        containerRegistry.RegisterSingleton<IStatusBarItemContribution, DashBoardStatusBarItem>();
        containerRegistry.Register<DashBoardNavigationView>();
        containerRegistry.Register<DashBoardOverviewView>();
        containerRegistry.Register<DashBoardRecentView>();
        containerRegistry.Register<DashBoardTasksView>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 启动台窗口由 shell 启动序列在模块加载前显示（ADR-0004），模块自身不再开窗
    }
}