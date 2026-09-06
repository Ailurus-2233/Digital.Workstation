# Framework — 模块关系链

## 依赖关系

### 项目引用（Framework.csproj:10-13）

| 依赖 | 用到的能力 | 本模块使用点 |
|---|---|---|
| `Core/Abstractions` | 窗口管理契约 `IWindowManager`、`IMainWindowManager`（`Abstractions/WindowManager/`）；Shell 贡献契约 `INavigationItemContribution`、`IMainViewContribution`、`IPanelTabContribution`、`IMenuItemContribution`、`IStatusBarItemContribution` 及定位枚举 `NavigationItemPlacement`/`PanelPlacement`/`MenuPlacement`（`Abstractions/Shell/`） | `FrameworkWindowManager.cs:12` 实现两个窗口接口；`ShellContributionCollector.cs` 五个 `Get*` 方法解析并过滤贡献 |
| `Core/Common` | `IoC`（一次性容器引用持有者）、`Logger`（Serilog 静态封装） | `FrameworkApplication.cs:144` `IoC.Initialize(...)`；`FrameworkWindowManager.cs:37` `IoC.Provider.Resolve(type)`；`FrameworkApplication.cs:92、109` `Logger.Error/Fatal` |
| `Core/Models` | 启动事件三件套：`StartupProgressEvent`/`StartupProgress`/`StartupPhase`、`ModuleLoadFailedEvent`/`ModuleLoadFailure`、`StartupFailureActionEvent`/`StartupFailureAction`（均位于 `Models/Events/`） | `FrameworkApplication.cs:69-104` 发布进度与失败事件；`:117-125` 订阅失败决策事件 |
| `Core/UIPackage` | `WorkstationTheme`（Semi/Ursa 等四个主题包的 Styles 集合）、`VSCodePalette.ApplyTo`（VS Code Dark+ 色键写入） | `FrameworkApplication.cs:25、27`，全应用唯一主题装载点 |

### NuGet 包（Framework.csproj:16-22）

`Avalonia.Desktop` 11.3.20、`Avalonia.Fonts.Inter` 11.3.20、`Avalonia.Themes.Fluent` 11.3.20、`AvaloniaUI.DiagnosticsSupport` 2.1.1、`CommunityToolkit.Mvvm` 8.4.0；另经 `Prism.DryIoc`（`using Prism.DryIoc;`，`FrameworkApplication.cs:12`）获得 `PrismApplication` 基类与 DryIoc 容器。目标框架 `net10.0`。

## 被依赖关系

| 依赖方 | 引用方式 | 用在什么场景 |
|---|---|---|
| `Modules/Workstation`（Workstation.csproj:10） | ProjectReference | 应用宿主：`WorkstationApplication.cs:12` `WorkstationApplication : FrameworkApplication<MainWindow>`（实现 `CreateSplashWindow`、配置模块目录）；`MainWindowViewModel.cs:27,38` 注入 `ShellContributionCollector` 并持有 `ShellLayoutState _state` 驱动整个工作区布局；`PanelResizer.cs` 把拖拽增量交给 `ShellLayoutState.Resize`；`Shell/AboutMenuItem.cs:17` 用 `IWindowManager.ShowDialog<AboutWindow>` |
| `Modules/DashBoard`（DashBoard.csproj:21） | ProjectReference | 启动台模块：`DashBoardWindowViewModel` 订阅 `StartupProgressEvent`/`ModuleLoadFailedEvent` 并发布 `StartupFailureActionEvent`（即启动台 UI 方，事件负载见 Models 文档）；`OpenDashBoardMenuItem.cs:18` 用 `IWindowManager.ShowWindow<DashBoardWindow>` 重开启动台 |
| `UnitTest/Framework`（UnitTest/Framework/Framework.csproj:17） | ProjectReference | 唯一的测试项目，仅测 `ShellLayoutState`（见 testing.md） |

即：Framework 是应用骨架，**唯一直接的消费场景是 Workstation 宿主 + DashBoard 启动台**；其余业务模块不引用 Framework，只引用 Abstractions 的贡献契约。

## 核心内部数据结构

### 布局状态族（Shell/，均为 `sealed record`，值相等、with 拷贝）

```
ShellLayoutState (Shell/ShellLayoutState.cs:7)
├── SelectedActivity : string?                当前选中导航项 Id
├── SideBar : SideBarState (Shell/SideBarState.cs:6)
│     Visible(bool=false) / Width(240) / ContentFor(string?)
│     const MinWidth=120, MaxWidth=480
├── AuxiliaryPanel : AuxiliaryPanelState (Shell/AuxiliaryPanelState.cs:6)
│     Visible(true) / Width(280) / Tabs(IReadOnlyList<string>=[]) / ActiveTab(string?)
│     const MinWidth=120, MaxWidth=480
├── BottomPanel : BottomPanelState (Shell/BottomPanelState.cs:6)
│     Visible(true) / Height(160) / Tabs([]) / ActiveTab(string?)
│     const MinHeight=80, MaxHeight=480
└── MainContent : MainContentState (Shell/MainContentState.cs:6)
      ActiveView(string?)
```

`PanelResizeTarget`（Shell/PanelResizeTarget.cs:6）：`Resize` 的目标枚举，三成员对应三个可调区域。

关系要点：`ShellLayoutState` 只持有数据与转换方法，不感知贡献收集与视图解析；`Tabs` 列表由消费方（MainWindowViewModel）从 `ShellContributionCollector.GetPanelTabs` 的结果填入；`ContentFor`/`ActiveTab`/`ActiveView` 的字符串 Id 与 Abstractions 贡献接口的 `Id` 对应（`SelectedActivity` ↔ `INavigationItemContribution.Id`，`ActiveView` ↔ `IMainViewContribution.Id`，`ActiveTab` ↔ `IPanelTabContribution.Id`）。

### 窗口注册表（WindowManager/FrameworkWindowManager.cs）

- `private readonly Dictionary<Type, Window> _windowMap`（第 17 行）：窗口运行时类型 → 当前打开实例，单实例语义（同类型同时只能有一个注册窗口）。
- `private Window? _mainWindow`（第 22 行）：主窗口，由 `HandleMainWindow()` 从 `PrismApplication.MainWindow` 捕获。
- `private const string NullMainWindowError`（第 27 行）：主窗口缺失时的统一错误消息。

### 启动序列字段（FrameworkApplication.cs）

- `private IEventAggregator? _eventAggregator`（第 18 行）、`private IMainWindowManager? _windowManager`（第 19 行）：`ResolveFrameworkServices`（第 157 行）在注册阶段解析缓存，启动序列使用。

### 与外部类型的对应关系

| 本模块类型 | 实现/消费的抽象（定义处） |
|---|---|
| `FrameworkWindowManager` | `IWindowManager`（Core/Abstractions/WindowManager/IWindowManager.cs）、`IMainWindowManager`（同目录 IMainWindowManager.cs） |
| `ShellContributionCollector` 的五个返回类型 | `INavigationItemContribution`/`IMainViewContribution`/`IPanelTabContribution`/`IMenuItemContribution`/`IStatusBarItemContribution`（Core/Abstractions/Shell/） |
| `FrameworkApplication<TWindow>` | `Prism.DryIoc.PrismApplication`（Prism.DryIoc.Avalonia 包） |
| 启动事件负载 | `StartupProgress`/`ModuleLoadFailure`/`StartupPhase`/`StartupFailureAction`（Core/Models/Events/） |
