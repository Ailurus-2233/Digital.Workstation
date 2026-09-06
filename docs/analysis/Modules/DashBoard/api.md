# DashBoard — 对外接口与调用方式

模块命名空间：`DigitalWorkstation.DashBoard`（根）、`DigitalWorkstation.DashBoard.Views`、`DigitalWorkstation.DashBoard.Views.Windows`、`DigitalWorkstation.DashBoard.ViewModels.Windows`。

## 公开 API 面

### 1. `DashBoardModule : IModule`（DashBoardModule.cs:6）

Prism 模块入口，被模块目录反射调用，**不被业务代码直接调用**。

| 成员 | 签名 | 说明 |
|---|---|---|
| `RegisterTypes` | `void RegisterTypes(IContainerRegistry containerRegistry)` | 注册 6 个贡献单例 + 4 个视图（见下"注册清单"） |
| `OnInitialized` | `void OnInitialized(IContainerProvider containerProvider)` | **空实现**（DashBoardModule.cs:24 注释：启动台窗口由 shell 启动序列在模块加载前显示（ADR-0004），模块自身不再开窗） |

注册清单（DashBoardModule.cs:10-19）：
- `RegisterSingleton<INavigationItemContribution, DashBoardNavigationItem>()`
- `RegisterSingleton<IMainViewContribution, DashBoardOverviewMainView>()`
- `RegisterSingleton<IMainViewContribution, DashBoardRecentMainView>()`
- `RegisterSingleton<IPanelTabContribution, DashBoardTasksPanelTab>()`
- `RegisterSingleton<IMenuItemContribution, OpenDashBoardMenuItem>()`
- `RegisterSingleton<IStatusBarItemContribution, DashBoardStatusBarItem>()`
- `Register<DashBoardNavigationView>()` / `<DashBoardOverviewView>()` / `<DashBoardRecentView>()` / `<DashBoardTasksView>()`（瞬态）

注意：`DashBoardWindow` 与 `DashBoardWindowViewModel` **不在** `RegisterTypes` 中注册——`DashBoardWindow` 由启动序列在模块加载前经 `Container.Resolve<DashBoardWindow>()`（WorkstationApplication.cs:53）解析，Prism 容器对未注册的具体类型仍可构造解析（DryIoc 默认行为），ViewModel 由 ViewModelLocator 约定装配。

### 2. 贡献类（六个，均只有属性，供 shell 收集消费）

| 类 | 实现接口 | `Id` | `Title` | `IconPath` | `Order` | 定位 | 视图类型 |
|---|---|---|---|---|---|---|---|
| `DashBoardNavigationItem`（DashBoardNavigationItem.cs:11） | `INavigationItemContribution` | `"dashboard"` | `Language.DashBoardNavigationTitle` | `Icons.DashBoard` | 0 | `NavigationItemPlacement.Top` | `ContentViewType => typeof(DashBoardNavigationView)` |
| `DashBoardOverviewMainView`（DashBoardOverviewMainView.cs:9） | `IMainViewContribution` | `ViewId` 常量 `"dashboard.overview"` | —（接口无 Title） | — | — | — | `ViewType => typeof(DashBoardOverviewView)` |
| `DashBoardRecentMainView`（DashBoardRecentMainView.cs:9） | `IMainViewContribution` | `ViewId` 常量 `"dashboard.recent"` | — | — | — | — | `ViewType => typeof(DashBoardRecentView)` |
| `DashBoardTasksPanelTab`（DashBoardTasksPanelTab.cs:11） | `IPanelTabContribution` | `"dashboard.tasks"` | `Language.DashBoardTasksTabTitle` | `Icons.Tasks` | 15 | `PanelPlacement.Bottom` | `ContentViewType => typeof(DashBoardTasksView)` |
| `DashBoardStatusBarItem`（DashBoardStatusBarItem.cs:11） | `IStatusBarItemContribution` | `"dashboard.status"` | `Language.DashBoardNavigationTitle`（复用导航标题） | `Icons.DashBoard` | 20 | — | — |
| `OpenDashBoardMenuItem`（OpenDashBoardMenuItem.cs:14） | `IMenuItemContribution` | `"dashboard.menu.open"` | `Language.DashBoardOpenWindowMenuTitle` | `Icons.DashBoard` | 10 | `MenuPlacement.File` | — |

