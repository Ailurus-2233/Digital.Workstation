# Workstation — 模块关系链

## 依赖关系

### 项目引用（Workstation.csproj:9-14）

| 依赖 | 用到的能力 | 本模块使用点 |
|---|---|---|
| `Core/Framework` | `FrameworkApplication<TWindow>` 应用入口基类（Framework/FrameworkApplication.cs）；`FrameworkWindow` 与主题（Framework/Windows/）；`PanelResizer`、`ShellLayoutState` 及区域 record、`PanelAlignment`/`PanelResize`/`PanelResizeTarget`、`SetPanelAlignmentEvent`（Framework/Layout/）；`ShellContributionCollector` 贡献收集器与 `ToolViewRegistration.RegisterToolViews` 工具视图扫描注册（Framework/Contributions/，ADR-0002）；`MenuTreeBuilder`/`MenuRegistration`/`MenuItemViewModel` 菜单建树、扫描注册与呈现模型（Framework/Menus/，ADR-0001） | `WorkstationApplication.cs:13` 继承、`:24` `RegisterToolViews`、`:28` `RegisterMenus`；`MainWindowViewModel.cs:18` 注入 collector、`:43` 持有 `ShellLayoutState _state`、`:142` `MenuTreeBuilder.Build`、`:275` `ResizePanel` 消费 `PanelResizeTarget`（using 见 `MainWindowViewModel.cs:7-11`：Abstractions.Contributions/Framework.Contributions/Layout/Menus/Models.Events）；`MainWindow.axaml.cs:1` `Framework.Windows` |
| `Core/Resource` | `Language` 本地化字符串（Resource/Language.cs） | `ReadyStatusBarItem.Title` 直接调 `Language.StatusReadyTitle`（`ReadyStatusBarItem.cs:14`）；五个 `[ToolView]` View 类与四个 attribute 菜单类不调用 `Language.*`，而是在 attribute 里写资源键字符串（`"SettingsNavigationTitle"`/`"PropertiesTabTitle"` 等工具视图标题键；`"MenuFileTitle"`/`"MenuViewTitle"`/`"MenuHelpTitle"`/`"MenuExitTitle"`/`"MenuAboutTitle"`/`"ToggleSideBarTitle"` 等菜单键），运行时分别由 Framework 的 `ToolViewRegistration`（`ToolViewRegistration.cs:51`）与 `MenuRegistration`/`MenuTreeBuilder` 经 `Language.Get` 解析 |
| `Core/UIPackage` | `Icons` 图标路径常量（UIPackage/Icons.cs）；Ursa/Semi 主题资源键（`SemiColor*`、`Chrome*`，运行期由 Framework 装载主题后可用） | 五个 View 类的 `[ToolView(Icon = …)]`、`ReadyStatusBarItem.IconPath` 与菜单类 `[MenuItem(Icon = …)]`；`MainWindowViewModel.cs:95、100` 收起按钮图标；`MainWindow.axaml` 全部 `{DynamicResource ...}` |
| `Modules/DashBoard` | `DashBoardModule`（Prism 模块）、`DashBoardWindow`（启动台窗口） | `WorkstationApplication.cs:17` `AddModule<DashBoardModule>()`、`:38-41` `CreateSplashWindow()` 返回 `Container.Resolve<DashBoardWindow>()` |

### 传递依赖（未在 csproj 直接引用，源码 using 其命名空间）

