using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Framework.Contributions;
using DigitalWorkstation.Core.Framework.Layout;
using DigitalWorkstation.Core.Framework.Menus;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ShellContributionCollector _collector;
    private readonly IContainerProvider _containerProvider;
    private readonly LayoutPersistence _persistence;
    private readonly Dictionary<string, NavigationItemViewModel> _itemsById = new();
    private readonly Dictionary<string, IMainViewContribution> _mainViewsById = new();
    private readonly Dictionary<string, object> _mainViewContents = new();
    private readonly Dictionary<string, object> _sideBarContents = new();
    private readonly Dictionary<string, ToolViewContribution> _auxTabsById = new();
    private readonly Dictionary<string, ToolViewContribution> _bottomTabsById = new();
    private readonly Dictionary<string, object> _auxTabContents = new();
    private readonly Dictionary<string, object> _bottomTabContents = new();
    private bool _contributionsLoaded;
    private IReadOnlyList<ToolViewContribution> _toolViews = [];

    public MainWindowViewModel(ShellContributionCollector collector, IContainerProvider containerProvider,
        IEventAggregator eventAggregator, LayoutPersistence persistence)
    {
        _collector = collector;
        _containerProvider = containerProvider;
        _persistence = persistence;
        eventAggregator.GetEvent<OpenMainViewEvent>().Subscribe(OpenMainView);
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Subscribe(TogglePanel);
        eventAggregator.GetEvent<SetPanelAlignmentEvent>().Subscribe(SetPanelAlignment);
        eventAggregator.GetEvent<ResetLayoutEvent>().Subscribe(ResetLayout);
        _mainContent = containerProvider.Resolve<EmptyStateView>();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SideBarColumnWidth), nameof(AuxiliaryColumnWidth))]
    private ShellLayoutState _state = ShellLayoutState.Initial;

    /// <summary>
    ///     当前窗口布局档位：与 FrameworkWindow.PanelAlignment 双向绑定（窗口依赖属性是布局定义的唯一入口），
    ///     默认居中 = 历史布局
    /// </summary>
    [ObservableProperty]
    private PanelAlignment _panelAlignment = PanelAlignment.Center;

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
    ///     菜单栏：全部菜单贡献经 MenuTreeBuilder 建树生成（ADR-0001）——顶层不分组不插分隔线，
    ///     子菜单按 Group/Order 分组排序、组间插分隔线
    /// </summary>
    public ObservableCollection<MenuItemViewModel> MenuBarItems { get; } = [];


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
    ///     SideBar 列宽：可见时为卡片宽度 + 4px 外边距间隙（布局模板的间隙约定），隐藏时归零，
    ///     BottomPanel 的跨度随之自然伸缩
    /// </summary>
    public GridLength SideBarColumnWidth =>
        State.SideBar.Visible ? new GridLength(State.SideBar.Width + 4) : new GridLength(0);

    /// <summary>
    ///     AuxiliaryPanel 列宽：规则同 SideBarColumnWidth
    /// </summary>
    public GridLength AuxiliaryColumnWidth =>
        State.AuxiliaryPanel.Visible ? new GridLength(State.AuxiliaryPanel.Width + 4) : new GridLength(0);

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
        _toolViews = _collector.GetToolViews();
        LoadToolViews(_persistence.Load());
        foreach (var mainView in _collector.GetMainViews())
        {
            _mainViewsById[mainView.Id] = mainView;
        }
        foreach (var submenu in MenuTreeBuilder.Build(_collector.GetMenuItems()))
        {
            MenuBarItems.Add(MenuItemViewModel.FromSubmenu(submenu));
        }
        foreach (var item in _collector.GetStatusBarItems())
        {
            StatusBarItems.Add(new StatusBarItemViewModel(item));
        }
    }

    /// <summary>
    ///     工具视图分派到三处 Bar 并恢复持久化布局：钉住项（AllowMove=false）恒落 ActivityBar 底部段；
    ///     可移动项「配置优先、默认兜底」——持久化 placements 优先于 attribute Default，孤儿条目随贡献迭代
    ///     自然丢弃，无配置条目的新工具视图按 Order 追加到 Default Bar 末尾；layout 为 null（含重置）时全部走默认
    /// </summary>
    private void LoadToolViews(ShellLayoutDto? layout)
    {
        var placements = layout?.Placements ?? [];

        IReadOnlyList<ToolViewContribution> MovableIn(ToolViewPlacement bar)
        {
            var configured = _toolViews
                .Where(view => view.AllowMove
                               && placements.TryGetValue(view.Id, out var entry) && entry.Bar == bar)
                .OrderBy(view => placements[view.Id].Index);
            var fresh = _toolViews
                .Where(view => view.AllowMove && !placements.ContainsKey(view.Id) && view.Placement == bar)
                .OrderBy(view => view.Order);
            return configured.Concat(fresh).ToArray();
        }

        LoadItems(MovableIn(ToolViewPlacement.ActivityBar), TopNavigationItems);
        LoadItems(_toolViews.Where(view => view.Placement == ToolViewPlacement.ActivityBar && !view.AllowMove),
            BottomNavigationItems);
        LoadPanelTabs(MovableIn(ToolViewPlacement.AuxiliaryPanel), ToolViewPlacement.AuxiliaryPanel,
            AuxiliaryTabs, _auxTabsById, layout?.AuxiliaryPanel?.ActiveTab,
            content => AuxiliaryContent = content, _auxTabContents);
        LoadPanelTabs(MovableIn(ToolViewPlacement.BottomPanel), ToolViewPlacement.BottomPanel,
            BottomTabs, _bottomTabsById, layout?.BottomPanel?.ActiveTab,
            content => BottomContent = content, _bottomTabContents);

        if (layout is not null)
        {
            RestoreLayout(layout);
        }
    }

    /// <summary>
    ///     恢复持久化的显隐/尺寸/选中项/对齐档位；尺寸 clamp 到各区域的合法区间，
    ///     选中项无对应工具视图时丢弃（孤儿）；活动 tab 已在 LoadPanelTabs 按配置恢复
    /// </summary>
    private void RestoreLayout(ShellLayoutDto layout)
    {
        if (layout.SideBar is { } sideBar)
        {
            var selected = sideBar.Selected is { } id && _itemsById.ContainsKey(id) ? id : null;
            State = State with
            {
                SelectedActivity = selected,
                SideBar = State.SideBar with
                {
                    Visible = sideBar.Visible,
                    Width = Math.Clamp(sideBar.Width, SideBarState.MinWidth, SideBarState.MaxWidth),
                    ContentFor = selected
                }
            };
            foreach (var navItem in _itemsById.Values)
            {
                navItem.IsSelected = navItem.Id == selected;
            }

            if (selected is not null)
            {
                if (!_sideBarContents.TryGetValue(selected, out var content))
                {
                    content = _containerProvider.Resolve(_itemsById[selected].Contribution.ViewType);
                    _sideBarContents[selected] = content;
                }

                SideBarTitle = _itemsById[selected].Title;
                SideBarContent = content;
            }
        }

        if (layout.AuxiliaryPanel is { } auxiliary)
        {
            State = State with
            {
                AuxiliaryPanel = State.AuxiliaryPanel with
                {
                    Visible = auxiliary.Visible,
                    Width = Math.Clamp(auxiliary.Width, AuxiliaryPanelState.MinWidth, AuxiliaryPanelState.MaxWidth)
                }
            };
        }

        if (layout.BottomPanel is { } bottom)
        {
            State = State with
            {
                BottomPanel = State.BottomPanel with
                {
                    Visible = bottom.Visible,
                    Height = Math.Clamp(bottom.Height, BottomPanelState.MinHeight, BottomPanelState.MaxHeight)
                }
            };
        }

        PanelAlignment = layout.PanelAlignment;
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

        ScheduleSave();

        if (!State.SideBar.Visible || State.SideBar.ContentFor is not { } contentId)
        {
            return;
        }

        if (!_sideBarContents.TryGetValue(contentId, out var content))
        {
            content = _containerProvider.Resolve(_itemsById[contentId].Contribution.ViewType);
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
        ScheduleSave();
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
        ScheduleSave();
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
    ///     面板对齐切换的唯一路径：视图菜单对齐项经 SetPanelAlignmentEvent 汇到这里；
    ///     布局模板由 FrameworkWindow 监听 PanelAlignment 依赖属性整体替换
    /// </summary>
    private void SetPanelAlignment(PanelAlignment alignment)
    {
        PanelAlignment = alignment;
        ScheduleSave();
    }

    /// <summary>
    ///     分隔条拖拽的唯一路径：PanelResizer 换算方向后经命令汇到这里；
    ///     增量经状态转换应用并 clamp 到合法区间；BottomPanel 调高度，其余调宽度；
    ///     面板收起时尺寸记录保留，恢复后不重置
    /// </summary>
    [RelayCommand]
    private void ResizePanel(PanelResize resize)
    {
        State = State.Resize(resize.Target, resize.Delta);
        ScheduleSave();
    }

    /// <summary>
    ///     面板显隐切换的唯一路径：快捷键、菜单项与收起按钮都汇到这里
    /// </summary>
    private void TogglePanel(TogglePanelTarget target)
    {
        State = target switch
        {
            TogglePanelTarget.SideBar => State.ToggleSideBar(),
            TogglePanelTarget.AuxiliaryPanel => State.ToggleAuxiliaryPanel(),
            _ => State.ToggleBottomPanel()
        };
        ScheduleSave();
    }

    /// <summary>
    ///     视图菜单"重置布局"项的唯一路径：删除持久化配置，按 attribute 默认立即重建布局
    ///     （视图实例缓存保留，重建只改归属/顺序/显隐/尺寸/选中）
    /// </summary>
    private void ResetLayout()
    {
        _persistence.Delete();

        TopNavigationItems.Clear();
        BottomNavigationItems.Clear();
        AuxiliaryTabs.Clear();
        BottomTabs.Clear();
        _itemsById.Clear();
        _auxTabsById.Clear();
        _bottomTabsById.Clear();

        State = ShellLayoutState.Initial;
        PanelAlignment = PanelAlignment.Center;
        SideBarContent = null;
        SideBarTitle = null;

        LoadToolViews(null);
    }

    /// <summary>
    ///     把当前布局捕获为持久化 DTO：placements 取三处 Bar 的当前顺序（钉住项不入表），
    ///     显隐/尺寸/选中/活动 tab 取 State，对齐档位取镜像属性
    /// </summary>
    private ShellLayoutDto CaptureLayout()
    {
        var placements = new Dictionary<string, ToolViewPlacementEntry>();
        for (var i = 0; i < TopNavigationItems.Count; i++)
        {
            placements[TopNavigationItems[i].Id] = new ToolViewPlacementEntry
            {
                Bar = ToolViewPlacement.ActivityBar, Index = i
            };
        }
        for (var i = 0; i < AuxiliaryTabs.Count; i++)
        {
            placements[AuxiliaryTabs[i].Id] = new ToolViewPlacementEntry
            {
                Bar = ToolViewPlacement.AuxiliaryPanel, Index = i
            };
        }
        for (var i = 0; i < BottomTabs.Count; i++)
        {
            placements[BottomTabs[i].Id] = new ToolViewPlacementEntry
            {
                Bar = ToolViewPlacement.BottomPanel, Index = i
            };
        }

        return new ShellLayoutDto
        {
            PanelAlignment = PanelAlignment,
            Placements = placements,
            SideBar = new SideBarLayoutDto
            {
                Visible = State.SideBar.Visible, Width = State.SideBar.Width, Selected = State.SelectedActivity
            },
            AuxiliaryPanel = new PanelLayoutDto
            {
                Visible = State.AuxiliaryPanel.Visible,
                Width = State.AuxiliaryPanel.Width,
                ActiveTab = State.AuxiliaryPanel.ActiveTab
            },
            BottomPanel = new BottomPanelLayoutDto
            {
                Visible = State.BottomPanel.Visible,
                Height = State.BottomPanel.Height,
                ActiveTab = State.BottomPanel.ActiveTab
            }
        };
    }

    /// <summary>
    ///     布局变更的统一出口：所有变更点（显隐/尺寸/激活/选中/对齐）汇到这里防抖落盘
    /// </summary>
    private void ScheduleSave()
    {
        _persistence.ScheduleSave(CaptureLayout());
    }

    /// <summary>
    ///     装载一个面板的 tab：建立索引、激活配置指定的活动 tab（无配置或不匹配时默认首个）并同步到状态
    /// </summary>
    private void LoadPanelTabs(IReadOnlyList<ToolViewContribution> contributions, ToolViewPlacement panel,
        ObservableCollection<PanelTabViewModel> target, Dictionary<string, ToolViewContribution> index,
        string? preferredActiveTab, Action<object> setContent, Dictionary<string, object> contentCache)
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
        var activeTab = preferredActiveTab is { } preferred && tabs.Contains(preferred)
            ? preferred
            : tabs.FirstOrDefault();
        State = panel switch
        {
            ToolViewPlacement.AuxiliaryPanel => State with
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
        Dictionary<string, ToolViewContribution> index, string? activeTab,
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
            content = _containerProvider.Resolve(contribution.ViewType);
            contentCache[activeTab] = content;
        }

        setContent(content);
    }

    private void LoadItems(IEnumerable<ToolViewContribution> contributions,
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
