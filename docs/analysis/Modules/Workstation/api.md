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

### RegisterCustomService 注册清单（:20-51，逐行）

| 注册 | 类型 | 说明 |
|---|---|---|
| `RegisterSingleton<INavigationItemContribution, SettingsNavigationItem>()` | 贡献 | "设置"导航项（ActivityBar 底部） |
| `Register<EmptyStateView>()` | 视图（瞬态） | MainContent 空状态页，MainWindowViewModel 构造时解析 |
| `RegisterSingleton<IPanelTabContribution, PropertiesPanelTab>()` / `OutlinePanelTab` / `OutputPanelTab` / `LogPanelTab` | 贡献 ×4 | AuxiliaryPanel"属性/大纲"、BottomPanel"输出/日志"演示 tab |
| `Register<PropertiesView>()` / `OutlineView` / `OutputView` / `LogView` | 视图（瞬态） | 四个 tab 的内容视图 |
| `RegisterSingleton<IMenuItemContribution, ExitMenuItem>()` / `AboutMenuItem` | 贡献 ×2 | 文件>退出、帮助>关于 |
| 循环 `TogglePanelTarget.SideBar/BottomPanel/AuxiliaryPanel` 各注册一次 `IMenuItemContribution`（:37-41） | 贡献 ×3 | 工厂 `provider => new TogglePanelContribution(provider.Resolve<IEventAggregator>(), target)`，视图菜单三个面板显隐切换项 |
| 循环 `Enum.GetValues<PanelAlignment>()` 各注册一次 `IMenuItemContribution`（:43-47） | 贡献 ×4 | 工厂 `provider => new PanelAlignmentContribution(provider.Resolve<IEventAggregator>(), alignment)`，视图菜单四档面板对齐项（显隐组与对齐组之间的分隔符由 ViewModel 收集后插入） |
| `RegisterSingleton<IStatusBarItemContribution, ReadyStatusBarItem>()` | 贡献 | 状态栏"就绪" |
| `Register<AboutWindow>()` | 窗口（瞬态） | "关于"对话框，经 `IWindowManager` 按需解析 |

## 2. `MainWindowViewModel`（MainWindowViewModel.cs:14）

```csharp
public partial class MainWindowViewModel : ObservableObject
```

构造注入：`MainWindowViewModel(ShellContributionCollector collector, IContainerProvider containerProvider, IEventAggregator eventAggregator)`（:29-38）。由 Prism ViewModelLocator 自动装配（`MainWindow.axaml:12` `prism:ViewModelLocator.AutoWireViewModel="True"`）。构造函数订阅 `OpenMainViewEvent`→`OpenMainView`、`TogglePanelVisibilityEvent`→`TogglePanel`、`SetPanelAlignmentEvent`→`SetPanelAlignment`（:33-36），并把 `_mainContent` 初始化为 `containerProvider.Resolve<EmptyStateView>()`。

### 可绑定属性（XAML 消费面，绑定点见 MainWindow.axaml 与 Framework 的 `FrameworkWindowTheme.axaml`——后者是宽松反射绑定，Framework 不引用本类型）

| 属性 | 类型 | 语义 |
|---|---|---|
| `State` | `ShellLayoutState`（[ObservableProperty]，:40，初值 `ShellLayoutState.Initial`；带 `[NotifyPropertyChangedFor(nameof(SideBarColumnWidth), nameof(AuxiliaryColumnWidth))]`，:39） | 整个布局状态：面板显隐/宽高、选中导航项、活动 tab、活动主视图。**面板对齐档位不在其中**（见下行） |
| `PanelAlignment` | `PanelAlignment`（[ObservableProperty]，:48，初值 `PanelAlignment.Center`） | 面板对齐档位（左/右/居中/两端）：与 `FrameworkWindow.PanelAlignment` 双向绑定的镜像属性——窗口依赖属性是布局定义的唯一入口（:42-45 注释）；由 `SetPanelAlignmentEvent` 订阅写入 |
| `SideBarContent` / `SideBarTitle` | `object?` / `string?`（:52、:60） | SideBar 当前内容与区域标题 |
| `MainContent` | `object`（:57） | 主区当前内容；初始为 `EmptyStateView`，被 `OpenMainView` 整体替换 |
| `AuxiliaryContent` / `BottomContent` | `object?`（:92、:98） | 两个面板当前活动 tab 的内容 |
| `TopNavigationItems` / `BottomNavigationItems` | `ObservableCollection<NavigationItemViewModel>`（:62、:64） | ActivityBar 顶部/底部导航项 |
| `AuxiliaryTabs` / `BottomTabs` | `ObservableCollection<PanelTabViewModel>`（:65、:67） | 两个面板的 tab 栏 |
| `FileMenuItems` / `ViewMenuItems` / `HelpMenuItems` | `ObservableCollection<MenuItemViewModel>`（:71、:76、:81） | 三个顶层菜单 |
| `StatusBarItems` | `ObservableCollection<StatusBarItemViewModel>`（:86） | 状态栏条目 |
| `CollapseBottomIcon` / `CollapseAuxiliaryIcon` | `Geometry`（:102 `Icons.ChevronDown`、:107 `Icons.ChevronRight`） | 两个面板收起按钮图标 |
| `SideBarColumnWidth` / `AuxiliaryColumnWidth` | `GridLength`（:113-114、:118-119） | 可见时面板宽度 + 4px 间隙（布局模板间隙约定），隐藏归零使 BottomPanel 跨度自然伸缩；供 Framework 布局模板的列宽绑定 |

