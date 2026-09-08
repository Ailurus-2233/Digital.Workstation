using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.Framework.Layout;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Menus;

/// <summary>
///     shell 预置的面板对齐项（视图菜单 Alignment 组，与 Panels 组之间由建树器插分隔线）：
///     点击发布 <see cref="SetPanelAlignmentEvent" />，主窗口写入 PanelAlignment 依赖属性整体切换布局模板
/// </summary>
[MenuGroup("MenuViewTitle", Group = "Alignment", GroupOrder = 200)]
public class ViewAlignmentMenus(IEventAggregator eventAggregator)
{
    [MenuItem("PanelAlignLeftTitle", Order = 100, Icon = Icons.AlignLeft)]
    public void AlignLeft()
    {
        eventAggregator.GetEvent<SetPanelAlignmentEvent>().Publish(PanelAlignment.Left);
    }

    [MenuItem("PanelAlignRightTitle", Order = 200, Icon = Icons.AlignRight)]
    public void AlignRight()
    {
        eventAggregator.GetEvent<SetPanelAlignmentEvent>().Publish(PanelAlignment.Right);
    }

    [MenuItem("PanelAlignCenterTitle", Order = 300, Icon = Icons.AlignCenter)]
    public void AlignCenter()
    {
        eventAggregator.GetEvent<SetPanelAlignmentEvent>().Publish(PanelAlignment.Center);
    }

    [MenuItem("PanelAlignJustifyTitle", Order = 400, Icon = Icons.AlignJustify)]
    public void AlignJustify()
    {
        eventAggregator.GetEvent<SetPanelAlignmentEvent>().Publish(PanelAlignment.Justify);
    }
}
