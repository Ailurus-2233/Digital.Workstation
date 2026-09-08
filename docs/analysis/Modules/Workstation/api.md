# Workstation — 对外接口与调用方式

命名空间四组：`DigitalWorkstation.Workstation`（根，宿主与主窗口）、`DigitalWorkstation.Workstation.Contributions`（预置贡献，`Contributions/`）、`DigitalWorkstation.Workstation.Menus`（attribute 菜单类，`Menus/`）、`DigitalWorkstation.Workstation.Views`（内置视图）。除注明外全部 public。

## 1. `WorkstationApplication`（WorkstationApplication.cs:12）

```csharp
public class WorkstationApplication : FrameworkApplication<MainWindow>
```

应用入口类，由 Launcher 启动（`App` 入口实例化它，见 reference.md 被依赖关系）。三个重写成员：

| 成员 | 签名/位置 | 行为 |
|---|---|---|
| `ConfigureModuleCatalog` | `protected override void`（:14-17） | 仅一行 `moduleCatalog.AddModule<DashBoardModule>()`——把 DashBoard 模块纳入逐模块加载清单 |
| `RegisterCustomService` | `protected override void RegisterCustomService(IContainerRegistry)`（:19-40） | 注册全部 shell 预置贡献与内置视图（清单见下） |
| `CreateSplashWindow` | `protected override Window CreateSplashWindow()`（:45-48） | `Container.Resolve<DashBoardWindow>()`，启动台窗口（ADR-0004） |

### RegisterCustomService 注册清单（:20-39，逐行）

| 注册 | 类型 | 说明 |
|---|---|---|
| `RegisterSingleton<INavigationItemContribution, SettingsNavigationItem>()` | 贡献 | "设置"导航项（ActivityBar 底部） |
| `Register<EmptyStateView>()` | 视图（瞬态） | MainContent 空状态页，MainWindowViewModel 构造时解析 |
| `RegisterSingleton<IPanelTabContribution, PropertiesPanelTab>()` / `OutlinePanelTab` / `OutputPanelTab` / `LogPanelTab` | 贡献 ×4 | AuxiliaryPanel"属性/大纲"、BottomPanel"输出/日志"演示 tab |
| `Register<PropertiesView>()` / `OutlineView` / `OutputView` / `LogView` | 视图（瞬态） | 四个 tab 的内容视图 |
| `containerRegistry.RegisterMenus(typeof(WorkstationApplication).Assembly)`（:35） | 菜单（attribute 扫描，ADR-0001） | Framework `MenuRegistration.RegisterMenus` 扩展扫描本程序集的 `[MenuGroup]` 类：`FileMenus`（文件>退出，Application 组 GroupOrder 1000）、`ViewPanelMenus`（视图>Panels 组三个显隐切换）、`ViewAlignmentMenus`（视图>Alignment 组四档对齐）、`HelpMenus`（帮助>关于）；每个 `[MenuItem]` 方法注册一个 `IMenuItemContribution` 工厂，菜单类本身 RegisterSingleton |
| `RegisterSingleton<IStatusBarItemContribution, ReadyStatusBarItem>()` | 贡献 | 状态栏"就绪" |
| `Register<AboutWindow>()` | 窗口（瞬态） | "关于"对话框，经 `IWindowManager` 按需解析 |

## 2. `MainWindowViewModel`（MainWindowViewModel.cs:16）

```csharp
public partial class MainWindowViewModel : ObservableObject
```

构造注入：`MainWindowViewModel(ShellContributionCollector collector, IContainerProvider containerProvider, IEventAggregator eventAggregator)`（:30-39）。由 Prism ViewModelLocator 自动装配（`MainWindow.axaml:12` `prism:ViewModelLocator.AutoWireViewModel="True"`）。构造函数订阅 `OpenMainViewEvent`→`OpenMainView`、`TogglePanelVisibilityEvent`→`TogglePanel`、`SetPanelAlignmentEvent`→`SetPanelAlignment`（:35-37），并把 `_mainContent` 初始化为 `containerProvider.Resolve<EmptyStateView>()`（:38）。

