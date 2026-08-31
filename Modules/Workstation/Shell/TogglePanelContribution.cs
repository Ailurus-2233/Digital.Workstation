using System.Windows.Input;
using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Shell;

/// <summary>
///     shell 预置的面板显隐切换项：贡献给视图菜单，
///     点击发布 <see cref="TogglePanelVisibilityEvent" />，与快捷键走同一状态转换
/// </summary>
public class TogglePanelContribution : IMenuItemContribution
{
    public TogglePanelContribution(IEventAggregator eventAggregator, TogglePanelTarget target)
    {
        Target = target;
        Command = new DelegateCommand(() =>
            eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(target));
    }

    /// <summary>
    ///     本项切换的目标面板/区域
    /// </summary>
    public TogglePanelTarget Target { get; }

    public string Id => $"shell.toggle-{Target.ToString().ToLowerInvariant()}";

    public string Title => Target switch
    {
        TogglePanelTarget.SideBar => Language.ToggleSideBarTitle,
        TogglePanelTarget.AuxiliaryPanel => Language.ToggleAuxiliaryPanelTitle,
        _ => Language.ToggleBottomPanelTitle
    };

    public string IconPath => Target switch
    {
        TogglePanelTarget.SideBar => Icons.PanelLeft,
        TogglePanelTarget.AuxiliaryPanel => Icons.PanelRight,
        _ => Icons.PanelBottom
    };

    public int Order => Target switch
    {
        TogglePanelTarget.SideBar => 10,
        TogglePanelTarget.BottomPanel => 20,
        _ => 30
    };

    public MenuPlacement Menu => MenuPlacement.View;

    public ICommand Command { get; }
}
