# Workstation — 模块关系链

## 依赖关系

### 项目引用（Workstation.csproj:9-15）

| 依赖 | 用到的能力 | 本模块使用点 |
|---|---|---|
| `Core/Framework` | `FrameworkApplication<TWindow>` 应用入口基类（Framework/FrameworkApplication.cs）；`FrameworkWindow` 与主题（Framework/Windows/）；`CommandPalette` 命令面板与 `CommandRegistration.RegisterCommands` 命令扫描注册（Framework/Windows/、Framework/Commands/，ADR-0005）；`PanelResizer`、`ShellLayoutState` 及区域 record、`PanelAlignment`/`PanelResize`/`PanelResizeTarget`、`SetPanelAlignmentEvent`、`LayoutPersistence` 布局落盘与 `ShellLayoutDto` 一族 DTO（Framework/Layout/）；`ShellContributionCollector` 贡献收集器与 `ToolViewRegistration.RegisterToolViews` 工具视图扫描注册（Framework/Contributions/，ADR-0002）；`MenuTreeBuilder`/`MenuRegistration`/`MenuItemViewModel` 菜单建树、扫描注册与呈现模型（Framework/Menus/，ADR-0001） | `WorkstationApplication.cs:15` 继承、`:27` `RegisterToolViews`、`:31` `RegisterMenus`、`:33` `RegisterCommands`；`MainWindowViewModel.cs:21` 注入 collector、`:24` 注入 `LayoutPersistence`、`:60` 持有 `ShellLayoutState _state`、`:184` `MenuTreeBuilder.Build`、`:438` `ResizePanel` 消费 `PanelResizeTarget`、`:536-590` `CaptureLayout`/`ScheduleSave` 产 `ShellLayoutDto`（using 见 `MainWindowViewModel.cs:6-12`：Abstractions.Commands/Abstractions.Contributions/Abstractions.Regions/Framework.Contributions/Layout/Menus/Models.Events）；`MainWindow.axaml.cs:1` `Framework.Windows`、`:22` `RegisterCommandGestures`（手势 KeyBinding 接线） |
| `Core/Resource` | `Language` 本地化字符串（Resource/Language.cs） | `ReadyStatusBarItem.Title` 直接调 `Language.StatusReadyTitle`（`ReadyStatusBarItem.cs:14`）；`MainWindowViewModel.SettingsTitle` 直接调 `Language.SettingsNavigationTitle`（`MainWindowViewModel.cs:151`，ActivityBar"设置"导航按钮标题，ADR-0006 决策 6——不再是 `[ToolView]` 资源键）；五个 attribute 菜单类不调用 `Language.*`，而是在 attribute 里写资源键字符串 |
| `Core/UIPackage` | `Icons` 图标路径常量（UIPackage/Icons.cs）；Ursa/Semi 主题资源键（`SemiColor*`、`Chrome*`，运行期由 Framework 装载主题后可用） | `ReadyStatusBarItem.IconPath` 与菜单类 `[MenuItem(Icon = …)]`；`MainWindowViewModel.cs:136、:141` 收起按钮图标、`:146` `SettingsIcon`（`Icons.Settings`，ActivityBar"设置"导航按钮图标，ADR-0006 决策 6）；`MainWindow.axaml` 全部 `{DynamicResource ...}` |
| `Modules/DashBoard` | `DashBoardModule`（Prism 模块）、`DashBoardWindow`（启动台窗口） | `WorkstationApplication.cs:19` `AddModule<DashBoardModule>()`、`:43-46` `CreateSplashWindow()` 返回 `Container.Resolve<DashBoardWindow>()` |
| `Modules/Settings` | `SettingsModule`（Prism 模块，向 MainContent 贡献设置页主视图占位骨架，ADR-0006） | `WorkstationApplication.cs:20` `AddModule<SettingsModule>()`（using `DigitalWorkstation.Settings`，:9） |

### 传递依赖（未在 csproj 直接引用，源码 using 其命名空间）