### 可绑定属性（XAML 消费面，绑定点见 MainWindow.axaml 与 Framework 的 `FrameworkWindowTheme.axaml`——后者是宽松反射绑定，Framework 不引用本类型）

| 属性 | 类型 | 语义 |
|---|---|---|
| `State` | `ShellLayoutState`（[ObservableProperty]，:43，初值 `ShellLayoutState.Initial`；带 `[NotifyPropertyChangedFor(nameof(SideBarColumnWidth), nameof(AuxiliaryColumnWidth))]`，:41-42） | 整个布局状态：面板显隐/宽高、选中导航项、活动 tab、活动主视图。**面板对齐档位不在其中**（见下行） |
| `PanelAlignment` | `PanelAlignment`（[ObservableProperty]，:50，初值 `PanelAlignment.Center`） | 面板对齐档位（左/右/居中/两端）：与 `FrameworkWindow.PanelAlignment` 双向绑定的镜像属性——窗口依赖属性是布局定义的唯一入口（:45-48 注释）；由 `SetPanelAlignmentEvent` 订阅写入 |
| `SideBarContent` / `SideBarTitle` | `object?` / `string?`（:53、:61） | SideBar 当前内容与区域标题 |
| `MainContent` | `object`（:58） | 主区当前内容；初始为 `EmptyStateView`，被 `OpenMainView` 整体替换 |
| `AuxiliaryContent` / `BottomContent` | `object?`（:85、:91） | 两个面板当前活动 tab 的内容 |
| `TopNavigationItems` / `BottomNavigationItems` | `ObservableCollection<NavigationItemViewModel>`（:63、:65） | ActivityBar 顶部/底部导航项 |
| `AuxiliaryTabs` / `BottomTabs` | `ObservableCollection<PanelTabViewModel>`（:66、:68） | 两个面板的 tab 栏 |
| `MenuBarItems` | `ObservableCollection<MenuItemViewModel>`（:73；`MenuItemViewModel` 类型在 Framework：`Core/Framework/Menus/MenuItemViewModel.cs`，经 `using DigitalWorkstation.Core.Framework.Menus` 解析） | 菜单栏（ADR-0001）：全部菜单贡献经 `MenuTreeBuilder.Build` 建树生成，顶层菜单与子菜单节点同为 `MenuItemViewModel`，分隔线以 Avalonia `Separator` 控件形式存在于子级 `Children`；渲染侧是 `FrameworkWindow` 内置的标题栏菜单栏（`Core/Framework/Windows/FrameworkWindow.cs:35-41` 宽松绑定 `MenuBarItems`，chrome-menu 样式在 `Core/Framework/Windows/FrameworkWindowTheme.axaml:551-569`） |
| `StatusBarItems` | `ObservableCollection<StatusBarItemViewModel>`（:79） | 状态栏条目 |
| `CollapseBottomIcon` / `CollapseAuxiliaryIcon` | `Geometry`（:95 `Icons.ChevronDown`、:100 `Icons.ChevronRight`） | 两个面板收起按钮图标 |
| `SideBarColumnWidth` / `AuxiliaryColumnWidth` | `GridLength`（:106-107、:112-113） | 可见时面板宽度 + 4px 间隙（布局模板间隙约定），隐藏归零使 BottomPanel 跨度自然伸缩；供 Framework 布局模板的列宽绑定 |

### 命令（[RelayCommand] 生成，XAML 绑定名 = 方法名 + Command）

