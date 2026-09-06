# DashBoard — 模块关系链

## 依赖关系

### 项目引用（DashBoard.csproj:20-23）

| 依赖 | 用到的能力 | 本模块使用点 |
|---|---|---|
| `Core/Abstractions` | Shell 贡献契约：`INavigationItemContribution`、`IMainViewContribution`、`IPanelTabContribution`、`IMenuItemContribution`、`IStatusBarItemContribution` 及定位枚举 `NavigationItemPlacement`/`PanelPlacement`/`MenuPlacement`（Abstractions/Shell/）；窗口管理契约 `IWindowManager` + 泛型扩展 `ShowWindow<TWindow>`（Abstractions/WindowManager/） | 五个贡献类各实现一个接口（如 DashBoardNavigationItem.cs:11）；`OpenDashBoardMenuItem.cs:3、16、18` 注入 `IWindowManager` 并用 `windowManager.ShowWindow<DashBoardWindow>` 构建 `DelegateCommand` |
| `Core/Framework` | 间接获得 Prism（`IModule`、`IContainerRegistry`、`IEventAggregator`、`PubSubEvent`、`DelegateCommand`、`ThreadOption`）与 CommunityToolkit.Mvvm（`ObservableObject`、`[ObservableProperty]`、`[RelayCommand]`）的传递引用；运行期由其提供 `IWindowManager` 实现与 `IoC` 初始化 | `DashBoardModule.cs:6` `IModule`；`DashBoardWindowViewModel.cs:1-2、12、69、78` |
| `Core/Resource` | `Language` 本地化字符串：`DashBoardNavigationTitle`、`DashBoardTasksTabTitle`、`DashBoardOpenWindowMenuTitle`、`SplashStartingText`、`SplashPhaseCoreServices`、`SplashPhaseLoadingModules`、`SplashPhaseReady`、`SplashPhaseFailed`（Resource/Language.cs；中英值在 Language.resx / Language.en-US.resx） | 各贡献类的 `Title` 属性；`DashBoardWindowViewModel.cs:24、43-45、56` |
| `Core/UIPackage` | `Icons` 图标路径常量：`Icons.DashBoard`（四宫格）、`Icons.Tasks`（勾选清单）（UIPackage/Icons.cs:18、46） | `DashBoardNavigationItem.cs:17`、`DashBoardTasksPanelTab.cs:17`、`DashBoardStatusBarItem.cs:17`、`OpenDashBoardMenuItem.cs:25` |

### 传递依赖（未在 csproj 直接引用，但源码 using 其命名空间）

| 传递来源 | 用到的能力 | 使用点 |
|---|---|---|
| `Core/Models`（经 Framework 传递） | 启动事件三件套：`StartupProgressEvent`/`StartupProgress`/`StartupPhase`、`ModuleLoadFailedEvent`/`ModuleLoadFailure`、`StartupFailureActionEvent`/`StartupFailureAction`；工作区事件 `OpenMainViewEvent`（均 Models/Events/） | `DashBoardWindowViewModel.cs:3、19-20、38、53、72、81`；`DashBoardNavigationView.axaml.cs:4、30、35` |
| `Core/Common`（经 Framework 传递） | `IoC.Provider`（一次性容器引用持有者） | `DashBoardNavigationView.axaml.cs:3、18` 无参构造 `IoC.Provider.Resolve<IEventAggregator>()` |

### NuGet / 框架

`net10.0`、`ImplicitUsings`+`Nullable` enable（DashBoard.csproj:4-7）。Avalonia（`Window`/`UserControl`/`RoutedEventArgs`）、Prism、CommunityToolkit.Mvvm 均经 Framework 传递。csproj 第 9-17 行另有两条 `Compile Update ... DependentUpon`（IDE 嵌套显示，对构建无语义影响）：`Views\Windows\DashBoardWindow.axaml.cs`（带 `SubType=Code`）与 `Views\DashBoardTasksView.axaml.cs`。

## 被依赖关系

| 依赖方 | 引用方式 | 用在什么场景 |
|---|---|---|
| `Modules/Workstation`（Workstation.csproj） | ProjectReference | 应用宿主：`WorkstationApplication.cs:16` `moduleCatalog.AddModule<DashBoardModule>()` 注册模块；`WorkstationApplication.cs:51-54` `CreateSplashWindow()` 重写返回 `Container.Resolve<DashBoardWindow>()`——启动序列逐模块加载前直接解析显示启动台（ADR-0004） |
| `Digital.Workstation.slnx`（第 11 行） | 解决方案成员 | `/Modules/` 文件夹下两个项目之一（另一个是 Workstation） |

除 Workstation 宿主外**无任何项目引用 DashBoard**；shell（Workstation 内）对贡献的消费全部经 Abstractions 接口完成，不引用本模块程序集。`UnitTest/` 下无 DashBoard 测试项目（见 testing.md）。

## 核心内部数据结构

### 贡献类属性矩阵（定义文件见 api.md 表）

六个贡献类无字段、无状态，全部数据即属性值；跨类关系由字符串 Id 建立：

```
DashBoardNavigationItem.Id  "dashboard"            ← shell SelectedActivity / SideBar 内容解析键
DashBoardOverviewMainView.ViewId  "dashboard.overview"  ┐
DashBoardRecentMainView.ViewId    "dashboard.recent"    ├← OpenMainViewEvent 负载（DashBoardNavigationView 发布）
DashBoardTasksPanelTab.Id   "dashboard.tasks"      ← BottomPanel Tabs/ActiveTab 键
DashBoardStatusBarItem.Id   "dashboard.status"
OpenDashBoardMenuItem.Id    "dashboard.menu.open"
```

### `DashBoardWindowViewModel` 内部状态（ViewModels/Windows/DashBoardWindowViewModel.cs）

- `private readonly IEventAggregator _eventAggregator`（第 14 行）：构造注入，存活期与 ViewModel 相同；订阅用 `keepSubscriberReferenceAlive: true`，事件聚合器持强引用，无需显式退订。
- 四个 `[ObservableProperty]` 字段（第 23-36 行）即全部 UI 状态：`PhaseText`/`ModuleText`/`IsFailed`/`ErrorMessage`；状态机只有两个有效组合——进行态（`IsFailed=false`，`PhaseText` 为三阶段文案之一）与失败态（`IsFailed=true`，`PhaseText=SplashPhaseFailed`，`ErrorMessage` 非空）。`OnProgress` 第 40 行每次先把 `IsFailed` 复位，失败后来新进度即恢复进行态。

### 与外部类型的对应关系

| 本模块类型 | 实现/消费的抽象（定义处） |
|---|---|
| `DashBoardModule` | `Prism.Modularity.IModule`（Prism.DryIoc 传递） |
| 五个贡献类 | `Core/Abstractions/Shell/` 下五个 `I*Contribution` 接口（详见 docs/analysis/Core/Abstractions/api.md） |
| `DashBoardWindowViewModel` 的事件订阅/发布 | `StartupProgressEvent`/`ModuleLoadFailedEvent`/`StartupFailureActionEvent`/`OpenMainViewEvent`（Core/Models/Events/，详见 docs/analysis/Core/Models/） |
| `DashBoardWindow` 的打开方 | `IWindowManager.ShowWindow`（Core/Abstractions/WindowManager/IWindowManager.cs），实现为 Core/Framework `FrameworkWindowManager`——同类型窗口单实例，`Closing` 后从 `_windowMap` 移除，关闭后可再次 `ShowWindow` |
| `DashBoardNavigationView` 无参构造的解析源 | `IoC.Provider`（Core/Common），由 `FrameworkApplication.RegisterFrameworkServices` 初始化 |