| 传递来源 | 用到的能力 | 使用点 |
|---|---|---|
| `Core/Abstractions`（经 Framework） | 工具视图契约：`ToolViewAttribute`/`ToolViewContribution`/`ToolViewPlacement`（Abstractions/Contributions/，ADR-0002；旧 `INavigationItemContribution`/`IPanelTabContribution` 与定位枚举已删除）；`IMainViewContribution`/`IStatusBarItemContribution`（Abstractions/Contributions/）；`IMenuItemContribution`、`MenuGroupAttribute`/`MenuItemAttribute`（Abstractions/Menus/）；`ICommandContribution`、`CommandAttribute`（Abstractions/Commands/，ADR-0005）；`WellKnownViews` 主视图 Id 常量（Abstractions/Regions/，ADR-0006 决策 5）；`IWindowManager`（Abstractions/WindowManager/） | `Views/` 四个 View 类标注 `[ToolView]`；`ReadyStatusBarItem` 实现 `IStatusBarItemContribution`；`MainWindowViewModel.cs:200-236` `LoadToolViews` 按 `ToolViewPlacement`/`AllowMove` 与持久化配置分派收集结果、`:120` `Commands` 属性类型为 `ICommandContribution`、`:305` `OpenSettings` 以 `WellKnownViews.Settings` 发布（using 见 `:8`）；五个菜单类标注两个 attribute（`Abstractions.Menus`）；`Commands/ViewCommands.cs` 四个方法标注 `[Command]`（`Abstractions.Commands`）；`Menus/HelpMenus.cs:12` 注入 `IWindowManager` |
| `Core/Models`（经 Framework） | `OpenMainViewEvent`、`TogglePanelVisibilityEvent`、`TogglePanelTarget`、`ResetLayoutEvent`（Models/Events/；`SetPanelAlignmentEvent` 已迁往 `Framework.Layout`） | `MainWindowViewModel.cs:48-51` 订阅四事件（含 `OpenMainViewEvent`）、`:305` `OpenSettings` 发布 `OpenMainViewEvent`（ADR-0006 决策 6）、`:488-497` `TogglePanel` 消费枚举、`:503-521` `ResetLayout` 处理重置；`Menus/ViewPanelMenus.cs` 发布 `TogglePanelVisibilityEvent`、`Menus/ViewLayoutMenus.cs:16` 发布 `ResetLayoutEvent`（`WorkstationApplication.cs` 不再引用本命名空间） |
| Prism（经 Framework：`Prism.DryIoc.Avalonia`） | `IContainerRegistry`/`IContainerProvider`、`IEventAggregator`、ViewModelLocator（`DelegateCommand` 不再出现于本模块——菜单命令由 Framework 的反射贡献实现包装） | `WorkstationApplication.cs:23、27、31、33、35`；`MainWindowViewModel.cs:41-55`；`Menus/ViewPanelMenus.cs:12`、`ViewAlignmentMenus.cs:13`、`ViewLayoutMenus.cs:11`、`Commands/ViewCommands.cs:11` 构造注入 `IEventAggregator`；`MainWindow.axaml:12` `AutoWireViewModel` |
| `CommunityToolkit.Mvvm`（经 Framework） | `ObservableObject`、`[ObservableProperty]`、`[RelayCommand]` | `MainWindowViewModel.cs:19、:57-68、:290 等`；`NavigationItemViewModel.cs:10、29`；`PanelTabViewModel.cs:10、29` |
| Avalonia / Ursa（经 Framework/UIPackage） | `Window`/`UserControl`/`GridSplitter`/`StreamGeometry`/`ApplicationLifetime`/`Separator`；`UrsaWindow`（经 FrameworkWindow 间接继承） | 全部 View/code-behind；`Core/Framework/Layout/PanelResizer.cs:13`；`Menus/FileMenus.cs:21`（ApplicationLifetime Shutdown）；Framework 的 `Core/Framework/Menus/MenuItemViewModel.cs:43、47`（StreamGeometry 解析图标、Separator 分隔线）；`MainWindow.axaml:1`（根元素 `win:FrameworkWindow`） |

### 编译设置

`net10.0`、`ImplicitUsings`+`Nullable` enable（Workstation.csproj:4-6）。第 18-23 行两条 `Compile Update ... DependentUpon`（EmptyStateView/AboutWindow 的 code-behind 嵌套显示），仅 IDE 语义。程序集/根命名空间 `DigitalWorkstation.Workstation`（由 `Build/Base.props` 统一规则，源码命名空间与之一致）。

## 被依赖关系

| 依赖方 | 引用方式 | 用在什么场景 |
|---|---|---|
| `Launcher`（解决方案入口项目） | ProjectReference | 应用启动入口：实例化 `WorkstationApplication` 并运行（Launcher → Workstation → Framework 的传递链见 Models 深读文档）。Workstation 是整个解决方案的应用宿主，除此无其他项目引用它 |
| `Digital.Workstation.slnx` | 解决方案成员 | `/Modules/` 文件夹下三个项目之一（另两个 DashBoard、Settings） |

功能模块（DashBoard 及未来模块）**不引用**本模块程序集：它们只实现 Abstractions 的贡献契约，由本模块经 `ShellContributionCollector` 收集——这是刻意的单向依赖（模块不知道 shell 的存在，shell 不知道模块的类型，靠容器+接口+事件解耦）。

## 核心内部数据结构

### `MainWindowViewModel` 私有字段（MainWindowViewModel.cs:21-39）