| 传递来源 | 用到的能力 | 使用点 |
|---|---|---|
| `Core/Abstractions`（经 Framework） | 工具视图契约：`ToolViewAttribute`/`ToolViewContribution`/`ToolViewPlacement`（Abstractions/Contributions/，ADR-0002；旧 `INavigationItemContribution`/`IPanelTabContribution` 与定位枚举已删除）；`IMainViewContribution`/`IStatusBarItemContribution`（Abstractions/Contributions/）；`IMenuItemContribution`、`MenuGroupAttribute`/`MenuItemAttribute`（Abstractions/Menus/）；`IWindowManager`（Abstractions/WindowManager/） | `Views/` 五个 View 类标注 `[ToolView]`；`ReadyStatusBarItem` 实现 `IStatusBarItemContribution`；`MainWindowViewModel.cs:127-141` 按 `ToolViewPlacement`/`AllowMove` 分派收集结果；四个菜单类标注两个 attribute（`Abstractions.Menus`）；`Menus/HelpMenus.cs:12` 注入 `IWindowManager` |
| `Core/Models`（经 Framework） | `OpenMainViewEvent`、`TogglePanelVisibilityEvent`、`TogglePanelTarget`（Models/Events/；`SetPanelAlignmentEvent` 已迁往 `Framework.Layout`） | `MainWindowViewModel.cs:35-36` 订阅两事件、`:283-290` `TogglePanel` 消费枚举；`Menus/ViewPanelMenus.cs` 发布 `TogglePanelVisibilityEvent`（`WorkstationApplication.cs` 不再引用本命名空间） |
| Prism（经 Framework：`Prism.DryIoc.Avalonia`） | `IContainerRegistry`/`IContainerProvider`、`IEventAggregator`、ViewModelLocator（`DelegateCommand` 不再出现于本模块——菜单命令由 Framework 的反射贡献实现包装） | `WorkstationApplication.cs:20、24、28、30`；`MainWindowViewModel.cs:30-39`；`Menus/ViewPanelMenus.cs:12`、`ViewAlignmentMenus.cs:13` 构造注入 `IEventAggregator`；`MainWindow.axaml:12` `AutoWireViewModel` |
| `CommunityToolkit.Mvvm`（经 Framework） | `ObservableObject`、`[ObservableProperty]`、`[RelayCommand]` | `MainWindowViewModel.cs:16、:41-50、:156 等`；`NavigationItemViewModel.cs:10、29`；`PanelTabViewModel.cs:10、29` |
| Avalonia / Ursa（经 Framework/UIPackage） | `Window`/`UserControl`/`GridSplitter`/`StreamGeometry`/`ApplicationLifetime`/`Separator`；`UrsaWindow`（经 FrameworkWindow 间接继承） | 全部 View/code-behind；`Core/Framework/Layout/PanelResizer.cs:13`；`Menus/FileMenus.cs:21`（ApplicationLifetime Shutdown）；Framework 的 `Core/Framework/Menus/MenuItemViewModel.cs:43、47`（StreamGeometry 解析图标、Separator 分隔线）；`MainWindow.axaml:1`（根元素 `win:FrameworkWindow`） |

### 编译设置

`net10.0`、`ImplicitUsings`+`Nullable` enable（Workstation.csproj:4-7）。第 16-26 行三条 `Compile Update ... DependentUpon`（EmptyStateView/AboutWindow/LogView 的 code-behind 嵌套显示），仅 IDE 语义。程序集/根命名空间 `DigitalWorkstation.Workstation`（由 `Build/Base.props` 统一规则，源码命名空间与之一致）。

## 被依赖关系

| 依赖方 | 引用方式 | 用在什么场景 |
|---|---|---|
| `Launcher`（解决方案入口项目） | ProjectReference | 应用启动入口：实例化 `WorkstationApplication` 并运行（Launcher → Workstation → Framework 的传递链见 Models 深读文档）。Workstation 是整个解决方案的应用宿主，除此无其他项目引用它 |
| `Digital.Workstation.slnx` | 解决方案成员 | `/Modules/` 文件夹下两个项目之一（另一个 DashBoard） |

功能模块（DashBoard 及未来模块）**不引用**本模块程序集：它们只实现 Abstractions 的贡献契约，由本模块经 `ShellContributionCollector` 收集——这是刻意的单向依赖（模块不知道 shell 的存在，shell 不知道模块的类型，靠容器+接口+事件解耦）。

## 核心内部数据结构

### `MainWindowViewModel` 私有字段（MainWindowViewModel.cs:18-28）