| `SelectActivityCommand` | `SelectActivity(NavigationItemViewModel)`（:151） | 选中导航项并驱动 SideBar 展开/收起；遍历 `_itemsById.Values` 把每项 `IsSelected` 设为 `Id == State.SelectedActivity`；随后若 `State.SideBar.Visible` 为 false 或 `State.SideBar.ContentFor` 为空则**提前 return**（:160-163，标题与内容保持不变）；否则按 `ContentFor` 的 Id 取/建缓存内容，`SideBarTitle` 取 `_itemsById[contentId].Title`（:171）、`SideBarContent` 设为该内容实例 |
| `ActivateAuxTabCommand` | `ActivateAuxTab(PanelTabViewModel)`（:198） | 激活 AuxiliaryPanel tab：先 `State.ActivateAuxTab(tab.Id)` 得 `next`；面板收起时 `ShellLayoutState` 拒绝并原样返回原实例，`ReferenceEquals(next, State)`（:201）为 true → 直接 return，不赋 `State`、不调 `SyncActiveTab`；状态变化时 `State = next` 并调 `SyncActiveTab` 同步各 tab 的 `IsActive` 与缓存的内容视图 |
| `ActivateBottomTabCommand` | `ActivateBottomTab(PanelTabViewModel)`（:215） | 同上，针对 BottomPanel（`State.ActivateBottomTab(tab.Id)`，:217） |
| `ToggleSideBarCommand` | `ToggleSideBar()`（:232） | Ctrl+B；转发 `TogglePanel(TogglePanelTarget.SideBar)` |
| `ToggleAuxiliaryPanelCommand` | `ToggleAuxiliaryPanel()`（:241） | Ctrl+Alt+B 或收起按钮 |
| `ToggleBottomPanelCommand` | `ToggleBottomPanel()`（:250） | Ctrl+J 或收起按钮 |
| `ResizePanelCommand` | `ResizePanel(PanelResize)`（:270） | 分隔条拖拽的唯一路径：Framework 的 `PanelResizer` 经 `Target` + `ResizeCommand` 声明式调用（code-behind 不再参与拖拽）；`State = State.Resize(resize.Target, resize.Delta)`，增量经状态转换应用并 clamp 到合法区间；面板收起时尺寸记录保留。面板对齐切换不是命令：视图菜单对齐项发布 `SetPanelAlignmentEvent`，构造函数订阅（:37）调私有 `SetPanelAlignment`（:259）写入 `PanelAlignment` 镜像属性 |

### 公开方法

| 方法 | 签名/位置 | 调用方 |
|---|---|---|
| `EnsureContributionsLoaded` | `public void`（:119-145） | 仅 `MainWindow.axaml.cs:13-17` 的 `OnOpened`；`_contributionsLoaded` 守卫保证只执行一次。方法体内依次调用 `ShellContributionCollector`（`_collector`）的五个收集方法：**`GetNavigationItems(NavigationItemPlacement.Top)` 与 `(…Bottom)`** 经 `LoadItems` 填充 `TopNavigationItems`/`BottomNavigationItems` 与 `_itemsById`；**`GetMainViews()`** 把每个 `IMainViewContribution` 按 `Id` 存入 `_mainViewsById`（:129-132）；**`GetPanelTabs(PanelPlacement.Auxiliary)` 与 `(…Bottom)`** 经 `LoadPanelTabs` 填充两个 tab 集合与索引字典并写入 `State`；**`GetMenuItems()`**（无参，不过滤不排序，ADR-0001）经 Framework 的 `MenuTreeBuilder.Build` 建树（顶层排序不分组、子菜单分组排序且组间自动插分隔线），逐顶层节点 Framework 的 `MenuItemViewModel.FromSubmenu` 递归转换加入 `MenuBarItems`（:137-140）；**`GetStatusBarItems()`** 逐项包装为 `StatusBarItemViewModel` 加入 `StatusBarItems` |

### 私有方法（改行为时直接面对）

