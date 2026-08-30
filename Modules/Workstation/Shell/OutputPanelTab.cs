using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation.Shell;

/// <summary>
///     shell 预置的 BottomPanel 演示 tab"输出"
/// </summary>
public class OutputPanelTab : IPanelTabContribution
{
    public string Id => "shell.output";

    public string Title => Language.OutputTabTitle;

    public string IconPath => Icons.Output;

    public int Order => 10;

    public PanelPlacement Panel => PanelPlacement.Bottom;

    public Type ContentViewType => typeof(OutputView);
}
