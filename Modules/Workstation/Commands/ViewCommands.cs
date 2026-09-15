using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Resources;

namespace DigitalWorkstation.Workstation.Commands;

/// <summary>
///     shell 预置命令（ADR-0005 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）：与视图菜单走同一事件通路，供命令面板检索执行；
///     三面板显隐命令带 Gesture，由 MainWindow.axaml.cs 的 RegisterCommandGestures 接线生成窗口级 KeyBinding
/// </summary>
public class ViewCommands(IEventAggregator eventAggregator)
{
    [Command(typeof(WorkstationResources), nameof(WorkstationResources.ToggleSideBarTitle), Order = 100, Icon = Icons.PanelLeft, Gesture = "Ctrl+B")]
    public void ToggleSideBar()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.SideBar);
    }

    [Command(typeof(WorkstationResources), nameof(WorkstationResources.ToggleBottomPanelTitle), Order = 200, Icon = Icons.PanelBottom, Gesture = "Ctrl+J")]
    public void ToggleBottomPanel()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.BottomPanel);
    }

    [Command(typeof(WorkstationResources), nameof(WorkstationResources.ToggleAuxiliaryPanelTitle), Order = 300, Icon = Icons.PanelRight, Gesture = "Ctrl+Alt+B")]
    public void ToggleAuxiliaryPanel()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.AuxiliaryPanel);
    }

    [Command(typeof(WorkstationResources), nameof(WorkstationResources.ResetLayoutTitle), Order = 400)]
    public void ResetLayout()
    {
        eventAggregator.GetEvent<ResetLayoutEvent>().Publish();
    }
}