`TogglePanel(TogglePanelTarget)`（:278-286，switch：SideBar→`State.ToggleSideBar()`、AuxiliaryPanel→`State.ToggleAuxiliaryPanel()`、默认 `_`→`State.ToggleBottomPanel()`）、`SetPanelAlignment(PanelAlignment)`（:259，事件处理，`PanelAlignment = alignment`）、`OpenMainView(string viewId)`（:177-193，未知 Id 静默返回）、`LoadPanelTabs`（:291-322：`contributions.Count == 0` 直接返回；逐贡献建 `PanelTabViewModel`、登记 `index` 字典并加入目标集合；取全部 tab Id 数组与 `tabs.FirstOrDefault()`（首个 tab）作默认 `ActiveTab`，用 `State with { … }` 按 `contributions[0].Panel` 选择更新 `AuxiliaryPanel` 或 `BottomPanel` 的 `Tabs`/`ActiveTab`；末尾调 `SyncActiveTab` 同步 `IsActive` 与首 tab 内容）、`SyncActiveTab`（:327-348：遍历 tab 集合设 `IsActive = (tab.Id == activeTab)`；`activeTab` 为 null 或索引查不到时返回；缓存未命中 `_containerProvider.Resolve(contribution.ContentViewType)` 写入内容缓存；经 `setContent` 回调赋给 `AuxiliaryContent`/`BottomContent`）、`LoadItems`（:350-359）

## 3. 呈现模型（包装贡献元数据，构造时解析图标几何）

三个类结构同构：构造接收对应贡献接口，`Icon = StreamGeometry.Parse(contribution.IconPath)`，透传 `Id`/`Title`。（菜单呈现模型 `MenuItemViewModel` 已迁入 Framework——`Core/Framework/Menus/MenuItemViewModel.cs`：不包装贡献，由菜单树 `MenuTreeSubmenu` 经静态 `FromSubmenu` 递归转换，`MenuTreeSeparator` 转 Avalonia `Separator`；详见 `docs/analysis/Core/Framework/` 文档。）

| 类（文件） | 基类 | 包装 | 额外成员 |
|---|---|---|---|
| `NavigationItemViewModel`（NavigationItemViewModel.cs:10） | `ObservableObject` | `INavigationItemContribution`（`Contribution` 属性公开） | `[ObservableProperty] bool IsSelected`（:30） |
| `PanelTabViewModel`（PanelTabViewModel.cs:10） | `ObservableObject` | `IPanelTabContribution`（`Contribution` 属性公开） | `[ObservableProperty] bool IsActive`（:30） |
| `StatusBarItemViewModel`（StatusBarItemViewModel.cs:9） | 无 | `IStatusBarItemContribution` | — |

## 4. `PanelResizer`（已迁入 Core/Framework）

本模块的 `PanelResizer.cs` 已删除；`PanelResizer` 现位于 `Core/Framework/Layout/PanelResizer.cs`，并改造为声明式调用：方向换算（SideBar 取 `+e.Vector.X`、AuxiliaryPanel 取 `-e.Vector.X`、BottomPanel 取 `-e.Vector.Y`）内聚进其 `OnDragDelta`，经 `Target`（`PanelResizeTarget`）与 `ResizeCommand`（`ICommand`）属性把增量包装为 `PanelResize` 发给 VM 的 `ResizePanelCommand`；`GetParentGrid() => null` 禁用原生重排的机制保持不变。XAML 用法迁至 `Core/Framework/Windows/FrameworkWindowTheme.axaml` 的各布局模板（声明 `Target`、`ResizeCommand="{Binding ResizePanelCommand}"`、`ResizeDirection`、8px 热区、`ZIndex="1"`）。细节见 `docs/analysis/Core/Framework/` 文档。

## 5. Shell 预置贡献（`Contributions/` 六个 `I*Contribution` 实现类 + `Menus/` 四个 attribute 菜单类）

属性矩阵（Id/Title 键/IconPath 常量/Order/定位/行为；菜单类的 Title 是 attribute 里的 Language 资源键字符串，运行时由 Framework 经 `Language.Get` 解析）：

