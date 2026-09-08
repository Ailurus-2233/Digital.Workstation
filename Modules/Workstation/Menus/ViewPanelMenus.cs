using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Menus;

/// <summary>
///     shell 预置的面板显隐切换项（视图菜单 Panels 组）：
///     点击发布 <see cref="TogglePanelVisibilityEvent" />，与快捷键走同一状态转换
/// </summary>
[MenuGroup("MenuViewTitle", Group = "Panels", GroupOrder = 100, Order = 200)]
public class ViewPanelMenus(IEventAggregator eventAggregator)
{
    [MenuItem("ToggleSideBarTitle", Order = 100, Icon = Icons.PanelLeft)]
    public void ToggleSideBar()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.SideBar);
    }

    [MenuItem("ToggleBottomPanelTitle", Order = 200, Icon = Icons.PanelBottom)]
    public void ToggleBottomPanel()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.BottomPanel);
    }

    [MenuItem("ToggleAuxiliaryPanelTitle", Order = 300, Icon = Icons.PanelRight)]
    public void ToggleAuxiliaryPanel()
    {
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(TogglePanelTarget.AuxiliaryPanel);
    }
}
