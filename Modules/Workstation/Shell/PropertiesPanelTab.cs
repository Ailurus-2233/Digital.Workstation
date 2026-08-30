using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation.Shell;

/// <summary>
///     shell 预置的 AuxiliaryPanel 演示 tab"属性"
/// </summary>
public class PropertiesPanelTab : IPanelTabContribution
{
    public string Id => "shell.properties";

    public string Title => Language.PropertiesTabTitle;

    public string IconPath => Icons.Properties;

    public int Order => 10;

    public PanelPlacement Panel => PanelPlacement.Auxiliary;

    public Type ContentViewType => typeof(PropertiesView);
}
