using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation.Shell;

/// <summary>
///     shell 预置的 BottomPanel 演示 tab"日志"
/// </summary>
public class LogPanelTab : IPanelTabContribution
{
    public string Id => "shell.log";

    public string Title => Language.LogTabTitle;

    public string IconPath => Icons.Log;

    public int Order => 20;

    public PanelPlacement Panel => PanelPlacement.Bottom;

    public Type ContentViewType => typeof(LogView);
}