两个 `IMainViewContribution` 实现各暴露一个 `public const string ViewId`（DashBoardOverviewMainView.cs:11、DashBoardRecentMainView.cs:11），`Id => ViewId`；`ViewId` 常量是 `DashBoardNavigationView` 发布 `OpenMainViewEvent` 时的负载来源。

### 3. `OpenDashBoardMenuItem` 的行为成员

```csharp
public OpenDashBoardMenuItem(IWindowManager windowManager)   // 构造注入
public ICommand Command { get; }                             // new DelegateCommand(windowManager.ShowWindow<DashBoardWindow>)
```
（OpenDashBoardMenuItem.cs:16-19、31）`Command` 执行即经 Abstractions 的泛型扩展 `ShowWindow<TWindow>` 重开启动台窗口。

### 4. `DashBoardWindowViewModel : ObservableObject`（ViewModels/Windows/DashBoardWindowViewModel.cs:12）

启动台进度窗的 ViewModel，由 Prism ViewModelLocator 按约定装配（不在容器中显式注册）。

**构造函数**：`DashBoardWindowViewModel(IEventAggregator eventAggregator)`（第 16 行）——订阅：
- `eventAggregator.GetEvent<StartupProgressEvent>().Subscribe(OnProgress, ThreadOption.UIThread, true)`（第 19 行）
- `eventAggregator.GetEvent<ModuleLoadFailedEvent>().Subscribe(OnModuleFailed, ThreadOption.UIThread, true)`（第 20 行）

**可观察属性**（`[ObservableProperty]` 源生成，绑定名为去下划线帕斯卡名）：

| 绑定属性 | 字段 | 初值 | 语义 |
|---|---|---|---|
| `PhaseText` | `_phaseText`（第 24 行） | `Language.SplashStartingText`（"正在启动…"） | 当前阶段文案 |
| `ModuleText` | `_moduleText`（第 30 行） | `string.Empty` | 当前模块名 + `i/N`；非 LoadingModules 阶段为空 |
| `IsFailed` | `_isFailed`（第 33 行） | `false` | 失败态：进度条停、错误区显示 |
| `ErrorMessage` | `_errorMessage`（第 36 行） | `string.Empty` | 失败模块的错误详情 |

**命令**（`[RelayCommand]` 源生成）：

| 命令属性 | 方法 | 行为 |
|---|---|---|
| `ContinueCommand` | `private void Continue()`（第 70 行） | `Publish StartupFailureActionEvent(StartupFailureAction.Continue)`——跳过失败模块继续加载 |
| `ExitCommand` | `private void Exit()`（第 80 行） | `Publish StartupFailureActionEvent(StartupFailureAction.Exit)`——终止应用 |

**私有方法**（事件回调与格式化，模块外不可见但为行为关键）：

- `private void OnProgress(StartupProgress progress)`（第 38 行）：`IsFailed=false`；`PhaseText` 按 `progress.Phase` 三值映射到 `Language.SplashPhaseCoreServices/SplashPhaseLoadingModules/SplashPhaseReady`，未知阶段 `_ => PhaseText` 保持原值；`ModuleText` 仅当 `Phase == StartupPhase.LoadingModules` 时取 `FormatModuleText(...)`，否则清空。
- `private void OnModuleFailed(ModuleLoadFailure failure)`（第 53 行）：`IsFailed=true`、`PhaseText=Language.SplashPhaseFailed`、`ModuleText=FormatModuleText(failure.ModuleName, failure.ModuleIndex, failure.ModuleCount)`、`ErrorMessage=failure.ErrorMessage`。
- `private static string FormatModuleText(string? moduleName, int index, int count)`（第 61 行）：返回 `$"{moduleName}（{index}/{count}）"`（**全角括号**）。

