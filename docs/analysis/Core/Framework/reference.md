# Framework — 模块关系链

## 依赖关系

### 项目引用（Framework.csproj:10-14）

| 依赖 | 用到的能力 | 本模块使用点 |
|---|---|---|
| `Core/Abstractions` | 窗口管理契约 `IWindowManager`、`IMainWindowManager`（`Abstractions/WindowManager/`）；Shell 贡献契约 `INavigationItemContribution`、`IMainViewContribution`、`IPanelTabContribution`、`IStatusBarItemContribution` 及定位枚举 `NavigationItemPlacement`/`PanelPlacement`（`Abstractions/Contributions/`，枚举分别随前两者同文件定义）；菜单贡献契约 `IMenuItemContribution` 与菜单 attribute `MenuGroupAttribute`/`MenuItemAttribute`（`Abstractions/Menus/`，ADR-0001） | `FrameworkWindowManager.cs:12` 实现两个窗口接口；`Contributions/ShellContributionCollector.cs` 五个 `Get*` 方法解析并过滤贡献；`Menus/MenuRegistration.cs:24、38` 读取两个 attribute 扫描注册菜单 |
| `Core/Common` | `IoC`（一次性容器引用持有者）、`Logger`（Serilog 静态封装） | `FrameworkApplication.cs:144` `IoC.Initialize(...)`；`FrameworkWindowManager.cs:37` `IoC.Provider.Resolve(type)`；`FrameworkApplication.cs:92、109` `Logger.Error/Fatal` |
| `Core/Models` | 启动事件三件套：`StartupProgressEvent`/`StartupProgress`/`StartupPhase`、`ModuleLoadFailedEvent`/`ModuleLoadFailure`、`StartupFailureActionEvent`/`StartupFailureAction`（均位于 `Models/Events/`） | `FrameworkApplication.cs:69-104` 发布进度与失败事件；`:117-125` 订阅失败决策事件 |
| `Core/UIPackage` | `WorkstationTheme`（Semi/Ursa 等四个主题包的 Styles 集合）、`VSCodePalette.ApplyTo`（VS Code Dark+ 色键写入） | `FrameworkApplication.cs:25、27`，全应用唯一主题装载点 |
| `Core/Resource` | `Language.Get(string)`（按当前 UI 区域性解析资源键） | `Menus/MenuRegistration.cs:85` 解析菜单条目标题；`Menus/MenuTreeBuilder.cs:59、74、98` 解析路径段标题——`Language.Get` 的首批真实消费方（ADR-0001） |

### NuGet 包（Framework.csproj:18-23）

`Avalonia.Desktop` 11.3.20、`Avalonia.Fonts.Inter` 11.3.20、`Avalonia.Themes.Fluent` 11.3.20、`AvaloniaUI.DiagnosticsSupport` 2.1.1、`CommunityToolkit.Mvvm` 8.4.0；另经 `Prism.DryIoc`（`using Prism.DryIoc;`，`FrameworkApplication.cs:12`）获得 `PrismApplication` 基类与 DryIoc 容器。目标框架 `net10.0`。

## 被依赖关系

| 依赖方 | 引用方式 | 用在什么场景 |
|---|---|---|
| `Modules/Workstation`（Workstation.csproj:10） | ProjectReference | 应用宿主：`WorkstationApplication.cs:12` `WorkstationApplication : FrameworkApplication<MainWindow>`（实现 `CreateSplashWindow`、配置模块目录；`:35` 调 `RegisterMenus` 扫描注册 shell 预置菜单）；`MainWindowViewModel.cs:28,41` 注入 `ShellContributionCollector` 并持有 `ShellLayoutState _state` 驱动整个工作区布局（菜单经 `MenuTreeBuilder.Build` 建树，`:135`）；`PanelResizer.cs` 把拖拽增量交给 `ShellLayoutState.Resize`；`Menus/HelpMenus.cs:20` 用 `IWindowManager.ShowDialog<AboutWindow>` |
| `Modules/DashBoard`（DashBoard.csproj:21） | ProjectReference | 启动台模块：`DashBoardWindowViewModel` 订阅 `StartupProgressEvent`/`ModuleLoadFailedEvent` 并发布 `StartupFailureActionEvent`（即启动台 UI 方，事件负载见 Models 文档）；`DashBoardModule.cs:15` 调 `RegisterMenus` 注册菜单；`DashBoardMenus.cs:20` 用 `IWindowManager.ShowWindow<DashBoardWindow>` 重开启动台 |
| `UnitTest/Framework`（UnitTest/Framework/Framework.csproj:17） | ProjectReference | 唯一的测试项目，仅测 `ShellLayoutState`（见 testing.md） |

即：Framework 是应用骨架，**唯一直接的消费场景是 Workstation 宿主 + DashBoard 启动台**；其余业务模块不引用 Framework，只引用 Abstractions 的贡献契约。

## 核心内部数据结构

### 布局状态族（Layout/，均为 `sealed record`，值相等、with 拷贝）