| 类（文件） | 接口 | Id | Title | IconPath | Order | 定位 | 行为字段 |
|---|---|---|---|---|---|---|---|
| `SettingsNavigationItem`（SettingsNavigationItem.cs:11） | `INavigationItemContribution` | `shell.settings` | `Language.SettingsNavigationTitle` | `Icons.Settings` | 0 | `NavigationItemPlacement.Bottom` | `ContentViewType = typeof(SettingsView)` |
| `PropertiesPanelTab`（PropertiesPanelTab.cs:11） | `IPanelTabContribution` | `shell.properties` | `Language.PropertiesTabTitle` | `Icons.Properties` | 10 | `PanelPlacement.Auxiliary` | `ContentViewType = typeof(PropertiesView)` |
| `OutlinePanelTab`（OutlinePanelTab.cs:11） | `IPanelTabContribution` | `shell.outline` | `Language.OutlineTabTitle` | `Icons.Outline` | 20 | `PanelPlacement.Auxiliary` | `ContentViewType = typeof(OutlineView)` |
| `OutputPanelTab`（OutputPanelTab.cs:11） | `IPanelTabContribution` | `shell.output` | `Language.OutputTabTitle` | `Icons.Output` | 10 | `PanelPlacement.Bottom` | `ContentViewType = typeof(OutputView)` |
| `LogPanelTab`（LogPanelTab.cs:11） | `IPanelTabContribution` | `shell.log` | `Language.LogTabTitle` | `Icons.Log` | 20 | `PanelPlacement.Bottom` | `ContentViewType = typeof(LogView)` |
| `FileMenus`（FileMenus.cs:13） | attribute 菜单类（ADR-0001） | — | `[MenuGroup("MenuFileTitle", Group = "Application", GroupOrder = 1000, Order = 100)]`（:12） | — | `Exit` 项 Order 100 | 顶层"文件"菜单，Application 组（GroupOrder 1000 保持在末尾） | `Exit()`（:19-22，`[MenuItem("MenuExitTitle", Order = 100, Icon = Icons.Exit)]`，:18）：`(Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown()` |
| `HelpMenus`（HelpMenus.cs:12） | attribute 菜单类 | — | `[MenuGroup("MenuHelpTitle", Order = 300)]`（:11） | — | `About` 项 Order 100 | 顶层"帮助"菜单（单段路径，Order 300 是顶层位次；方法项进默认组） | 构造注入 `IWindowManager`；`About()`（:18-21，`[MenuItem("MenuAboutTitle", Order = 100, Icon = Icons.About)]`，:17）：`windowManager.ShowDialog<AboutWindow>()`（:20） |
| `ViewPanelMenus`（ViewPanelMenus.cs:12） | attribute 菜单类 | — | `[MenuGroup("MenuViewTitle", Group = "Panels", GroupOrder = 100, Order = 200)]`（:11） | — | 三项 Order 100/200/300 | 顶层"视图"菜单 Panels 组 | 构造注入 `IEventAggregator`；`ToggleSideBar`/`ToggleBottomPanel`/`ToggleAuxiliaryPanel`（:15/:21/:27）各发布 `TogglePanelVisibilityEvent` 对应 `TogglePanelTarget` |
| `ViewAlignmentMenus`（ViewAlignmentMenus.cs:13） | attribute 菜单类 | — | `[MenuGroup("MenuViewTitle", Group = "Alignment", GroupOrder = 200)]`（:12） | — | 四项 Order 100/200/300/400 | 顶层"视图"菜单 Alignment 组（与 Panels 组之间由建树器插分隔线） | 构造注入 `IEventAggregator`；`AlignLeft`/`AlignRight`/`AlignCenter`/`AlignJustify`（:16/:22/:28/:34）各发布 `SetPanelAlignmentEvent` 对应 `PanelAlignment` |
| `ReadyStatusBarItem`（ReadyStatusBarItem.cs:10） | `IStatusBarItemContribution` | `shell.status.ready` | `Language.StatusReadyTitle` | `Icons.Ready` | 10 | — | — |

### 菜单类的 `[MenuItem]` 精确映射（消费方关键事实）

`ViewPanelMenus`（菜单类本身单例注册，方法即命令；标题/图标注在 attribute 上）：

