﻿using Avalonia.Controls;
using Avalonia.Interactivity;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.DashBoard.Views;

/// <summary>
///     DashBoard 的 SideBar 内容：条目点击经 OpenMainViewEvent 驱动 MainContent 单视图切换；
///     同时是 ActivityBar 顶部段的"启动台"工具视图（tracer bullet：验证模块到 shell 的贡献通路）
/// </summary>
[ToolView("dashboard", "DashBoardNavigationTitle", Icon = Icons.DashBoard,
    Default = ToolViewPlacement.ActivityBar)]
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