```csharp
private readonly ShellContributionCollector _collector;         // :21 贡献收集器（Framework）
private readonly IContainerProvider _containerProvider;         // :22 视图实例解析源
private readonly IEventAggregator _eventAggregator;             // :23 事件聚合器（订阅四事件；OpenSettingsCommand 经它发布 OpenMainViewEvent，ADR-0006 决策 6）
private readonly LayoutPersistence _persistence;                // :24 布局落盘机制（Framework，捕获/恢复/重置在本类）
private readonly Dictionary<string, NavigationItemViewModel> _itemsById;     // :25 导航项 Id → VM（Top+Bottom 合并）
private readonly Dictionary<string, IMainViewContribution> _mainViewsById;   // :26 主视图 Id → 贡献
private readonly Dictionary<string, object> _mainViewContents;             // :27 主视图 Id → 已解析视图实例（缓存）
private readonly Dictionary<string, ToolViewContribution> _contributionsById; // :31 全部工具视图（含钉住项）Id → 元数据（归属随拖拽变，索引不变）
private readonly Dictionary<string, PanelTabViewModel> _tabsById;          // :32 面板 tab Id → VM（跨面板迁移复用）
private readonly Dictionary<string, object> _toolViewContents;             // :37 工具视图 Id → 内容实例（统一单实例缓存，ADR-0002；跨 Bar 迁移实例随 tab 走）
private bool _contributionsLoaded;                              // :38 EnsureContributionsLoaded 一次性守卫
private IReadOnlyList<ToolViewContribution> _toolViews;         // :39 工具视图贡献缓存（装载时一次拉出，ResetLayout 重建复用）
```

关系要点：`_toolViewContents` 与 `_mainViewContents` 两个缓存字典是视图实例的**唯一持有者**（除此之外只有 XAML `ContentControl` 的 Content 引用），缓存键 = 贡献的字符串 Id，与 `ShellLayoutState` 里的 `ContentFor`/`ActiveTab`/`ActiveView`/`ActivityBarItems` 对应。字典索引用赋值（`_mainViewsById[id] = contribution`），重复 Id **静默覆盖**（见 pitfalls.md）。`_toolViews` 只在 `EnsureContributionsLoaded` 赋值一次（:178），是 `LoadToolViews` 与 `ResetLayout` 重建的共同数据源。

### 贡献类（Contributions/）与菜单类（Menus/）——无字段、无状态，全部数据即 attribute 值/属性值

`ReadyStatusBarItem` 的属性矩阵见 api.md 第 5 节（五个 attribute 菜单类不实现贡献接口、无 Id——`IMenuItemContribution` 的 Id 已随 ADR-0001 删除）；当前本模块无 `[ToolView]` 标注类（原四个演示占位视图 `shell.properties`/`shell.outline`/`shell.output`/`shell.log` 已删除，机制见 ADR-0002——工具视图 Id 即 `[ToolView]` 主构造参数，默认归属仅决定首次/重置布局，实际归属是 `State.ActivityBarItems` / `AuxiliaryPanel.Tabs` / `BottomPanel.Tabs`）。跨类关系由字符串 Id 建立：

```
ReadyStatusBarItem.Id       "shell.status.ready"
```

### 与外部类型的对应关系

| 本模块类型 | 实现/消费的抽象（定义处） |
|---|---|
| `WorkstationApplication` | `FrameworkApplication<TWindow>`（Core/Framework/FrameworkApplication.cs:16），最终基类 `Prism.DryIoc.PrismApplication` |
| `ReadyStatusBarItem`（Contributions/）+ 五个 attribute 菜单类（Menus/） | `IStatusBarItemContribution`（Core/Abstractions/Contributions/）与 `[MenuGroup]`/`[MenuItem]`（Core/Abstractions/Menus/）；工具视图契约 `ToolViewAttribute`（ADR-0002）当前无本模块实现，详见 docs/analysis/Core/Abstractions/api.md |
| `MainWindowViewModel.State` | `ShellLayoutState` 一族 record（Core/Framework/Layout/），转换语义详见 docs/analysis/Core/Framework/api.md 第 3 节 |
| `MainWindowViewModel` 的事件订阅 | `OpenMainViewEvent`/`TogglePanelVisibilityEvent`/`ResetLayoutEvent`（Core/Models/Events/）与 `SetPanelAlignmentEvent`（Core/Framework/Layout/），负载分别为 `string`（= `IMainViewContribution.Id`）、`TogglePanelTarget`、无负载 与 `PanelAlignment`（Layout） |
| `ViewCommands`（Commands/） | `CommandAttribute`（Core/Abstractions/Commands/CommandAttribute.cs，ADR-0005）：四个 `[Command]` 方法经 Framework `RegisterCommands` 扫描生成 `ICommandContribution` |
| `HelpMenus.About` 的弹窗 | `IWindowManager`（Core/Abstractions/WindowManager/），实现为 Framework 的 `FrameworkWindowManager`（`ShowDialog` 要求主窗口已设且 IsActive） |
| `ReadyStatusBarItem` 的 `Title`/`IconPath`、菜单类 attribute | `Language`（Core/Resource/Language.cs）/`Icons`（Core/UIPackage/Icons.cs）；attribute 里是资源键字符串，由 Framework 经 `Language.Get` 解析 |
