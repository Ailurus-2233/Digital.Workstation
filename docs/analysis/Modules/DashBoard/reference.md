# DashBoard — 模块关系链

## 依赖关系

### 项目引用（DashBoard.csproj:20-23）

| 依赖 | 用到的能力 | 本模块使用点 |
|---|---|---|
| `Core/Abstractions` | Shell 贡献契约：工具视图 attribute `ToolViewAttribute` 与放置枚举 `ToolViewPlacement { ActivityBar, AuxiliaryPanel, BottomPanel }`（ADR-0002，Abstractions/Contributions/ToolViewAttribute.cs）、`IMainViewContribution`、`IStatusBarItemContribution`（Abstractions/Contributions/） | 两个工具视图的 `[ToolView]` 标注（using `DigitalWorkstation.Core.Abstractions.Contributions`：DashBoardNavigationView.axaml.cs:3、14-15；DashBoardTasksView.axaml.cs:2、10-11）；`DashBoardOverviewMainView.cs:1、9`/`DashBoardRecentMainView.cs:1、9`/`DashBoardStatusBarItem.cs:1、11` 各实现一个接口 |
| `Core/Framework` | 间接获得 Prism（`IModule`、`IContainerRegistry`、`IEventAggregator`、`PubSubEvent`、`ThreadOption`）与 CommunityToolkit.Mvvm（`ObservableObject`、`[ObservableProperty]`、`[RelayCommand]`）的传递引用；运行期由其提供 `IoC` 初始化与 `RegisterToolViews` 工具视图注册扩展（Framework/Contributions/ToolViewRegistration.cs，ADR-0002） | `DashBoardModule.cs:2、7、13` `IModule` 与 `RegisterToolViews`（using `DigitalWorkstation.Core.Framework.Contributions`，第 2 行）；`DashBoardWindowViewModel.cs:1-2、12、69、78` |
| `Core/Resource` | `Language` 本地化字符串：`DashBoardNavigationTitle`、`DashBoardTasksTabTitle`、`SplashStartingText`、`SplashPhaseCoreServices`、`SplashPhaseLoadingModules`、`SplashPhaseReady`、`SplashPhaseFailed`（Resource/Language.cs；中英值在 Language.resx / Language.en-US.resx） | `DashBoardStatusBarItem.Title` 属性（DashBoardStatusBarItem.cs:15）；两个 `[ToolView]` 以字符串 TitleKey 引用（`"DashBoardNavigationTitle"`/`"DashBoardTasksTabTitle"`，扫描时经 `Language.Get` 解析）；`DashBoardWindowViewModel.cs:24、43-45、56` |
| `Core/UIPackage` | `Icons` 图标路径常量：`Icons.DashBoard`（四宫格）、`Icons.Tasks`（勾选清单）（UIPackage/Icons.cs:18、46） | `DashBoardNavigationView.axaml.cs:14`（`[ToolView]` 的 `Icon`）、`DashBoardTasksView.axaml.cs:10`、`DashBoardStatusBarItem.cs:17` |

### 传递依赖（未在 csproj 直接引用，但源码 using 其命名空间）

| 传递来源 | 用到的能力 | 使用点 |
|---|---|---|
| `Core/Models`（经 Framework 传递） | 启动事件三件套：`StartupProgressEvent`/`StartupProgress`/`StartupPhase`、`ModuleLoadFailedEvent`/`ModuleLoadFailure`、`StartupFailureActionEvent`/`StartupFailureAction`；工作区事件 `OpenMainViewEvent`（均 Models/Events/） | `DashBoardWindowViewModel.cs:3、19-20、38、53、72、81`；`DashBoardNavigationView.axaml.cs:5、35、40` |
| `Core/Common`（经 Framework 传递） | `IoC.Provider`（一次性容器引用持有者） | `DashBoardNavigationView.axaml.cs:4、23` 无参构造 `IoC.Provider.Resolve<IEventAggregator>()` |

### NuGet / 框架

`net10.0`、`ImplicitUsings`+`Nullable` enable（DashBoard.csproj:4-7）。Avalonia（`Window`/`UserControl`/`RoutedEventArgs`）、Prism、CommunityToolkit.Mvvm 均经 Framework 传递。csproj 第 9-17 行另有两条 `Compile Update ... DependentUpon`（IDE 嵌套显示，对构建无语义影响）：`Views\Windows\DashBoardWindow.axaml.cs`（带 `SubType=Code`）与 `Views\DashBoardTasksView.axaml.cs`。

## 被依赖关系