### 命令（[RelayCommand] 生成，XAML 绑定名 = 方法名 + Command）

| `SelectActivityCommand` | `SelectActivity(NavigationItemViewModel)`（:166） | 选中导航项并驱动 SideBar 展开/收起；遍历 `_itemsById.Values` 把每项 `IsSelected` 设为 `Id == State.SelectedActivity`；随后若 `State.SideBar.Visible` 为 false 或 `State.SideBar.ContentFor` 为空则**提前 return**（:175-178，标题与内容保持不变）；否则按 `ContentFor` 的 Id 取/建缓存内容，`SideBarTitle` 取 `_itemsById[contentId].Title`（:186）、`SideBarContent` 设为该内容实例 |
| `ActivateAuxTabCommand` | `ActivateAuxTab(PanelTabViewModel)`（:213） | 激活 AuxiliaryPanel tab：先 `State.ActivateAuxTab(tab.Id)` 得 `next`；面板收起时 `ShellLayoutState` 拒绝并原样返回原实例，`ReferenceEquals(next, State)`（:216）为 true → 直接 return，不赋 `State`、不调 `SyncActiveTab`；状态变化时 `State = next` 并调 `SyncActiveTab` 同步各 tab 的 `IsActive` 与缓存的内容视图 |
| `ActivateBottomTabCommand` | `ActivateBottomTab(PanelTabViewModel)`（:230） | 同上，针对 BottomPanel（`State.ActivateBottomTab(tab.Id)`，:232） |
| `ToggleSideBarCommand` | `ToggleSideBar()`（:247） | Ctrl+B；转发 `TogglePanel(TogglePanelTarget.SideBar)` |
| `ToggleAuxiliaryPanelCommand` | `ToggleAuxiliaryPanel()`（:256） | Ctrl+Alt+B 或收起按钮 |
| `ToggleBottomPanelCommand` | `ToggleBottomPanel()`（:265） | Ctrl+J 或收起按钮 |
| `ResizePanelCommand` | `ResizePanel(PanelResize)`（:285） | 分隔条拖拽的唯一路径：Framework 的 `PanelResizer` 经 `Target` + `ResizeCommand` 声明式调用（code-behind 不再参与拖拽）；`State = State.Resize(resize.Target, resize.Delta)`，增量经状态转换应用并 clamp 到合法区间；面板收起时尺寸记录保留。面板对齐切换不是命令：视图菜单对齐项发布 `SetPanelAlignmentEvent`，构造函数订阅（:36）调私有 `SetPanelAlignment`（:274）写入 `PanelAlignment` 镜像属性 |

### 公开方法

| 方法 | 签名/位置 | 调用方 |
|---|---|---|
| `EnsureContributionsLoaded` | `public void`（:126-152） | 仅 `MainWindow.axaml.cs:13-17` 的 `OnOpened`；`_contributionsLoaded` 守卫保证只执行一次。方法体内依次调用 `ShellContributionCollector`（`_collector`）的五个收集方法：**`GetNavigationItems(NavigationItemPlacement.Top)` 与 `(…Bottom)`** 经 `LoadItems` 填充 `TopNavigationItems`/`BottomNavigationItems` 与 `_itemsById`；**`GetMainViews()`** 把每个 `IMainViewContribution` 按 `Id` 存入 `_mainViewsById`（:136-139）；**`GetPanelTabs(PanelPlacement.Auxiliary)` 与 `(…Bottom)`** 经 `LoadPanelTabs` 填充两个 tab 集合与索引字典并写入 `State`；**`GetMenuItems(MenuPlacement.File/View/Help)`** 经 `LoadChrome` 填充三个菜单集合（`ObservableCollection<object>`），视图菜单收集后在第一个 `PanelAlignmentContribution` 项前插入 `Separator`（:148-156）；**`GetStatusBarItems()`** 逐项包装为 `StatusBarItemViewModel` 加入 `StatusBarItems` |

