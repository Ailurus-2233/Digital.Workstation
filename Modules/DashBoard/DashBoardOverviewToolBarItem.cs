using System.Windows.Input;
using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.Resource;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.DashBoard;

/// <summary>
///     DashBoard 贡献给快速工具栏的"概览"按钮：在 MainContent 打开概览主视图。
///     Order(15) 介于 shell 预置切换项 SideBar(10) 与 BottomPanel(20) 之间，验证按 Order 排序
/// </summary>
public class DashBoardOverviewToolBarItem : IToolBarItemContribution
{
    public DashBoardOverviewToolBarItem(IEventAggregator eventAggregator)
    {
        Command = new DelegateCommand(() =>
            eventAggregator.GetEvent<OpenMainViewEvent>().Publish(DashBoardOverviewMainView.ViewId));
    }

    public string Id => "dashboard.toolbar.overview";

    public string Title => Language.DashBoardOverviewToolBarTitle;

    public string IconPath => Icons.DashBoard;

    public int Order => 15;

    public ICommand Command { get; }
}
