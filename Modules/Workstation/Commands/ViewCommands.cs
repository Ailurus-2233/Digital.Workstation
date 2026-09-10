using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Models.Events;

namespace DigitalWorkstation.Workstation.Commands;

/// <summary>
///     shell 预置命令（ADR-0005）：与视图菜单走同一事件通路，供命令面板检索执行。
///     面板显隐快捷键仍是 MainWindow.axaml 的硬编码 KeyBinding，迁移为命令 Gesture 留待后续
/// </summary>
public class ViewCommands(IEventAggregator eventAggregator)
{
    [Command("ToggleSideBarTitle", Order = 100)]
    public void ToggleSideBar()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.SideBar);
    }

    [Command("ToggleBottomPanelTitle", Order = 200)]
    public void ToggleBottomPanel()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.BottomPanel);
    }

    [Command("ToggleAuxiliaryPanelTitle", Order = 300)]
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
