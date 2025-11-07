using DigitalWorkstation.DashBoard.Views.Windows;
using DigitalWorkstation.WindowManager;

namespace DigitalWorkstation.DashBoard;

public class DashBoardModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        var windowManager = containerProvider.Resolve<IWindowManager>();
        windowManager.ShowWindow<DashBoardWindow>();
    }
}