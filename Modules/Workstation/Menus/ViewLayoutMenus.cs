using DigitalWorkstation.Core.Abstractions.Menus;
using DigitalWorkstation.Core.Models.Events;

namespace DigitalWorkstation.Workstation.Menus;

/// <summary>
///     shell 预置的"重置布局"项（视图菜单 Layout 组，ADR-0002）：
///     点击发布 <see cref="ResetLayoutEvent" />，主窗口删除持久化配置并按 attribute 默认重建布局
/// </summary>
[MenuGroup("MenuViewTitle", Group = "Layout", GroupOrder = 300)]
public class ViewLayoutMenus(IEventAggregator eventAggregator)
{
    [MenuItem("ResetLayoutTitle", Order = 100)]
    public void ResetLayout()
    {
        eventAggregator.GetEvent<ResetLayoutEvent>().Publish();
    }
}
