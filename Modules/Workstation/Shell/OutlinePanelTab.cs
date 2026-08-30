using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation.Shell;

/// <summary>
///     shell 预置的 AuxiliaryPanel 演示 tab"大纲"
/// </summary>
public class OutlinePanelTab : IPanelTabContribution
{
    public string Id => "shell.outline";

    public string Title => Language.OutlineTabTitle;

    public string IconPath => Icons.Outline;

    public int Order => 20;

    public PanelPlacement Panel => PanelPlacement.Auxiliary;

    public Type ContentViewType => typeof(OutlineView);
}