| 依赖方 | 引用方式 | 用在什么场景 |
|---|---|---|
| `Modules/Workstation`（Workstation.csproj） | ProjectReference | 应用宿主：`WorkstationApplication.cs:18` `moduleCatalog.AddModule<DashBoardModule>()` 注册模块；`WorkstationApplication.cs:41-44` `CreateSplashWindow()` 重写返回 `Container.Resolve<DashBoardWindow>()`——启动序列逐模块加载前直接解析显示启动台（ADR-0004） |
| `Digital.Workstation.slnx`（第 11 行） | 解决方案成员 | `/Modules/` 文件夹下两个项目之一（另一个是 Workstation） |

除 Workstation 宿主外**无任何项目引用 DashBoard**；shell（Workstation 内）对贡献的消费全部经 Abstractions 接口完成，不引用本模块程序集。`UnitTest/` 下无 DashBoard 测试项目（见 testing.md）。

## 核心内部数据结构

### 贡献声明矩阵（声明值详表见 api.md）

三个接口贡献类无字段、无状态，全部数据即属性值；两个工具视图只有 `[ToolView]` attribute 声明（元数据 `ToolViewContribution` 由 `RegisterToolViews` 扫描生成）。跨类关系由字符串 Id 建立：

```
[ToolView] DashBoardNavigationView  "dashboard"          ← shell SelectedActivity / SideBar 内容解析键（ToolViewContribution.Id）
DashBoardOverviewMainView.ViewId  "dashboard.overview"  ┐
DashBoardRecentMainView.ViewId    "dashboard.recent"    ├← OpenMainViewEvent 负载（DashBoardNavigationView 发布）
[ToolView] DashBoardTasksView       "dashboard.tasks"    ← BottomPanel Tabs/ActiveTab 键
DashBoardStatusBarItem.Id   "dashboard.status"
```

### `DashBoardWindowViewModel` 内部状态（ViewModels/Windows/DashBoardWindowViewModel.cs）

- `private readonly IEventAggregator _eventAggregator`（第 14 行）：构造注入，存活期与 ViewModel 相同；订阅用 `keepSubscriberReferenceAlive: true`，事件聚合器持强引用，无需显式退订。
- 四个 `[ObservableProperty]` 字段（第 23-36 行）即全部 UI 状态：`PhaseText`/`ModuleText`/`IsFailed`/`ErrorMessage`；状态机只有两个有效组合——进行态（`IsFailed=false`，`PhaseText` 为三阶段文案之一）与失败态（`IsFailed=true`，`PhaseText=SplashPhaseFailed`，`ErrorMessage` 非空）。`OnProgress` 第 40 行每次先把 `IsFailed` 复位，失败后来新进度即恢复进行态。

### 与外部类型的对应关系

| 本模块类型 | 实现/消费的抽象（定义处） |
|---|---|
| `DashBoardModule` | `Prism.Modularity.IModule`（Prism.DryIoc 传递） |
| 三个接口贡献类（`DashBoardOverviewMainView`/`DashBoardRecentMainView`/`DashBoardStatusBarItem`） | `Core/Abstractions/Contributions/` 下 `IMainViewContribution`/`IStatusBarItemContribution` 两个接口（详见 docs/analysis/Core/Abstractions/api.md） |
| 两个 `[ToolView]` 视图（`DashBoardNavigationView`/`DashBoardTasksView`） | `ToolViewAttribute`/`ToolViewPlacement`（Core/Abstractions/Contributions/ToolViewAttribute.cs，ADR-0002）；经 Core/Framework `RegisterToolViews` 扩展（Framework/Contributions/ToolViewRegistration.cs）扫描生成 `ToolViewContribution` 元数据单例并把 View 注册进容器，由 `ShellContributionCollector.GetToolViews()` 收集（详见 docs/analysis/Core/Framework/api.md） |
| `DashBoardWindowViewModel` 的事件订阅/发布 | `StartupProgressEvent`/`ModuleLoadFailedEvent`/`StartupFailureActionEvent`/`OpenMainViewEvent`（Core/Models/Events/，详见 docs/analysis/Core/Models/） |
| `DashBoardWindow` 的显示方 | shell 启动序列直接 `Container.Resolve<DashBoardWindow>()` 显示（`WorkstationApplication.CreateSplashWindow`）；模块内不再有重开通路（原菜单/命令贡献已删除） |
| `DashBoardNavigationView` 无参构造的解析源 | `IoC.Provider`（Core/Common），由 `FrameworkApplication.RegisterFrameworkServices` 初始化 |
