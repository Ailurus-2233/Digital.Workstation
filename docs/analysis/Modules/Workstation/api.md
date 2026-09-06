# Workstation — 对外接口与调用方式

命名空间三组：`DigitalWorkstation.Workstation`（根，宿主与主窗口）、`DigitalWorkstation.Workstation.Shell`（预置贡献）、`DigitalWorkstation.Workstation.Views`（内置视图）。除注明外全部 public。

## 1. `WorkstationApplication`（WorkstationApplication.cs:12）

```csharp
public class WorkstationApplication : FrameworkApplication<MainWindow>
```

应用入口类，由 Launcher 启动（`App` 入口实例化它，见 reference.md 被依赖关系）。三个重写成员：

| 成员 | 签名/位置 | 行为 |
|---|---|---|
| `ConfigureModuleCatalog` | `protected override void`（:14-17） | 仅一行 `moduleCatalog.AddModule<DashBoardModule>()`——把 DashBoard 模块纳入逐模块加载清单 |
| `RegisterCustomService` | `protected override void RegisterCustomService(IContainerRegistry)`（:19-46） | 注册全部 shell 预置贡献与内置视图（清单见下） |
| `CreateSplashWindow` | `protected override Window CreateSplashWindow()`（:51-54） | `Container.Resolve<DashBoardWindow>()`，启动台窗口（ADR-0004） |

### RegisterCustomService 注册清单（:21-45，逐行）

| 注册 | 类型 | 说明 |
|---|---|---|
| `RegisterSingleton<INavigationItemContribution, SettingsNavigationItem>()` | 贡献 | "设置"导航项（ActivityBar 底部） |
| `Register<EmptyStateView>()` | 视图（瞬态） | MainContent 空状态页，MainWindowViewModel 构造时解析 |
| `RegisterSingleton<IPanelTabContribution, PropertiesPanelTab>()` / `OutlinePanelTab` / `OutputPanelTab` / `LogPanelTab` | 贡献 ×4 | AuxiliaryPanel"属性/大纲"、BottomPanel"输出/日志"演示 tab |
| `Register<PropertiesView>()` / `OutlineView` / `OutputView` / `LogView` | 视图（瞬态） | 四个 tab 的内容视图 |
| `RegisterSingleton<IMenuItemContribution, ExitMenuItem>()` / `AboutMenuItem` | 贡献 ×2 | 文件>退出、帮助>关于 |
| 循环 `TogglePanelTarget.SideBar/BottomPanel/AuxiliaryPanel` 各注册一次 `IMenuItemContribution`（:37-41） | 贡献 ×3 | 工厂 `provider => new TogglePanelContribution(provider.Resolve<IEventAggregator>(), target)`，视图菜单三个面板显隐切换项 |
| `RegisterSingleton<IStatusBarItemContribution, ReadyStatusBarItem>()` | 贡献 | 状态栏"就绪" |
| `Register<AboutWindow>()` | 窗口（瞬态） | "关于"对话框，经 `IWindowManager` 按需解析 |

## 2. `MainWindowViewModel`（MainWindowViewModel.cs:13）

```csharp
public partial class MainWindowViewModel : ObservableObject
```

构造注入：`MainWindowViewModel(ShellContributionCollector collector, IContainerProvider containerProvider, IEventAggregator eventAggregator)`（:27-35）。由 Prism ViewModelLocator 自动装配（`MainWindow.axaml:11` `prism:ViewModelLocator.AutoWireViewModel="True"`）。构造函数订阅 `OpenMainViewEvent`→`OpenMainView`、`TogglePanelVisibilityEvent`→`TogglePanel`，并把 `_mainContent` 初始化为 `containerProvider.Resolve<EmptyStateView>()`。

### 可绑定属性（XAML 消费面，绑定点见 MainWindow.axaml）