| 方法（位置） | `[MenuItem]` 标题键 | Icon | Order | 发布 |
|---|---|---|---|---|
| `ToggleSideBar`（:14-18） | `"ToggleSideBarTitle"` | `Icons.PanelLeft` | **100** | `TogglePanelTarget.SideBar` |
| `ToggleBottomPanel`（:20-24） | `"ToggleBottomPanelTitle"` | `Icons.PanelBottom` | **200** | `TogglePanelTarget.BottomPanel` |
| `ToggleAuxiliaryPanel`（:26-30） | `"ToggleAuxiliaryPanelTitle"` | `Icons.PanelRight` | **300** | `TogglePanelTarget.AuxiliaryPanel` |

`ViewAlignmentMenus`：

| 方法（位置） | `[MenuItem]` 标题键 | Icon | Order | 发布 |
|---|---|---|---|---|
| `AlignLeft`（:15-19） | `"PanelAlignLeftTitle"` | `Icons.AlignLeft` | **100** | `PanelAlignment.Left` |
| `AlignRight`（:21-25） | `"PanelAlignRightTitle"` | `Icons.AlignRight` | **200** | `PanelAlignment.Right` |
| `AlignCenter`（:27-31） | `"PanelAlignCenterTitle"` | `Icons.AlignCenter` | **300** | `PanelAlignment.Center` |
| `AlignJustify`（:33-37） | `"PanelAlignJustifyTitle"` | `Icons.AlignJustify` | **400** | `PanelAlignment.Justify` |

注册方是 `WorkstationApplication.cs:35` 的一行 `RegisterMenus(typeof(WorkstationApplication).Assembly)`——Framework 的 `MenuRegistration` 扫描程序集内全部 `[MenuGroup]` 类，菜单类 RegisterSingleton、每个合法 `[MenuItem]` 方法注册一个 `IMenuItemContribution` 工厂（非法签名与空段路径记日志跳过，详见 Framework 文档）。`PanelAlignment`/`TogglePanelTarget` 新增枚举成员时需要在此手工加对应方法（不像旧工厂循环那样自动覆盖，见 pitfalls.md）。

## 6. 内置视图（Views/，均为无逻辑占位）

`SettingsView`/`PropertiesView`/`OutlineView`/`OutputView`/`LogView` 均为 `UserControl`，axaml 里只有一行"XX（占位）" `TextBlock`，code-behind 仅 `InitializeComponent()`。`EmptyStateView`（EmptyStateView.axaml）是静态快捷键提示页（Ctrl+B/Ctrl+J/Ctrl+Alt+B 三个键帽 + 引导文案）。`AboutWindow`（AboutWindow.axaml）：`Window`，360×160、`CanResize="False"`、`WindowStartupLocation="CenterOwner"`，标题与正文为**硬编码中文**（未走 `Language`，见 pitfalls.md）。

## 典型调用序列

模块向 shell 贡献东西**不需要引用本模块**：导航项等在模块的 `RegisterTypes` 里 `containerRegistry.RegisterSingleton<INavigationItemContribution, MyItem>()`，菜单则新建 `[MenuGroup]` 类 + `[MenuItem]` 方法后调 `containerRegistry.RegisterMenus(模块程序集)`（ADR-0001），本模块的 `EnsureContributionsLoaded` 收集并渲染。打开主视图：`IEventAggregator.GetEvent<OpenMainViewEvent>().Publish("my.view.id")`（真实调用点：DashBoard 模块 `DashBoardNavigationView.axaml.cs:30、35`）。切换面板：`Publish(TogglePanelTarget.SideBar)` 到 `TogglePanelVisibilityEvent`（真实发布点：`Menus/ViewPanelMenus.cs:17`）。切换布局档位：`Publish(PanelAlignment.Justify)` 到 `SetPanelAlignmentEvent`（真实发布点：`Menus/ViewAlignmentMenus.cs:36`）。弹窗：注入 `IWindowManager` 调 `ShowDialog<AboutWindow>()`（真实调用点：`Menus/HelpMenus.cs:20`）。