```csharp
private readonly ShellContributionCollector _collector;         // :18 贡献收集器（Framework）
private readonly IContainerProvider _containerProvider;         // :19 视图实例解析源
private readonly Dictionary<string, NavigationItemViewModel> _itemsById;     // :20 导航项 Id → VM（Top+Bottom 合并）
private readonly Dictionary<string, IMainViewContribution> _mainViewsById;   // :21 主视图 Id → 贡献
private readonly Dictionary<string, object> _mainViewContents;             // :22 主视图 Id → 已解析视图实例（缓存）
private readonly Dictionary<string, object> _sideBarContents;              // :23 导航项 Id → SideBar 内容实例（缓存）
private readonly Dictionary<string, ToolViewContribution> _auxTabsById;     // :24 AuxiliaryPanel tab Id → 工具视图元数据
private readonly Dictionary<string, ToolViewContribution> _bottomTabsById;  // :25 BottomPanel tab Id → 工具视图元数据
private readonly Dictionary<string, object> _auxTabContents;               // :26 aux tab Id → 内容实例（缓存）
private readonly Dictionary<string, object> _bottomTabContents;            // :27 bottom tab Id → 内容实例（缓存）
private bool _contributionsLoaded;                              // :28 EnsureContributionsLoaded 一次性守卫
```

关系要点：五个 `*Contents` 缓存字典是视图实例的**唯一持有者**（除此之外只有 XAML `ContentControl` 的 Content 引用），缓存键 = 贡献的字符串 Id，与 `ShellLayoutState` 里的 `ContentFor`/`ActiveTab`/`ActiveView` 对应。字典索引用赋值（`_mainViewsById[id] = contribution`），重复 Id **静默覆盖**（见 pitfalls.md）。

### 工具视图 attribute（Views/）、贡献类（Contributions/）与菜单类（Menus/）——无字段、无状态，全部数据即 attribute 值/属性值

五个 `[ToolView]` View 的 attribute 矩阵与 `ReadyStatusBarItem` 的属性矩阵见 api.md 第 5 节（四个 attribute 菜单类不实现贡献接口、无 Id——`IMenuItemContribution` 的 Id 已随 ADR-0001 删除；工具视图 Id 即 `[ToolView]` 主构造参数，ADR-0002）。跨类关系由字符串 Id 建立：

```
SettingsView   [ToolView("shell.settings",…)]    ← State.SelectedActivity / _sideBarContents 键（钉住项）
PropertiesView [ToolView("shell.properties",…)]  ┐
OutlineView    [ToolView("shell.outline",…)]     ┴← State.AuxiliaryPanel.Tabs/ActiveTab、_auxTabContents 键
OutputView     [ToolView("shell.output",…)]      ┐
LogView        [ToolView("shell.log",…)]         ┴← State.BottomPanel.Tabs/ActiveTab、_bottomTabContents 键
ReadyStatusBarItem.Id       "shell.status.ready"
```

### 与外部类型的对应关系

| 本模块类型 | 实现/消费的抽象（定义处） |
|---|---|
| `WorkstationApplication` | `FrameworkApplication<TWindow>`（Core/Framework/FrameworkApplication.cs:16），最终基类 `Prism.DryIoc.PrismApplication` |
| 五个 `[ToolView]` View 类（Views/）+ `ReadyStatusBarItem`（Contributions/）+ 四个 attribute 菜单类（Menus/） | `ToolViewAttribute`（Core/Abstractions/Contributions/ToolViewAttribute.cs，ADR-0002）、`IStatusBarItemContribution`（Core/Abstractions/Contributions/）与 `[MenuGroup]`/`[MenuItem]`（Core/Abstractions/Menus/），契约详见 docs/analysis/Core/Abstractions/api.md |
| `MainWindowViewModel.State` | `ShellLayoutState` 一族 record（Core/Framework/Layout/），转换语义详见 docs/analysis/Core/Framework/api.md 第 3 节 |
| `MainWindowViewModel` 的事件订阅 | `OpenMainViewEvent`/`TogglePanelVisibilityEvent`（Core/Models/Events/）与 `SetPanelAlignmentEvent`（Core/Framework/Layout/），负载分别为 `string`（= `IMainViewContribution.Id`）、`TogglePanelTarget` 与 `PanelAlignment`（Layout） |
| `HelpMenus.About` 的弹窗 | `IWindowManager`（Core/Abstractions/WindowManager/），实现为 Framework 的 `FrameworkWindowManager`（`ShowDialog` 要求主窗口已设且 IsActive） |
| `[ToolView]` attribute 与 `ReadyStatusBarItem` 的 `Title`/`IconPath`、菜单类 attribute | `Language`（Core/Resource/Language.cs）/`Icons`（Core/UIPackage/Icons.cs）；attribute 里是资源键字符串，由 Framework 经 `Language.Get` 解析 |