| 属性 | 类型 | 语义 |
|---|---|---|
| `State` | `ShellLayoutState`（[ObservableProperty]，:38，初值 `ShellLayoutState.Initial`） | 整个布局状态：面板显隐/宽高、选中导航项、活动 tab、活动主视图 |
| `SideBarContent` / `SideBarTitle` | `object?` / `string?`（:41、:49） | SideBar 当前内容与区域标题 |
| `MainContent` | `object`（:46） | 主区当前内容；初始为 `EmptyStateView`，被 `OpenMainView` 整体替换 |
| `AuxiliaryContent` / `BottomContent` | `object?`（:81、:87） | 两个面板当前活动 tab 的内容 |
| `TopNavigationItems` / `BottomNavigationItems` | `ObservableCollection<NavigationItemViewModel>`（:51、:53） | ActivityBar 顶部/底部导航项 |
| `AuxiliaryTabs` / `BottomTabs` | `ObservableCollection<PanelTabViewModel>`（:54、:56） | 两个面板的 tab 栏 |
| `FileMenuItems` / `ViewMenuItems` / `HelpMenuItems` | `ObservableCollection<MenuItemViewModel>`（:60、:65、:70） | 三个顶层菜单 |
| `StatusBarItems` | `ObservableCollection<StatusBarItemViewModel>`（:75） | 状态栏条目 |
| `CollapseBottomIcon` / `CollapseAuxiliaryIcon` | `Geometry`（:91 `Icons.ChevronDown`、:96 `Icons.ChevronRight`） | 两个面板收起按钮图标 |

### 命令（[RelayCommand] 生成，XAML 绑定名 = 方法名 + Command）

| 命令 | 源方法 | 行为 |
|---|---|---|
| `SelectActivityCommand` | `SelectActivity(NavigationItemViewModel)`（:133） | 选中导航项并驱动 SideBar 展开/收起；遍历 `_itemsById.Values` 把每项 `IsSelected` 设为 `Id == State.SelectedActivity`；随后若 `State.SideBar.Visible` 为 false 或 `State.SideBar.ContentFor` 为空则**提前 return**（:142-145，标题与内容保持不变）；否则按 `ContentFor` 的 Id 取/建缓存内容，`SideBarTitle` 取 `_itemsById[contentId].Title`（:153）、`SideBarContent` 设为该内容实例 |
| `ActivateAuxTabCommand` | `ActivateAuxTab(PanelTabViewModel)`（:180） | 激活 AuxiliaryPanel tab：先 `State.ActivateAuxTab(tab.Id)` 得 `next`；面板收起时 `ShellLayoutState` 拒绝并原样返回原实例，`ReferenceEquals(next, State)`（:183）为 true → 直接 return，不赋 `State`、不调 `SyncActiveTab`；状态变化时 `State = next` 并调 `SyncActiveTab` 同步各 tab 的 `IsActive` 与缓存的内容视图 |
| `ActivateBottomTabCommand` | `ActivateBottomTab(PanelTabViewModel)`（:197） | 同上，针对 BottomPanel（`State.ActivateBottomTab(tab.Id)`，:199） |
| `ToggleSideBarCommand` | `ToggleSideBar()`（:214） | Ctrl+B；转发 `TogglePanel(TogglePanelTarget.SideBar)` |
| `ToggleAuxiliaryPanelCommand` | `ToggleAuxiliaryPanel()`（:223） | Ctrl+Alt+B 或收起按钮 |
| `ToggleBottomPanelCommand` | `ToggleBottomPanel()`（:232） | Ctrl+J 或收起按钮 |

### 公开方法

| 方法 | 签名/位置 | 调用方 |
|---|---|---|
| `EnsureContributionsLoaded` | `public void`（:102-127） | 仅 `MainWindow.axaml.cs:42` 的 `OnOpened`；`_contributionsLoaded` 守卫保证只执行一次。方法体内依次调用 `ShellContributionCollector`（`_collector`）的五个收集方法：**`GetNavigationItems(NavigationItemPlacement.Top)` 与 `(…Bottom)`** 经 `LoadItems` 填充 `TopNavigationItems`/`BottomNavigationItems` 与 `_itemsById`；**`GetMainViews()`** 把每个 `IMainViewContribution` 按 `Id` 存入 `_mainViewsById`（:112-115）；**`GetPanelTabs(PanelPlacement.Auxiliary)` 与 `(…Bottom)`** 经 `LoadPanelTabs` 填充两个 tab 集合与索引字典并写入 `State`；**`GetMenuItems(MenuPlacement.File/View/Help)`** 经 `LoadChrome` 填充三个菜单集合；**`GetStatusBarItems()`** 逐项包装为 `StatusBarItemViewModel` 加入 `StatusBarItems`（:123-126） |
| `ResizePanel` | `public void ResizePanel(PanelResizeTarget target, double delta)`（:241-244） | 仅 `MainWindow.axaml.cs` 三个拖拽 handler；`State = State.Resize(target, delta)` |

### 私有方法（改行为时直接面对）

