using Avalonia.Controls;
using Avalonia.Interactivity;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Models.Events;

namespace DigitalWorkstation.DashBoard.Views;

/// <summary>
///     DashBoard 的 SideBar 内容：条目点击经 OpenMainViewEvent 驱动 MainContent 单视图切换
/// </summary>
public partial class DashBoardNavigationView : UserControl
{
    private readonly IEventAggregator _eventAggregator;

    /// <summary>
    ///     XAML runtime loader 需要无参构造；实际实例由容器经依赖注入构造创建
    /// </summary>
    public DashBoardNavigationView() : this(IoC.Provider.Resolve<IEventAggregator>())
    {
    }

    public DashBoardNavigationView(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        InitializeComponent();
    }

    private void OpenOverview(object? sender, RoutedEventArgs e)
    {
        _eventAggregator.GetEvent<OpenMainViewEvent>().Publish(DashBoardOverviewMainView.ViewId);
    }

    private void OpenRecent(object? sender, RoutedEventArgs e)
    {
        _eventAggregator.GetEvent<OpenMainViewEvent>().Publish(DashBoardRecentMainView.ViewId);
    }
}