### 私有方法（改行为时直接面对）

`TogglePanel(TogglePanelTarget)`（:293-301，switch：SideBar→`State.ToggleSideBar()`、AuxiliaryPanel→`State.ToggleAuxiliaryPanel()`、默认 `_`→`State.ToggleBottomPanel()`）、`SetPanelAlignment(PanelAlignment)`（:274，事件处理，`PanelAlignment = alignment`）、`OpenMainView(string viewId)`（:192-208，未知 Id 静默返回）、`LoadPanelTabs`（:315-346：`contributions.Count == 0` 直接返回；逐贡献建 `PanelTabViewModel`、登记 `index` 字典并加入目标集合；取全部 tab Id 数组与 `tabs.FirstOrDefault()`（首个 tab）作默认 `ActiveTab`，用 `State with { … }` 按 `contributions[0].Panel` 选择更新 `AuxiliaryPanel` 或 `BottomPanel` 的 `Tabs`/`ActiveTab`；末尾调 `SyncActiveTab` 同步 `IsActive` 与首 tab 内容）、`SyncActiveTab`（:351-372：遍历 tab 集合设 `IsActive = (tab.Id == activeTab)`；`activeTab` 为 null 或索引查不到时返回；缓存未命中 `_containerProvider.Resolve(contribution.ContentViewType)` 写入内容缓存；经 `setContent` 回调赋给 `AuxiliaryContent`/`BottomContent`）、`LoadChrome`（:303，静态，目标集合类型 `ObservableCollection<object>`）、`LoadItems`（:374）

## 3. 呈现模型（包装贡献元数据，构造时解析图标几何）

四个类结构同构：构造接收对应贡献接口，`Icon = StreamGeometry.Parse(contribution.IconPath)`，透传 `Id`/`Title`。

| 类（文件） | 基类 | 包装 | 额外成员 |
|---|---|---|---|
| `NavigationItemViewModel`（NavigationItemViewModel.cs:10） | `ObservableObject` | `INavigationItemContribution`（`Contribution` 属性公开） | `[ObservableProperty] bool IsSelected`（:30） |
| `MenuItemViewModel`（MenuItemViewModel.cs:10） | 无（普通类） | `IMenuItemContribution` | `ICommand Command => Contribution.Command`（:29） |
| `PanelTabViewModel`（PanelTabViewModel.cs:10） | `ObservableObject` | `IPanelTabContribution`（`Contribution` 属性公开） | `[ObservableProperty] bool IsActive`（:30） |
| `StatusBarItemViewModel`（StatusBarItemViewModel.cs:9） | 无 | `IStatusBarItemContribution` | — |

## 4. `PanelResizer`（已迁入 Core/Framework）

本模块的 `PanelResizer.cs` 已删除；`PanelResizer` 现位于 `Core/Framework/Shell/PanelResizer.cs`，并改造为声明式调用：方向换算（SideBar 取 `+e.Vector.X`、AuxiliaryPanel 取 `-e.Vector.X`、BottomPanel 取 `-e.Vector.Y`）内聚进其 `OnDragDelta`，经 `Target`（`PanelResizeTarget`）与 `ResizeCommand`（`ICommand`）属性把增量包装为 `PanelResize` 发给 VM 的 `ResizePanelCommand`；`GetParentGrid() => null` 禁用原生重排的机制保持不变。XAML 用法迁至 `Core/Framework/Shell/FrameworkWindowTheme.axaml` 的各布局模板（声明 `Target`、`ResizeCommand="{Binding ResizePanelCommand}"`、`ResizeDirection`、8px 热区、`ZIndex="1"`）。细节见 `docs/analysis/Core/Framework/` 文档。

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
| `PanelAlignmentContribution`（PanelAlignmentContribution.cs:13） | `IMenuItemContribution` | `shell.align-{Alignment.ToString().ToLowerInvariant()}`（:27） | switch（:29-35） | switch（:37-43） | switch（:45-51：Left 40 / Right 50 / Center 60 / Justify 默认 70） | `MenuPlacement.View`（:53） | 构造注入 `IEventAggregator` + `PanelAlignment`，`Command = new DelegateCommand(() => eventAggregator.GetEvent<SetPanelAlignmentEvent>().Publish(alignment))`（:18-19） |
| `ReadyStatusBarItem`（ReadyStatusBarItem.cs:10） | `IStatusBarItemContribution` | `shell.status.ready` | `Language.StatusReadyTitle` | `Icons.Ready` | 10 | — | — |

