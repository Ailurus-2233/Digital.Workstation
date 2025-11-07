using DigitalWorkstation.Core.Abstractions.WindowManager;
using DigitalWorkstation.DashBoard.Views.Windows;

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