```
ShellLayoutState (Layout/ShellLayoutState.cs:7)
├── SelectedActivity : string?                当前选中导航项 Id
├── SideBar : SideBarState (Layout/SideBarState.cs:6)
│     Visible(bool=false) / Width(240) / ContentFor(string?)
│     const MinWidth=120, MaxWidth=480
├── AuxiliaryPanel : AuxiliaryPanelState (Layout/AuxiliaryPanelState.cs:6)
│     Visible(true) / Width(280) / Tabs(IReadOnlyList<string>=[]) / ActiveTab(string?)
│     const MinWidth=120, MaxWidth=480
├── BottomPanel : BottomPanelState (Layout/BottomPanelState.cs:6)
│     Visible(true) / Height(160) / Tabs([]) / ActiveTab(string?)
│     const MinHeight=80, MaxHeight=480
└── MainContent : MainContentState (Layout/MainContentState.cs:6)
      ActiveView(string?)
```

`PanelResizeTarget`（Layout/PanelResizeTarget.cs:6）：`Resize` 的目标枚举，三成员对应三个可调区域。

关系要点：`ShellLayoutState` 只持有数据与转换方法，不感知贡献收集与视图解析；`Tabs` 列表由消费方（MainWindowViewModel）从 `ShellContributionCollector.GetPanelTabs` 的结果填入；`ContentFor`/`ActiveTab`/`ActiveView` 的字符串 Id 与 Abstractions 贡献接口的 `Id` 对应（`SelectedActivity` ↔ `INavigationItemContribution.Id`，`ActiveView` ↔ `IMainViewContribution.Id`，`ActiveTab` ↔ `IPanelTabContribution.Id`）。

### 窗口注册表（WindowManager/FrameworkWindowManager.cs）

- `private readonly Dictionary<Type, Window> _windowMap`（第 17 行）：窗口运行时类型 → 当前打开实例，单实例语义（同类型同时只能有一个注册窗口）。
- `private Window? _mainWindow`（第 22 行）：主窗口，由 `HandleMainWindow()` 从 `PrismApplication.MainWindow` 捕获。
- `private const string NullMainWindowError`（第 27 行）：主窗口缺失时的统一错误消息。

### 菜单建树结构（Menus/MenuTreeBuilder.cs + Menus/MenuTreeEntry.cs）

建树产物（对外，`MenuTreeEntry.cs`）：

```
MenuTreeEntry (abstract record, MenuTreeEntry.cs:10)
├── MenuTreeItem(Title, IconPath?, Command)            :15  叶子菜单项，标题已解析
├── MenuTreeSubmenu(Title, Children: IReadOnlyList<MenuTreeEntry>)  :21  子菜单节点（含顶层），
│     Children 中分隔线已插好，无开头/结尾/连续分隔线
└── MenuTreeSeparator                                  :26  组间分隔线标记，单例 Instance（:28）
```

建树期临时累积结构（`MenuTreeBuilder.cs` 私有，不外泄）：

- `LeafAccum`（record，:101）：一条叶子的 `(Group?, GroupOrder, Order, Title, IconPath?, Command)`。
- `NodeAccum`（class，:104）：路径段节点——`Segment`、`Children: Dictionary<string, NodeAccum>`（Ordinal 键）、`Items: List<LeafAccum>`、可空 `Group`/`GroupOrder`/`NodeOrder`；`MergeNodeOrder`（:126，顶层位次多处声明取最小）与 `MergePlacement`（:141，子菜单节点 GroupOrder/NodeOrder 取最小、Group 取 GroupOrder 最小声明、同值取组名 Ordinal 小者）负责合并多处位次声明。

关系要点：`MenuTreeBuilder.Build` 是纯函数，输入 `ShellContributionCollector.GetMenuItems()` 的扁平列表，输出 `IReadOnlyList<MenuTreeSubmenu>`；标题（路径段与条目）在建树时经 `Language.Get` 一次性解析；本模块的 `MenuItemViewModel.FromSubmenu`（Menus/MenuItemViewModel.cs:33）把树递归转为 Avalonia 控件（`MenuTreeSeparator` → `Separator`），由 `FrameworkWindow` 内置的标题栏菜单栏（`FrameworkWindow.cs:35-40`，宽松绑定 ViewModel 的 `MenuBarItems`）呈现。

### 启动序列字段（FrameworkApplication.cs）

- `private IEventAggregator? _eventAggregator`（第 18 行）、`private IMainWindowManager? _windowManager`（第 19 行）：`ResolveFrameworkServices`（第 157 行）在注册阶段解析缓存，启动序列使用。

### 与外部类型的对应关系

| 本模块类型 | 实现/消费的抽象（定义处） |
|---|---|
| `FrameworkWindowManager` | `IWindowManager`（Core/Abstractions/WindowManager/IWindowManager.cs）、`IMainWindowManager`（同目录 IMainWindowManager.cs） |
| `ShellContributionCollector` 的五个返回类型 | `INavigationItemContribution`/`IMainViewContribution`/`IPanelTabContribution`/`IStatusBarItemContribution`（Core/Abstractions/Contributions/）、`IMenuItemContribution`（Core/Abstractions/Menus/） |
| `FrameworkApplication<TWindow>` | `Prism.DryIoc.PrismApplication`（Prism.DryIoc.Avalonia 包） |
| 启动事件负载 | `StartupProgress`/`ModuleLoadFailure`/`StartupPhase`/`StartupFailureAction`（Core/Models/Events/） |