> 上游调查备注：Core/Framework 深读验证期间曾以 `SetProgress` 指称本类的进度回调方法。在 main @ 04cfd02 及全部 git 历史中（`git log -S SetProgress` 无结果），该类**从未存在**名为 `SetProgress` 的成员；真实的进度回调方法名是 `OnProgress`，失败回调是 `OnModuleFailed`。

### 5. 视图类型（五个公开类，均为 `partial`，无行为成员）

| 类 | 基类 | 文件 | 构造 |
|---|---|---|---|
| `DashBoardWindow` | `Avalonia.Controls.Window` | Views/Windows/DashBoardWindow.axaml(.cs) | 无参，`InitializeComponent()` |
| `DashBoardNavigationView` | `UserControl` | Views/DashBoardNavigationView.axaml(.cs) | **双构造**：无参转发 `IoC.Provider.Resolve<IEventAggregator>()`（XAML loader 用）；`DashBoardNavigationView(IEventAggregator)`（容器用） |
| `DashBoardOverviewView` | `UserControl` | Views/DashBoardOverviewView.axaml(.cs) | 无参 |
| `DashBoardRecentView` | `UserControl` | Views/DashBoardRecentView.axaml(.cs) | 无参 |
| `DashBoardTasksView` | `UserControl` | Views/DashBoardTasksView.axaml(.cs) | 无参 |

`DashBoardNavigationView` 的两个私有事件处理器（不是公开 API，但决定行为）：
- `private void OpenOverview(object? sender, RoutedEventArgs e)`（.axaml.cs:28）→ `Publish OpenMainViewEvent(DashBoardOverviewMainView.ViewId)`
- `private void OpenRecent(object? sender, RoutedEventArgs e)`（.axaml.cs:33）→ `Publish OpenMainViewEvent(DashBoardRecentMainView.ViewId)`

## 调用方式与生命周期

**模块没有供业务代码调用的主动 API**；全部交互是"被调用"：

1. 宿主把模块加进目录：`moduleCatalog.AddModule<DashBoardModule>()`（Modules/Workstation/WorkstationApplication.cs:16）。
2. 启动序列在模块加载前显示启动台：`Container.Resolve<DashBoardWindow>()`（WorkstationApplication.cs:53，经 `IWindowManager.ShowWindow`）；ViewModel 由 `prism:ViewModelLocator.AutoWireViewModel="True"`（DashBoardWindow.axaml:3）按约定装配，构造时完成事件订阅。
3. 启动序列逐模块发布 `StartupProgressEvent`/`ModuleLoadFailedEvent`，ViewModel 回调更新属性；用户点"继续/退出"时 ViewModel 发布 `StartupFailureActionEvent`。
4. 模块加载时 Prism 调 `RegisterTypes` 注册贡献；shell 收集渲染。
5. 工作区阶段的用户交互：SideBar 按钮发布 `OpenMainViewEvent`；文件菜单项执行 `ShowWindow<DashBoardWindow>`。

**消费事件的发布方**（反向依赖）：`StartupProgressEvent`/`ModuleLoadFailedEvent` 由 Core/Framework 的 `FrameworkApplication.RunStartupSequenceAsync` 发布；`StartupFailureActionEvent` 由同一处订阅等待（`WaitForFailureActionAsync`）。`OpenMainViewEvent` 由 shell（MainWindowViewModel）订阅。详见 docs/analysis/Core/Framework/ 与 docs/analysis/Core/Models/ 文档。

## 对外公开的数据结构

本模块**不定义**任何 DTO/record/枚举；对外数据完全由以下两类承载：

- 贡献类的属性（上表），字符串 Id 与 `Type` 引用；
- 事件负载（定义在 Core/Models/Events，本模块只消费）：`StartupProgress{Phase, ModuleName, ModuleIndex, ModuleCount}`、`ModuleLoadFailure{ModuleName, ModuleIndex, ModuleCount, ErrorMessage}`、`StartupFailureAction{Continue, Exit}`、`OpenMainViewEvent` 负载为主视图 `Id` 字符串（本模块发出的是 `"dashboard.overview"`/`"dashboard.recent"`）。
