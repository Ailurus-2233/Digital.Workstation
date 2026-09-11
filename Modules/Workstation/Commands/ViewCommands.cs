using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Commands;

/// <summary>
///     shell 预置命令（ADR-0005）：与视图菜单走同一事件通路，供命令面板检索执行；
///     三面板显隐命令带 Gesture，由 MainWindow.axaml.cs 的 RegisterCommandGestures 接线生成窗口级 KeyBinding
/// </summary>
public class ViewCommands(IEventAggregator eventAggregator)
{
    [Command("ToggleSideBarTitle", Order = 100, Icon = Icons.PanelLeft, Gesture = "Ctrl+B")]
    public void ToggleSideBar()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.SideBar);
    }

    [Command("ToggleBottomPanelTitle", Order = 200, Icon = Icons.PanelBottom, Gesture = "Ctrl+J")]
    public void ToggleBottomPanel()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.BottomPanel);
    }

    [Command("ToggleAuxiliaryPanelTitle", Order = 300, Icon = Icons.PanelRight, Gesture = "Ctrl+Alt+B")]
    public void ToggleAuxiliaryPanel()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.AuxiliaryPanel);
    }

    [Command("ResetLayoutTitle", Order = 400)]
    public void ResetLayout()
    {
        eventAggregator.GetEvent<ResetLayoutEvent>().Publish();
    }
}