### `TogglePanelContribution` 三个 switch 的精确映射（消费方关键事实）

构造：`TogglePanelContribution(IEventAggregator eventAggregator, TogglePanelTarget target)`，`Target` 属性只读公开。

| Target | Id | Title（:29-34） | IconPath（:36-41） | Order（:43-48） |
|---|---|---|---|---|
| `SideBar` | `shell.toggle-sidebar` | `Language.ToggleSideBarTitle` | `Icons.PanelLeft` | **10** |
| `BottomPanel` | `shell.toggle-bottompanel` | `Language.ToggleBottomPanelTitle`（默认分支） | `Icons.PanelBottom`（默认分支） | **20**（显式 case） |
| `AuxiliaryPanel` | `shell.toggle-auxiliarypanel` | `Language.ToggleAuxiliaryPanelTitle` | `Icons.PanelRight` | **30**（默认分支） |

| Alignment（`PanelAlignmentContribution`） | Id | Title | IconPath | Order |
|---|---|---|---|---|
| `Left` | `shell.align-left` | `Language.PanelAlignLeftTitle` | `Icons.AlignLeft` | **40** |
| `Right` | `shell.align-right` | `Language.PanelAlignRightTitle` | `Icons.AlignRight` | **50** |
| `Center` | `shell.align-center` | `Language.PanelAlignCenterTitle`（默认分支） | `Icons.AlignCenter`（默认分支） | **60**（显式 case） |
| `Justify` | `shell.align-justify` | `Language.PanelAlignJustifyTitle` | `Icons.AlignJustify` | **70**（默认分支） |

`PanelAlignmentContribution` 与 `TogglePanelContribution` 同构：有状态（每实例固定一个档位），`WorkstationApplication.cs:43-47` 按 `Enum.GetValues<PanelAlignment>()` 注册四个实例；Title/IconPath 的默认分支是 Center、Order 默认分支是 70（Justify）——`PanelAlignment` 新增成员时静默映射为 Center 文案/图标、Order 70（详见 pitfalls.md）。视图菜单中四个对齐项与三个显隐项之间由 `MainWindowViewModel.EnsureContributionsLoaded` 在收集后插入 `Separator`（定位第一个对齐项，:148-156）。

**注意 switch 结构不对称**：Title/IconPath 中 `SideBar`、`AuxiliaryPanel` 是显式 case，`BottomPanel` 走 `_` 默认分支；Order 中 `SideBar`(10)、`BottomPanel`(20) 是显式 case，`AuxiliaryPanel`(30) 走 `_` 默认分支。**`TogglePanelTarget` 新增成员时，Title/IconPath 静默映射为 BottomPanel 的文案与图标、Order 静默为 30，不报错**（详见 pitfalls.md）。注册方在 `WorkstationApplication.cs:37-41` 显式枚举三个 target 各注册一个实例。

## 6. 内置视图（Views/，均为无逻辑占位）

`SettingsView`/`PropertiesView`/`OutlineView`/`OutputView`/`LogView` 均为 `UserControl`，axaml 里只有一行"XX（占位）" `TextBlock`，code-behind 仅 `InitializeComponent()`。`EmptyStateView`（EmptyStateView.axaml）是静态快捷键提示页（Ctrl+B/Ctrl+J/Ctrl+Alt+B 三个键帽 + 引导文案）。`AboutWindow`（AboutWindow.axaml）：`Window`，360×160、`CanResize="False"`、`WindowStartupLocation="CenterOwner"`，标题与正文为**硬编码中文**（未走 `Language`，见 pitfalls.md）。

## 典型调用序列

模块向 shell 贡献东西**不需要引用本模块**：在模块的 `RegisterTypes` 里 `containerRegistry.RegisterSingleton<INavigationItemContribution, MyItem>()` 等，本模块的 `EnsureContributionsLoaded` 收集并渲染。打开主视图：`IEventAggregator.GetEvent<OpenMainViewEvent>().Publish("my.view.id")`（真实调用点：DashBoard 模块 `DashBoardNavigationView.axaml.cs:30、35`）。切换面板：`Publish(TogglePanelTarget.SideBar)` 到 `TogglePanelVisibilityEvent`（真实发布点：`Shell/TogglePanelContribution.cs:19`）。切换布局档位：`Publish(PanelAlignment.Justify)` 到 `SetPanelAlignmentEvent`（真实发布点：`Shell/PanelAlignmentContribution.cs:19`）。弹窗：注入 `IWindowManager` 调 `ShowDialog<AboutWindow>()`（真实调用点：`Shell/AboutMenuItem.cs:17`）。