`TogglePanel(TogglePanelTarget)`（:249，switch：SideBar→`State.ToggleSideBar()`、AuxiliaryPanel→`State.ToggleAuxiliaryPanel()`、默认 `_`→`State.ToggleBottomPanel()`）、`OpenMainView(string viewId)`（:159，未知 Id 静默返回）、`LoadPanelTabs`（:271-302：`contributions.Count == 0` 直接返回；逐贡献建 `PanelTabViewModel`、登记 `index` 字典并加入目标集合；取全部 tab Id 数组与 `tabs.FirstOrDefault()`（首个 tab）作默认 `ActiveTab`，用 `State with { … }` 按 `contributions[0].Panel` 选择更新 `AuxiliaryPanel` 或 `BottomPanel` 的 `Tabs`/`ActiveTab`（:289-299）；末尾调 `SyncActiveTab` 同步 `IsActive` 与首 tab 内容）、`SyncActiveTab`（:307-328：遍历 tab 集合设 `IsActive = (tab.Id == activeTab)`；`activeTab` 为 null 或索引查不到时返回；缓存未命中 `_containerProvider.Resolve(contribution.ContentViewType)` 写入内容缓存；经 `setContent` 回调赋给 `AuxiliaryContent`/`BottomContent`）、`LoadChrome`（:259）、`LoadItems`（:330）。

## 3. 呈现模型（包装贡献元数据，构造时解析图标几何）

四个类结构同构：构造接收对应贡献接口，`Icon = StreamGeometry.Parse(contribution.IconPath)`，透传 `Id`/`Title`。

| 类（文件） | 基类 | 包装 | 额外成员 |
|---|---|---|---|
| `NavigationItemViewModel`（NavigationItemViewModel.cs:10） | `ObservableObject` | `INavigationItemContribution`（`Contribution` 属性公开） | `[ObservableProperty] bool IsSelected`（:30） |
| `MenuItemViewModel`（MenuItemViewModel.cs:10） | 无（普通类） | `IMenuItemContribution` | `ICommand Command => Contribution.Command`（:29） |
| `PanelTabViewModel`（PanelTabViewModel.cs:10） | `ObservableObject` | `IPanelTabContribution`（`Contribution` 属性公开） | `[ObservableProperty] bool IsActive`（:30） |
| `StatusBarItemViewModel`（StatusBarItemViewModel.cs:9） | 无 | `IStatusBarItemContribution` | — |

## 4. `PanelResizer`（PanelResizer.cs:9）

```csharp
public class PanelResizer : GridSplitter
```

XAML 用法（`MainWindow.axaml:252-262` 等三处）：设置 `ResizeDirection`（Columns/Rows）、`DragDelta="On…ResizerDragDelta"`（事件在 code-behind 处理）、`Width`/`Height` 8px 热区、`ZIndex="1"`。两个重写：`StyleKeyOverride => typeof(GridSplitter)`（继承主题）、`GetParentGrid() => null`（禁用原生重排，见 pitfalls.md）。

## 5. Shell 预置贡献（Shell/ 目录，实现 Abstractions 的 `I*Contribution` 接口）

属性矩阵（Id/Title 键/IconPath 常量/Order/定位/行为）：

| 类（文件） | 接口 | Id | Title | IconPath | Order | 定位 | 行为字段 |
|---|---|---|---|---|---|---|---|
| `SettingsNavigationItem`（SettingsNavigationItem.cs:11） | `INavigationItemContribution` | `shell.settings` | `Language.SettingsNavigationTitle` | `Icons.Settings` | 0 | `NavigationItemPlacement.Bottom` | `ContentViewType = typeof(SettingsView)` |
| `PropertiesPanelTab`（PropertiesPanelTab.cs:11） | `IPanelTabContribution` | `shell.properties` | `Language.PropertiesTabTitle` | `Icons.Properties` | 10 | `PanelPlacement.Auxiliary` | `ContentViewType = typeof(PropertiesView)` |
| `OutlinePanelTab`（OutlinePanelTab.cs:11） | `IPanelTabContribution` | `shell.outline` | `Language.OutlineTabTitle` | `Icons.Outline` | 20 | `PanelPlacement.Auxiliary` | `ContentViewType = typeof(OutlineView)` |
| `OutputPanelTab`（OutputPanelTab.cs:11） | `IPanelTabContribution` | `shell.output` | `Language.OutputTabTitle` | `Icons.Output` | 10 | `PanelPlacement.Bottom` | `ContentViewType = typeof(OutputView)` |
| `LogPanelTab`（LogPanelTab.cs:11） | `IPanelTabContribution` | `shell.log` | `Language.LogTabTitle` | `Icons.Log` | 20 | `PanelPlacement.Bottom` | `ContentViewType = typeof(LogView)` |
| `ExitMenuItem`（ExitMenuItem.cs:14） | `IMenuItemContribution` | `shell.menu.exit` | `Language.MenuExitTitle` | `Icons.Exit` | 100 | `MenuPlacement.File` | `Command`：`DelegateCommand` → `(Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown()`（:26-27）；Order 取大值 100 保持文件菜单末尾（:11-13 注释） |
| `AboutMenuItem`（AboutMenuItem.cs:13） | `IMenuItemContribution` | `shell.menu.about` | `Language.MenuAboutTitle` | `Icons.About` | 10 | `MenuPlacement.Help` | 构造注入 `IWindowManager`，`Command = new DelegateCommand(windowManager.ShowDialog<AboutWindow>)`（:15-18） |
| `TogglePanelContribution`（TogglePanelContribution.cs:13） | `IMenuItemContribution` | `shell.toggle-{Target.ToString().ToLowerInvariant()}`（:27） | switch（:29-34） | switch（:36-41） | switch（:43-48） | `MenuPlacement.View`（:50） | 构造注入 `IEventAggregator` + `TogglePanelTarget`，`Command = new DelegateCommand(() => eventAggregator.GetEvent<TogglePanelVisibilityEvent>().Publish(target))`（:18-19） |
| `ReadyStatusBarItem`（ReadyStatusBarItem.cs:10） | `IStatusBarItemContribution` | `shell.status.ready` | `Language.StatusReadyTitle` | `Icons.Ready` | 10 | — | — |

