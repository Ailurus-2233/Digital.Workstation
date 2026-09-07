# Workstation — 模块关系链

## 依赖关系

### 项目引用（Workstation.csproj:9-14）

| 依赖 | 用到的能力 | 本模块使用点 |
|---|---|---|
| `Core/Framework` | `FrameworkApplication<TWindow>` 应用入口基类（Framework/FrameworkApplication.cs）；`ShellContributionCollector` 贡献收集器、`ShellLayoutState` 及区域 record、`PanelResizeTarget` 枚举（Framework/Shell/） | `WorkstationApplication.cs:12` 继承；`MainWindowViewModel.cs:15` 注入 collector、`:38` 持有 `ShellLayoutState _state`、`:241` `ResizePanel` 消费 `PanelResizeTarget`；`MainWindow.axaml.cs:20、28、36` 三个拖拽 handler |
| `Core/Resource` | `Language` 本地化字符串（Resource/Language.cs） | `Shell/` 全部九个贡献类的 `Title` 属性（如 `TogglePanelContribution.cs:31-33`、`ExitMenuItem.cs:18`） |
| `Core/UIPackage` | `Icons` 图标路径常量（UIPackage/Icons.cs）；Ursa/Semi 主题资源键（`SemiColor*`、`Chrome*`，运行期由 Framework 装载主题后可用） | 贡献类 `IconPath` 属性；`MainWindowViewModel.cs:91、96` 收起按钮图标；`MainWindow.axaml` 全部 `{DynamicResource ...}` |
| `Modules/DashBoard` | `DashBoardModule`（Prism 模块）、`DashBoardWindow`（启动台窗口） | `WorkstationApplication.cs:16` `AddModule<DashBoardModule>()`、`:53` `CreateSplashWindow()` 返回 `Container.Resolve<DashBoardWindow>()` |

### 传递依赖（未在 csproj 直接引用，源码 using 其命名空间）

| 传递来源 | 用到的能力 | 使用点 |
|---|---|---|
| `Core/Abstractions`（经 Framework） | 五个贡献接口与定位枚举：`INavigationItemContribution`/`IMainViewContribution`/`IPanelTabContribution`/`IMenuItemContribution`/`IStatusBarItemContribution`、`NavigationItemPlacement`/`PanelPlacement`/`MenuPlacement`（Abstractions/Shell/）；`IWindowManager`（Abstractions/WindowManager/） | `Shell/` 九个贡献类各实现一个接口；`MainWindowViewModel.cs:150-175` 收集方法参数；`Shell/AboutMenuItem.cs:15-17` 注入 `IWindowManager` |
| `Core/Models`（经 Framework） | `OpenMainViewEvent`、`TogglePanelVisibilityEvent`、`TogglePanelTarget`（Models/Events/） | `MainWindowViewModel.cs:33-34` 订阅两事件、`:309-316` `TogglePanel` 消费枚举；`WorkstationApplication.cs:37` 遍历枚举注册；`Shell/TogglePanelContribution.cs:19` 发布事件 |
| Prism（经 Framework：`Prism.DryIoc.Avalonia`） | `IContainerRegistry`/`IContainerProvider`、`IEventAggregator`、`DelegateCommand`、ViewModelLocator | `WorkstationApplication.cs:19、39-40`；`MainWindowViewModel.cs:28-36`；`Shell/ExitMenuItem.cs:26`、`AboutMenuItem.cs:17`、`TogglePanelContribution.cs:18`；`MainWindow.axaml:12` `AutoWireViewModel` |
| `CommunityToolkit.Mvvm`（经 Framework） | `ObservableObject`、`[ObservableProperty]`、`[RelayCommand]` | `MainWindowViewModel.cs:14、:38-49、:181 等`；`NavigationItemViewModel.cs:10、29`；`PanelTabViewModel.cs:10、29` |
| Avalonia / Ursa（经 Framework/UIPackage） | `Window`/`UserControl`/`GridSplitter`/`StreamGeometry`/`ApplicationLifetime`；`UrsaWindow`（经 FrameworkWindow 间接继承） | 全部 View/code-behind；`Core/Framework/Shell/PanelResizer.cs:13`；`Shell/ExitMenuItem.cs:27`；`MainWindow.axaml:1`（根元素 `shell:FrameworkWindow`） |

### 编译设置

`net10.0`、`ImplicitUsings`+`Nullable` enable（Workstation.csproj:4-7）。第 16-26 行三条 `Compile Update ... DependentUpon`（EmptyStateView/AboutWindow/LogView 的 code-behind 嵌套显示），仅 IDE 语义。程序集/根命名空间 `DigitalWorkstation.Workstation`（由 `Build/Base.props` 统一规则，源码命名空间与之一致）。

## 被依赖关系

