using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.DashBoard.Views;
using DigitalWorkstation.DashBoard.Views.Windows;

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
        var windowManager = containerProvider.Resolve<IWindowManager>();
        windowManager.ShowWindow<DashBoardWindow>();
    }
}