using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation.Shell;

/// <summary>
///     shell 预置的"设置"导航项，固定在 ActivityBar 底部
/// </summary>
public class SettingsNavigationItem : INavigationItemContribution
{
    public string Id => "shell.settings";

    public string Title => Language.SettingsNavigationTitle;

    public string IconPath => Icons.Settings;

    public int Order => 0;

    public NavigationItemPlacement Placement => NavigationItemPlacement.Bottom;

    public Type ContentViewType => typeof(SettingsView);
}
