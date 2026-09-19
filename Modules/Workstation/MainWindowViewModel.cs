using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigitalWorkstation.Core.Abstractions.Commands;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Abstractions.Regions;
using DigitalWorkstation.Core.Framework.Contributions;
using DigitalWorkstation.Core.Framework.Layout;
using DigitalWorkstation.Core.Framework.Menus;
using DigitalWorkstation.Core.Models.Events;
using DigitalWorkstation.Workstation.Resources;
using DigitalWorkstation.Core.UIPackage;
using DigitalWorkstation.Workstation.Views;

namespace DigitalWorkstation.Workstation;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ShellContributionCollector _collector;
    private readonly IContainerProvider _containerProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly LayoutPersistence _persistence;
    private readonly EmptyStateView _homeContent;
    private readonly Dictionary<string, NavigationItemViewModel> _itemsById = new();
    private readonly Dictionary<string, IMainViewContribution> _mainViewsById = new();
    private readonly Dictionary<string, object> _mainViewContents = new();
    /// <summary>
    ///     全部工具视图（含钉住项）的元数据索引；归属随拖拽变化，索引不变
    /// </summary>
    private readonly Dictionary<string, ToolViewContribution> _contributionsById = new();
    private readonly Dictionary<string, PanelTabViewModel> _tabsById = new();
    /// <summary>
    ///     工具视图内容实例按 Id 单一缓存（ADR-0002 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：跨 Bar 迁移时实例随 tab 走，
    ///     切换再切回不丢状态；缓存永不失效，寿命 = 应用寿命
    /// </summary>
    private readonly Dictionary<string, object> _toolViewContents = new();
    private bool _contributionsLoaded;
    private IReadOnlyList<ToolViewContribution> _toolViews = [];

    public MainWindowViewModel(ShellContributionCollector collector, IContainerProvider containerProvider,
        IEventAggregator eventAggregator, LayoutPersistence persistence)
    {
        _collector = collector;
        _containerProvider = containerProvider;
        _eventAggregator = eventAggregator;
        _persistence = persistence;
        eventAggregator.GetEvent<OpenMainViewEvent>().Subscribe(OpenMainView);
        eventAggregator.GetEvent<ReturnHomeEvent>().Subscribe(ReturnHome);
        eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Subscribe(TogglePanel);
        eventAggregator.GetEvent<SetPanelAlignmentEvent>().Subscribe(SetPanelAlignment);
        eventAggregator.GetEvent<ResetLayoutEvent>().Subscribe(ResetLayout);
        // 拖拽会话期间临时显露隐藏面板，作为「向隐藏面板拖入」的投放区
        ToolViewDragSession.ActiveChanged += active => IsToolViewDragActive = active;
        _homeContent = containerProvider.Resolve<EmptyStateView>();
        _mainContent = _homeContent;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SideBarColumnWidth), nameof(AuxiliaryColumnWidth),
        nameof(AuxiliaryPanelRevealed), nameof(BottomPanelRevealed))]
    private ShellLayoutState _state = ShellLayoutState.Initial;

    /// <summary>
    ///     工具视图拖拽会话进行中（Framework ToolViewDragSession 驱动）：隐藏面板临时显露
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AuxiliaryPanelRevealed), nameof(BottomPanelRevealed),
        nameof(AuxiliaryColumnWidth))]
    private bool _isToolViewDragActive;

    /// <summary>
    ///     AuxiliaryPanel 是否应当呈现：持久可见，或拖拽会话期间临时显露以接受投放
    /// </summary>
    public bool AuxiliaryPanelRevealed => State.AuxiliaryPanel.Visible || IsToolViewDragActive;

    /// <summary>
    ///     BottomPanel 是否应当呈现：规则同 AuxiliaryPanelRevealed
    /// </summary>
    public bool BottomPanelRevealed => State.BottomPanel.Visible || IsToolViewDragActive;

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
    ///     菜单栏：全部菜单贡献经 MenuTreeBuilder 建树生成（ADR-0001 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）——顶层不分组不插分隔线，
    ///     子菜单按 Group/Order 分组排序、组间插分隔线
    /// </summary>
    public ObservableCollection<MenuItemViewModel> MenuBarItems { get; } = [];


    /// <summary>
    ///     状态栏条目：shell 预置项与模块贡献项按 Order 统一排序
    /// </summary>
    public ObservableCollection<StatusBarItemViewModel> StatusBarItems { get; } = [];

    /// <summary>
    ///     全部命令贡献（ADR-0005 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）：命令面板数据源与手势 KeyBinding 来源，一次性收集
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<ICommandContribution> _commands = [];

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
    ///     ActivityBar 底部"设置"入口按钮的图标几何（纯导航按钮，非工具视图，ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 6）
    /// </summary>
    public Geometry SettingsIcon { get; } = StreamGeometry.Parse(Icons.Settings);

    /// <summary>
    ///     ActivityBar 底部"设置"入口按钮的标题（工具提示）
    /// </summary>
    public string SettingsTitle => WorkstationResources.SettingsNavigationTitle;

    /// <summary>
    ///     SideBar 列宽：由 Framework 的共享尺寸规则投影，隐藏时归零，
    ///     BottomPanel 的跨度随之自然伸缩
    /// </summary>
    public GridLength SideBarColumnWidth =>
        ShellLayoutMetrics.PanelColumn(State.SideBar.Width, State.SideBar.Visible);

    /// <summary>
    ///     AuxiliaryPanel 列宽：规则同 SideBarColumnWidth
    /// </summary>
    public GridLength AuxiliaryColumnWidth =>
        ShellLayoutMetrics.PanelColumn(State.AuxiliaryPanel.Width, AuxiliaryPanelRevealed);

    /// <summary>
    ///     收集模块贡献的导航项。模块在 Prism 模块初始化阶段（晚于 shell 创建）才注册贡献，
    ///     因此由启动序列在模块准备完成后、Ready 前触发，且只收集一次
    /// </summary>
    public void EnsureContributionsLoaded()
    {
        if (_contributionsLoaded)
        {
            return;
        }

        _toolViews = _collector.GetToolViews();
        LoadToolViews(_persistence.Load());
        foreach (var mainView in _collector.GetMainViews())
        {
            _mainViewsById[mainView.Id] = mainView;
        }
        foreach (var submenu in MenuTreeBuilder.Build(_collector.GetMenuItems()))
        {
            MenuBarItems.Add(MenuItemViewModel.FromSubmenu(submenu, isTopLevel: true));
        }
        foreach (var item in _collector.GetStatusBarItems())
        {
            StatusBarItems.Add(new StatusBarItemViewModel(item));
        }
        Commands = _collector.GetCommands();
        _contributionsLoaded = true;
    }

    /// <summary>
    ///     工具视图分派到三处 Bar 并恢复持久化布局：钉住项（AllowMove=false）恒落 ActivityBar 底部段；
    ///     可移动项「配置优先、默认兜底」——持久化 placements 优先于 attribute Default，孤儿条目随贡献迭代
    ///     自然丢弃，无配置条目的新工具视图按 Order 追加到 Default Bar 末尾；layout 为 null（含重置）时全部走默认
    /// </summary>
    private void LoadToolViews(ShellLayoutDto? layout)
    {
        foreach (var toolView in _toolViews)
            _contributionsById[toolView.Id] = toolView;

        var restored = ShellLayoutConfiguration.Restore(_toolViews, layout, State.MainContent);
        ApplyLayout(restored.State, restored.Alignment, persist: false);
    }

    /// <summary>
    ///     点击导航项：切换选中并驱动 SideBar 展开/收起；内容视图按导航项缓存，收起再展开不丢
    /// </summary>
    [RelayCommand]
    private void SelectActivity(NavigationItemViewModel item)
    {
        ApplyLayout(State.SelectActivity(item.Id));
    }

    /// <summary>
    ///     ActivityBar 底部"设置"入口：纯导航（非工具视图，ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 6），
    ///     发布 OpenMainViewEvent 打开设置页主视图
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        _eventAggregator.GetEvent<OpenMainViewEvent>().Publish(WellKnownViews.Settings);
    }

    private void ReturnHome()
    {
        if (ReferenceEquals(MainContent, _homeContent) && State.MainContent.ActiveView is null)
        {
            return;
        }

        State = State with { MainContent = State.MainContent with { ActiveView = null } };
        MainContent = _homeContent;
    }

    /// <summary>
    ///     工具视图内容实例的统一入口：同一 Id 全应用单实例缓存，跨 Bar 迁移时实例随 tab 走
    /// </summary>
    private object ContentFor(string id)
    {
        if (!_toolViewContents.TryGetValue(id, out var content))
        {
            content = _containerProvider.Resolve(_contributionsById[id].ViewType);
            _toolViewContents[id] = content;
        }

        return content;
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

        if (!_mainViewContents.TryGetValue(viewId, out var content))
        {
            content = _containerProvider.Resolve(contribution.ViewType);
            _mainViewContents[viewId] = content;
        }

        State = State.OpenMainView(viewId);
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

        ApplyLayout(next);
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

        ApplyLayout(next);
    }

    /// <summary>
    ///     收起按钮：独立翻转 AuxiliaryPanel 可见性，活动 tab 记录保留（快捷键走 ViewCommands 的命令 Gesture）
    /// </summary>
    [RelayCommand]
    private void ToggleAuxiliaryPanel()
    {
        TogglePanel(TogglePanelTarget.AuxiliaryPanel);
    }

    /// <summary>
    ///     收起按钮：独立翻转 BottomPanel 可见性，活动 tab 记录保留（快捷键走 ViewCommands 的命令 Gesture）
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
        ApplyLayout(State, alignment);
    }

    /// <summary>
    ///     分隔条拖拽的唯一路径：PanelResizer 换算方向后经命令汇到这里；
    ///     增量经状态转换应用并 clamp 到合法区间；BottomPanel 调高度，其余调宽度；
    ///     面板收起时尺寸记录保留，恢复后不重置
    /// </summary>
    [RelayCommand]
    private void ResizePanel(PanelResize resize)
    {
        ApplyLayout(State.Resize(resize.Target, resize.Delta));
    }

    /// <summary>
    ///     工具视图拖拽落放的唯一路径：Framework 的 ToolViewBar 在 Drop 时经 MoveCommand 汇到这里；
    ///     钉住项（AllowMove=false）与不属于任何 Bar 的 Id 在此拒绝发起的状态转换
    /// </summary>
    [RelayCommand]
    private void MoveTab(ToolViewMove move)
    {
        if (!_contributionsById.TryGetValue(move.TabId, out var contribution) || !contribution.AllowMove)
        {
            return;
        }

        var next = State.MoveTab(move.TabId, move.TargetBar, move.Index);
        if (ReferenceEquals(next, State))
        {
            return;
        }

        ApplyLayout(next);
    }

    /// <summary>
    ///     面板显隐切换的唯一路径：快捷键、菜单项与收起按钮都汇到这里
    /// </summary>
    private void TogglePanel(TogglePanelTarget target)
    {
        var next = target switch
        {
            TogglePanelTarget.SideBar => State.ToggleSideBar(),
            TogglePanelTarget.AuxiliaryPanel => State.ToggleAuxiliaryPanel(),
            TogglePanelTarget.BottomPanel => State.ToggleBottomPanel(),
            _ => State
        };
        ApplyLayout(next);
    }

    /// <summary>
    ///     视图菜单"重置布局"项的唯一路径：删除持久化配置，按 attribute 默认立即重建布局
    ///     （视图实例缓存保留，重建只改归属/顺序/显隐/尺寸/选中）
    /// </summary>
    private void ResetLayout()
    {
        // 重置只改变布局；主视图状态与实例都保留。所有宿主由 ApplyLayout 同步。
        LoadToolViews(null);
        _persistence.Delete();
    }

    /// <summary>
    ///     布局变更的统一提交：先准备目标内容，全部脱离变化的旧宿主，再同步状态、集合与内容，
    ///     最后从同一状态捕获配置。恢复和重置也经过此出口，但不调度保存。
    /// </summary>
    private void ApplyLayout(ShellLayoutState next, PanelAlignment? alignment = null, bool persist = true)
    {
        object? Content(string? id) =>
            id is not null && _contributionsById.ContainsKey(id) ? ContentFor(id) : null;
        var side = Content(next.SideBar.ContentFor);
        var auxiliary = Content(next.AuxiliaryPanel.ActiveTab);
        var bottom = Content(next.BottomPanel.ActiveTab);
        if ((side is not null && (ReferenceEquals(side, auxiliary) || ReferenceEquals(side, bottom))) ||
            (auxiliary is not null && ReferenceEquals(auxiliary, bottom)))
            throw new InvalidOperationException("A tool view cannot occupy multiple layout hosts.");

        var nextAlignment = alignment ?? PanelAlignment;
        var templateChanged = nextAlignment != PanelAlignment;
        if (templateChanged || !ReferenceEquals(SideBarContent, side)) SideBarContent = null;
        if (templateChanged || !ReferenceEquals(AuxiliaryContent, auxiliary)) AuxiliaryContent = null;
        if (templateChanged || !ReferenceEquals(BottomContent, bottom)) BottomContent = null;

        State = next;
        PanelAlignment = nextAlignment;
        SyncBarCollection(TopNavigationItems, next.ActivityBarItems, _itemsById,
            item => item.Id, id => new NavigationItemViewModel(_contributionsById[id]));
        SyncBarCollection(BottomNavigationItems,
            _toolViews.Where(view => !view.AllowMove && view.Placement == ToolViewPlacement.ActivityBar)
                .Select(view => view.Id).ToArray(), _itemsById,
            item => item.Id, id => new NavigationItemViewModel(_contributionsById[id]));
        SyncBarCollection(AuxiliaryTabs, next.AuxiliaryPanel.Tabs, _tabsById,
            tab => tab.Id, id => new PanelTabViewModel(_contributionsById[id]));
        SyncBarCollection(BottomTabs, next.BottomPanel.Tabs, _tabsById,
            tab => tab.Id, id => new PanelTabViewModel(_contributionsById[id]));

        foreach (var item in _itemsById.Values) item.IsSelected = item.Id == next.SelectedActivity;
        foreach (var tab in AuxiliaryTabs) tab.IsActive = tab.Id == next.AuxiliaryPanel.ActiveTab;
        foreach (var tab in BottomTabs) tab.IsActive = tab.Id == next.BottomPanel.ActiveTab;
        SideBarTitle = next.SideBar.ContentFor is { } id && _contributionsById.TryGetValue(id, out var contribution)
            ? contribution.Title : null;
        SideBarContent = side;
        AuxiliaryContent = auxiliary;
        BottomContent = bottom;

        if (persist)
            _persistence.ScheduleSave(ShellLayoutConfiguration.Capture(next, nextAlignment));
    }

    /// <summary>
    ///     把一个 Bar 的呈现集合对齐到 State 的有序 Id 列表：移出项删除、缺失项经工厂创建并缓存、
    ///     错位项移动；跨 Bar 迁移时 ViewModel 实例经 itemsById 缓存复用，IsSelected/IsActive 不丢
    /// </summary>
    private static void SyncBarCollection<TItem>(ObservableCollection<TItem> collection,
        IReadOnlyList<string> orderedIds, Dictionary<string, TItem> itemsById,
        Func<TItem, string> idOf, Func<string, TItem> factory)
    {
        for (var i = collection.Count - 1; i >= 0; i--)
        {
            if (!orderedIds.Contains(idOf(collection[i])))
            {
                collection.RemoveAt(i);
            }
        }

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var id = orderedIds[i];
            var currentIndex = -1;
            for (var j = 0; j < collection.Count; j++)
            {
                if (idOf(collection[j]) == id)
                {
                    currentIndex = j;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                if (!itemsById.TryGetValue(id, out var item))
                {
                    itemsById[id] = item = factory(id);
                }

                collection.Insert(i, item);
            }
            else if (currentIndex != i)
            {
                collection.Move(currentIndex, i);
            }
        }
    }
}
