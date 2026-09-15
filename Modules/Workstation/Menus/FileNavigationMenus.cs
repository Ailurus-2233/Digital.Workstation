using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.Abstractions.Regions;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Resources;

namespace DigitalWorkstation.Workstation.Menus;

/// <summary>
///     文件菜单的导航组，排在应用退出组之前；两个导航动作同时暴露为命令面板命令
/// </summary>
[MenuGroup("shell.file", Group = "Navigation", GroupOrder = 100, Order = 100)]
public class FileNavigationMenus(IEventAggregator eventAggregator)
{
    [MenuItem(typeof(WorkstationResources), nameof(WorkstationResources.ReturnHomeTitle), Order = 100)]
    [Command(typeof(WorkstationResources), nameof(WorkstationResources.ReturnHomeTitle), Order = 500)]
    public void ReturnHome()
    {
        eventAggregator.GetEvent<ReturnHomeEvent>().Publish();
    }

    [MenuItem(typeof(WorkstationResources), nameof(WorkstationResources.MenuPreferencesTitle), Order = 200, Icon = Icons.Settings)]
    [Command(typeof(WorkstationResources), nameof(WorkstationResources.MenuPreferencesTitle), Order = 600, Icon = Icons.Settings, Gesture = "Ctrl+OemComma")]
    public void OpenPreferences()
    {
        eventAggregator.GetEvent<OpenMainViewEvent>().Publish(WellKnownViews.Settings);
    }
}