| 依赖方 | 引用方式 | 用在什么场景 |
|---|---|---|
| `Launcher`（解决方案入口项目） | ProjectReference | 应用启动入口：实例化 `WorkstationApplication` 并运行（Launcher → Workstation → Framework 的传递链见 Models 深读文档）。Workstation 是整个解决方案的应用宿主，除此无其他项目引用它 |
| `Digital.Workstation.slnx` | 解决方案成员 | `/Modules/` 文件夹下两个项目之一（另一个 DashBoard） |

功能模块（DashBoard 及未来模块）**不引用**本模块程序集：它们只实现 Abstractions 的贡献契约，由本模块经 `ShellContributionCollector` 收集——这是刻意的单向依赖（模块不知道 shell 的存在，shell 不知道模块的类型，靠容器+接口+事件解耦）。

## 核心内部数据结构

### `MainWindowViewModel` 私有字段（MainWindowViewModel.cs:15-25）

```csharp
private readonly ShellContributionCollector _collector;         // :15 贡献收集器（Framework）
private readonly IContainerProvider _containerProvider;         // :16 视图实例解析源
private readonly Dictionary<string, NavigationItemViewModel> _itemsById;     // :17 导航项 Id → VM（Top+Bottom 合并）
private readonly Dictionary<string, IMainViewContribution> _mainViewsById;   // :18 主视图 Id → 贡献
private readonly Dictionary<string, object> _mainViewContents;             // :19 主视图 Id → 已解析视图实例（缓存）
private readonly Dictionary<string, object> _sideBarContents;              // :20 导航项 Id → SideBar 内容实例（缓存）
private readonly Dictionary<string, IPanelTabContribution> _auxTabsById;   // :21 AuxiliaryPanel tab Id → 贡献
private readonly Dictionary<string, IPanelTabContribution> _bottomTabsById;// :22 BottomPanel tab Id → 贡献
private readonly Dictionary<string, object> _auxTabContents;               // :23 aux tab Id → 内容实例（缓存）
private readonly Dictionary<string, object> _bottomTabContents;            // :24 bottom tab Id → 内容实例（缓存）
private bool _contributionsLoaded;                              // :25 EnsureContributionsLoaded 一次性守卫
```

关系要点：五个 `*Contents` 缓存字典是视图实例的**唯一持有者**（除此之外只有 XAML `ContentControl` 的 Content 引用），缓存键 = 贡献的字符串 Id，与 `ShellLayoutState` 里的 `ContentFor`/`ActiveTab`/`ActiveView` 对应。字典索引用赋值（`_mainViewsById[id] = contribution`），重复 Id **静默覆盖**（见 pitfalls.md）。

### 贡献类（Shell/）——无字段、无状态，全部数据即属性值

九个贡献类的属性矩阵见 api.md 第 5 节。跨类关系由字符串 Id 建立：

```
SettingsNavigationItem.Id   "shell.settings"            ← State.SelectedActivity / _sideBarContents 键
PropertiesPanelTab.Id       "shell.properties"  ┐
OutlinePanelTab.Id          "shell.outline"     ┴← State.AuxiliaryPanel.Tabs/ActiveTab、_auxTabContents 键
OutputPanelTab.Id           "shell.output"      ┐
LogPanelTab.Id              "shell.log"         ┴← State.BottomPanel.Tabs/ActiveTab、_bottomTabContents 键
TogglePanelContribution.Id  "shell.toggle-sidebar|bottompanel|auxiliarypanel"
ExitMenuItem.Id             "shell.menu.exit"
AboutMenuItem.Id            "shell.menu.about"
ReadyStatusBarItem.Id       "shell.status.ready"
```

### 与外部类型的对应关系

| 本模块类型 | 实现/消费的抽象（定义处） |
|---|---|
| `WorkstationApplication` | `FrameworkApplication<TWindow>`（Core/Framework/FrameworkApplication.cs:16），最终基类 `Prism.DryIoc.PrismApplication` |
| 九个 Shell 贡献类 | `I*Contribution` 五接口（Core/Abstractions/Shell/），契约详见 docs/analysis/Core/Abstractions/api.md |
| `MainWindowViewModel.State` | `ShellLayoutState` 一族 record（Core/Framework/Shell/），转换语义详见 docs/analysis/Core/Framework/api.md 第 3 节 |
| `MainWindowViewModel` 的事件订阅 | `OpenMainViewEvent`/`TogglePanelVisibilityEvent`（Core/Models/Events/），负载分别为 `string`（= `IMainViewContribution.Id`）与 `TogglePanelTarget` |
| `AboutMenuItem` 的弹窗 | `IWindowManager`（Core/Abstractions/WindowManager/），实现为 Framework 的 `FrameworkWindowManager`（`ShowDialog` 要求主窗口已设且 IsActive） |
| 贡献类 `Title`/`IconPath` | `Language`（Core/Resource/Language.cs）/`Icons`（Core/UIPackage/Icons.cs） |