### `TogglePanelContribution` 三个 switch 的精确映射（消费方关键事实）

构造：`TogglePanelContribution(IEventAggregator eventAggregator, TogglePanelTarget target)`，`Target` 属性只读公开。

| Target | Id | Title（:29-34） | IconPath（:36-41） | Order（:43-48） |
|---|---|---|---|---|
| `SideBar` | `shell.toggle-sidebar` | `Language.ToggleSideBarTitle` | `Icons.PanelLeft` | **10** |
| `BottomPanel` | `shell.toggle-bottompanel` | `Language.ToggleBottomPanelTitle`（默认分支） | `Icons.PanelBottom`（默认分支） | **20**（显式 case） |
| `AuxiliaryPanel` | `shell.toggle-auxiliarypanel` | `Language.ToggleAuxiliaryPanelTitle` | `Icons.PanelRight` | **30**（默认分支） |

**注意 switch 结构不对称**：Title/IconPath 中 `SideBar`、`AuxiliaryPanel` 是显式 case，`BottomPanel` 走 `_` 默认分支；Order 中 `SideBar`(10)、`BottomPanel`(20) 是显式 case，`AuxiliaryPanel`(30) 走 `_` 默认分支。**`TogglePanelTarget` 新增成员时，Title/IconPath 静默映射为 BottomPanel 的文案与图标、Order 静默为 30，不报错**（详见 pitfalls.md）。注册方在 `WorkstationApplication.cs:37-41` 显式枚举三个 target 各注册一个实例。

## 6. 内置视图（Views/，均为无逻辑占位）

`SettingsView`/`PropertiesView`/`OutlineView`/`OutputView`/`LogView` 均为 `UserControl`，axaml 里只有一行"XX（占位）" `TextBlock`，code-behind 仅 `InitializeComponent()`。`EmptyStateView`（EmptyStateView.axaml）是静态快捷键提示页（Ctrl+B/Ctrl+J/Ctrl+Alt+B 三个键帽 + 引导文案）。`AboutWindow`（AboutWindow.axaml）：`Window`，360×160、`CanResize="False"`、`WindowStartupLocation="CenterOwner"`，标题与正文为**硬编码中文**（未走 `Language`，见 pitfalls.md）。

## 典型调用序列

模块向 shell 贡献东西**不需要引用本模块**：在模块的 `RegisterTypes` 里 `containerRegistry.RegisterSingleton<INavigationItemContribution, MyItem>()` 等，本模块的 `EnsureContributionsLoaded` 收集并渲染。打开主视图：`IEventAggregator.GetEvent<OpenMainViewEvent>().Publish("my.view.id")`（真实调用点：DashBoard 模块 `DashBoardNavigationView.axaml.cs:30、35`）。切换面板：`Publish(TogglePanelTarget.SideBar)` 到 `TogglePanelVisibilityEvent`（真实发布点：`Shell/TogglePanelContribution.cs:19`）。弹窗：注入 `IWindowManager` 调 `ShowDialog<AboutWindow>()`（真实调用点：`Shell/AboutMenuItem.cs:17`）。
