using System.Windows.Input;
using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Framework.Shell;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.Workstation.Shell;

/// <summary>
///     shell 预置的面板对齐项：贡献给视图菜单（对齐组，与显隐组之间由 ViewModel 插分隔符），
///     点击发布 <see cref="SetPanelAlignmentEvent" />，主窗口写入 PanelAlignment 依赖属性整体切换布局模板
/// </summary>
public class PanelAlignmentContribution : IMenuItemContribution
{
    public PanelAlignmentContribution(IEventAggregator eventAggregator, PanelAlignment alignment)
    {
        Alignment = alignment;
        Command = new DelegateCommand(() =>
            eventAggregator.GetEvent<SetPanelAlignmentEvent>().Publish(alignment));
    }

    /// <summary>
    ///     本项切换到的布局档位
    /// </summary>
    public PanelAlignment Alignment { get; }

    public string Id => $"shell.align-{Alignment.ToString().ToLowerInvariant()}";

    public string Title => Alignment switch
    {
        PanelAlignment.Left => Language.PanelAlignLeftTitle,
        PanelAlignment.Right => Language.PanelAlignRightTitle,
        PanelAlignment.Justify => Language.PanelAlignJustifyTitle,
        _ => Language.PanelAlignCenterTitle
    };

    public string IconPath => Alignment switch
    {
        PanelAlignment.Left => Icons.AlignLeft,
        PanelAlignment.Right => Icons.AlignRight,
        PanelAlignment.Justify => Icons.AlignJustify,
        _ => Icons.AlignCenter
    };

    public int Order => Alignment switch
    {
        PanelAlignment.Left => 40,
        PanelAlignment.Right => 50,
        PanelAlignment.Center => 60,
        _ => 70
    };

    public MenuPlacement Menu => MenuPlacement.View;

    public ICommand Command { get; }
}
