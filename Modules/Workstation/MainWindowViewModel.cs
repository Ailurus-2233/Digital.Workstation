using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigitalWorkstation.Core.Abstractions.Shell;
using DigitalWorkstation.Core.Framework.Shell;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ShellContributionCollector _collector;
    private readonly IContainerProvider _containerProvider;
    private readonly Dictionary<string, NavigationItemViewModel> _itemsById = new();
    private readonly Dictionary<string, IMainViewContribution> _mainViewsById = new();
    private readonly Dictionary<string, object> _mainViewContents = new();
    private readonly Dictionary<string, object> _sideBarContents = new();
    private readonly Dictionary<string, IPanelTabContribution> _auxTabsById = new();
    private readonly Dictionary<string, IPanelTabContribution> _bottomTabsById = new();
    private readonly Dictionary<string, object> _auxTabContents = new();
    private readonly Dictionary<string, object> _bottomTabContents = new();
    private bool _contributionsLoaded;

    public MainWindowViewModel(ShellContributionCollector collector, IContainerProvider containerProvider,
        IEventAggregator eventAggregator)
    {
        _collector = collector;
        _containerProvider = containerProvider;
        eventAggregator.GetEvent<OpenMainViewEvent>().Subscribe(OpenMainView);
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Subscribe(TogglePanel);
        _mainContent = containerProvider.Resolve<EmptyStateView>();
    }

    [ObservableProperty]
    private ShellLayoutState _state = ShellLayoutState.Initial;

    [ObservableProperty]
    private object? _sideBarContent;
    /// <summary>
    ///     MainContent 当前内容；初始为 shell 内置空状态页，OpenMainViewEvent 到达后整体替换
    /// </summary>
    [ObservableProperty]
    private object _mainContent;

    [ObservableProperty]
    private string? _sideBarTitle;

    public ObservableCollection<NavigationItemViewModel> TopNavigationItems { get; } = [];

    public ObservableCollection<NavigationItemViewModel> BottomNavigationItems { get; } = [];
    public ObservableCollection<PanelTabViewModel> AuxiliaryTabs { get; } = [];

    public ObservableCollection<PanelTabViewModel> BottomTabs { get; } = [];
    /// <summary>
    ///     文件菜单项：shell 预置项与模块贡献项按 Order 统一排序
    /// </summary>
    public ObservableCollection<MenuItemViewModel> FileMenuItems { get; } = [];

    /// <summary>
    ///     视图菜单项：shell 预置项与模块贡献项按 Order 统一排序
    /// </summary>
    public ObservableCollection<MenuItemViewModel> ViewMenuItems { get; } = [];

    /// <summary>
    ///     帮助菜单项：shell 预置项与模块贡献项按 Order 统一排序
    /// </summary>
    public ObservableCollection<MenuItemViewModel> HelpMenuItems { get; } = [];

    /// <summary>
    ///     快速工具栏按钮：shell 预置项与模块贡献项按 Order 统一排序
    /// </summary>
    public ObservableCollection<ToolBarItemViewModel> ToolBarItems { get; } = [];

    /// <summary>
    ///     状态栏条目：shell 预置项与模块贡献项按 Order 统一排序
    /// </summary>
    public ObservableCollection<StatusBarItemViewModel> StatusBarItems { get; } = [];

    /// <summary>
    ///     AuxiliaryPanel 当前活动 tab 的内容；视图实例按 tab 缓存，切换再切回不丢
    /// </summary>
    [ObservableProperty]
    private object? _auxiliaryContent;

    /// <summary>
    ///     BottomPanel 当前活动 tab 的内容；视图实例按 tab 缓存，切换再切回不丢
    /// </summary>
    [ObservableProperty]
    private object? _bottomContent;
    /// <summary>
    ///     BottomPanel 收起按钮的图标几何
    /// </summary>
    public Geometry CollapseBottomIcon { get; } = StreamGeometry.Parse(Icons.ChevronDown);

    /// <summary>
    ///     AuxiliaryPanel 收起按钮的图标几何
    /// </summary>
    public Geometry CollapseAuxiliaryIcon { get; } = StreamGeometry.Parse(Icons.ChevronRight);

    /// <summary>
    ///     收集模块贡献的导航项。模块在 Prism 模块初始化阶段（晚于 shell 创建）才注册贡献，
    ///     因此由主窗口首次显示时触发，且只收集一次
    /// </summary>
    public void EnsureContributionsLoaded()
    {
        if (_contributionsLoaded)
        {
            return;
        }

        _contributionsLoaded = true;
        LoadItems(_collector.GetNavigationItems(NavigationItemPlacement.Top), TopNavigationItems);
        LoadItems(_collector.GetNavigationItems(NavigationItemPlacement.Bottom), BottomNavigationItems);
        foreach (var mainView in _collector.GetMainViews())
        {
            _mainViewsById[mainView.Id] = mainView;
        }
        LoadPanelTabs(_collector.GetPanelTabs(PanelPlacement.Auxiliary), AuxiliaryTabs, _auxTabsById,
            content => AuxiliaryContent = content, _auxTabContents);
        LoadPanelTabs(_collector.GetPanelTabs(PanelPlacement.Bottom), BottomTabs, _bottomTabsById,
            content => BottomContent = content, _bottomTabContents);
        LoadChrome(_collector.GetMenuItems(MenuPlacement.File), FileMenuItems);
        LoadChrome(_collector.GetMenuItems(MenuPlacement.View), ViewMenuItems);
        LoadChrome(_collector.GetMenuItems(MenuPlacement.Help), HelpMenuItems);
        foreach (var item in _collector.GetToolBarItems())
        {
            ToolBarItems.Add(new ToolBarItemViewModel(item));
        }
        foreach (var item in _collector.GetStatusBarItems())
        {
            StatusBarItems.Add(new StatusBarItemViewModel(item));
        }
    }

    /// <summary>
    ///     点击导航项：切换选中并驱动 SideBar 展开/收起；内容视图按导航项缓存，收起再展开不丢
    /// </summary>
    [RelayCommand]
    private void SelectActivity(NavigationItemViewModel item)
    {
        State = State.SelectActivity(item.Id);

        foreach (var navItem in _itemsById.Values)
        {
            navItem.IsSelected = navItem.Id == State.SelectedActivity;
        }

        if (!State.SideBar.Visible || State.SideBar.ContentFor is not { } contentId)
        {
            return;
        }

        if (!_sideBarContents.TryGetValue(contentId, out var content))
        {
            content = _containerProvider.Resolve(_itemsById[contentId].Contribution.ContentViewType);
            _sideBarContents[contentId] = content;
        }

        SideBarTitle = _itemsById[contentId].Title;
        SideBarContent = content;
    }
    /// <summary>
    ///     SideBar 内交互请求打开主视图：单视图切换，整体替换当前内容；视图实例按 Id 缓存
    /// </summary>
    private void OpenMainView(string viewId)
    {
        if (!_mainViewsById.TryGetValue(viewId, out var contribution))
        {
            return;
        }

        State = State.OpenMainView(viewId);

        if (!_mainViewContents.TryGetValue(viewId, out var content))
        {
            content = _containerProvider.Resolve(contribution.ViewType);
            _mainViewContents[viewId] = content;
        }

        MainContent = content;
    }
    /// <summary>
    ///     点击 AuxiliaryPanel tab：面板收起时 ShellLayoutState 拒绝（状态不变，本方法直接返回）
    /// </summary>
    [RelayCommand]
    private void ActivateAuxTab(PanelTabViewModel tab)
    {
        var next = State.ActivateAuxTab(tab.Id);
        if (ReferenceEquals(next, State))
        {
            return;
        }

        State = next;
        SyncActiveTab(AuxiliaryTabs, _auxTabsById, next.AuxiliaryPanel.ActiveTab,
            content => AuxiliaryContent = content, _auxTabContents);
    }

    /// <summary>
    ///     点击 BottomPanel tab：面板收起时 ShellLayoutState 拒绝（状态不变，本方法直接返回）
    /// </summary>
    [RelayCommand]
    private void ActivateBottomTab(PanelTabViewModel tab)
    {
        var next = State.ActivateBottomTab(tab.Id);
        if (ReferenceEquals(next, State))
        {
            return;
        }

        State = next;
        SyncActiveTab(BottomTabs, _bottomTabsById, next.BottomPanel.ActiveTab,
            content => BottomContent = content, _bottomTabContents);
    }

    /// <summary>
    ///     Ctrl+B：独立翻转 SideBar 可见性，选中项与内容保留
    /// </summary>
    [RelayCommand]
    private void ToggleSideBar()
    {
        TogglePanel(TogglePanelTarget.SideBar);
    }

    /// <summary>
    ///     收起按钮或 Ctrl+Alt+B：独立翻转 AuxiliaryPanel 可见性，活动 tab 记录保留
    /// </summary>
    [RelayCommand]
    private void ToggleAuxiliaryPanel()
    {
        TogglePanel(TogglePanelTarget.AuxiliaryPanel);
    }

    /// <summary>
    ///     收起按钮或 Ctrl+J：独立翻转 BottomPanel 可见性，活动 tab 记录保留
    /// </summary>
    [RelayCommand]
    private void ToggleBottomPanel()
    {
        TogglePanel(TogglePanelTarget.BottomPanel);
    }

    /// <summary>
    ///     面板显隐切换的唯一路径：快捷键、菜单项、快速工具栏按钮与收起按钮都汇到这里
    /// </summary>
    private void TogglePanel(TogglePanelTarget target)
    {
        State = target switch
        {
            TogglePanelTarget.SideBar => State.ToggleSideBar(),
            TogglePanelTarget.AuxiliaryPanel => State.ToggleAuxiliaryPanel(),
            _ => State.ToggleBottomPanel()
        };
    }

    private static void LoadChrome(IReadOnlyList<IMenuItemContribution> contributions,
        ObservableCollection<MenuItemViewModel> target)
    {
        foreach (var contribution in contributions)
        {
            target.Add(new MenuItemViewModel(contribution));
        }
    }

    /// <summary>
    ///     装载一个面板的 tab：建立索引、默认激活首个 tab 并同步到状态
    /// </summary>
    private void LoadPanelTabs(IReadOnlyList<IPanelTabContribution> contributions,
        ObservableCollection<PanelTabViewModel> target, Dictionary<string, IPanelTabContribution> index,
        Action<object> setContent, Dictionary<string, object> contentCache)
    {
        if (contributions.Count == 0)
        {
            return;
        }

        foreach (var contribution in contributions)
        {
            var tab = new PanelTabViewModel(contribution);
            index[tab.Id] = contribution;
            target.Add(tab);
        }

        var tabs = target.Select(tab => tab.Id).ToArray();
        var activeTab = tabs.FirstOrDefault();
        State = contributions[0].Panel switch
        {
            PanelPlacement.Auxiliary => State with
            {
                AuxiliaryPanel = State.AuxiliaryPanel with { Tabs = tabs, ActiveTab = activeTab }
            },
            _ => State with
            {
                BottomPanel = State.BottomPanel with { Tabs = tabs, ActiveTab = activeTab }
            }
        };

        SyncActiveTab(target, index, activeTab, setContent, contentCache);
    }

    /// <summary>
    ///     按活动 tab 同步高亮与内容；内容视图按 tab 缓存
    /// </summary>
    private void SyncActiveTab(ObservableCollection<PanelTabViewModel> tabs,
        Dictionary<string, IPanelTabContribution> index, string? activeTab,
        Action<object> setContent, Dictionary<string, object> contentCache)
    {
        foreach (var tab in tabs)
        {
            tab.IsActive = tab.Id == activeTab;
        }

        if (activeTab is null || !index.TryGetValue(activeTab, out var contribution))
        {
            return;
        }

        if (!contentCache.TryGetValue(activeTab, out var content))
        {
            content = _containerProvider.Resolve(contribution.ContentViewType);
            contentCache[activeTab] = content;
        }

        setContent(content);
    }

    private void LoadItems(IEnumerable<INavigationItemContribution> contributions,
        ObservableCollection<NavigationItemViewModel> target)
    {
        foreach (var contribution in contributions)
        {
            var item = new NavigationItemViewModel(contribution);
            _itemsById[item.Id] = item;
            target.Add(item);
        }
    }
}
