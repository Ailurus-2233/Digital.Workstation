# DashBoard — 对外接口与调用方式

模块命名空间：`DigitalWorkstation.DashBoard`（根）、`DigitalWorkstation.DashBoard.Views.Windows`、`DigitalWorkstation.DashBoard.ViewModels.Windows`（`Views/` 根不再持有视图类——四个占位视图已删除）。

## 公开 API 面

### 1. `DashBoardModule : IModule`（DashBoardModule.cs:7）

Prism 模块入口，被模块目录反射调用，**不被业务代码直接调用**。

| 成员 | 签名 | 说明 |
|---|---|---|
| `RegisterTypes` | `void RegisterTypes(IContainerRegistry containerRegistry)` | `RegisterToolViews` 扫描（当前程序集无 `[ToolView]` 标注类，注册为空）+ 1 个 `IStatusBarItemContribution` 单例（见下"注册清单"） |
| `OnInitialized` | `void OnInitialized(IContainerProvider containerProvider)` | **空实现**（DashBoardModule.cs:18 注释：启动台窗口由 shell 启动序列在模块加载前显示（ADR-0004），模块自身不再开窗） |

注册清单（DashBoardModule.cs:12-13）：
- `RegisterToolViews(typeof(DashBoardModule).Assembly)`（第 12 行，Core/Framework `DigitalWorkstation.Core.Framework.Contributions` 扩展，ADR-0002）——扫描程序集内 `[ToolView]` 类：**当前程序集无标注类**（原 `DashBoardNavigationView`/`DashBoardTasksView` 已删除），扫描注册为空，保留该行以覆盖将来新增；机制为对每个合法的（可实例化 `Control`、程序集内 Id 不重复）View 执行 `Register(viewType)` 并注册一个 `ToolViewContribution` 元数据单例（`Title` 扫描时经 `Language.Get(TitleKey)` 解析），非法者记 `Logger.Warning` 跳过
- `RegisterSingleton<IStatusBarItemContribution, DashBoardStatusBarItem>()`（第 13 行）

注意：`DashBoardWindow` 与 `DashBoardWindowViewModel` **不在** `RegisterTypes` 中注册——`DashBoardWindow` 由启动序列在模块加载前经 `Container.Resolve<DashBoardWindow>()`（WorkstationApplication.cs:43）解析，Prism 容器对未注册的具体类型仍可构造解析（DryIoc 默认行为），ViewModel 由 ViewModelLocator 约定装配。

### 2. 贡献声明（供 shell 收集消费）

**接口贡献类（仅一个，只有属性）：**

| 类 | 实现接口 | `Id` | `Title` | `IconPath` | `Order` |
|---|---|---|---|---|---|
| `DashBoardStatusBarItem`（DashBoardStatusBarItem.cs:11） | `IStatusBarItemContribution` | `"dashboard.status"` | `Language.DashBoardNavigationTitle`（启动台标题资源） | `Icons.DashBoard` | 20 |

原五个演示贡献已删除：两个 `[ToolView]` 工具视图（`DashBoardNavigationView` `"dashboard"` / `DashBoardTasksView` `"dashboard.tasks"`，ADR-0002）与两个 `IMainViewContribution` 主视图（`"dashboard.overview"` / `"dashboard.recent"`，各暴露 `public const string ViewId` 供 `OpenMainViewEvent` 负载）——工具视图 attribute 机制与 `ViewId` 常量负载模式当前无本模块实例。

### 3. `DashBoardWindowViewModel : ObservableObject`（ViewModels/Windows/DashBoardWindowViewModel.cs:12）

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

### 4. 视图类型（一个公开类，`partial`，无行为成员）

| 类 | 基类 | 文件 | 构造 |
|---|---|---|---|
| `DashBoardWindow` | `Avalonia.Controls.Window` | Views/Windows/DashBoardWindow.axaml(.cs) | 无参，`InitializeComponent()` |

原四个占位视图（`DashBoardNavigationView`/`DashBoardOverviewView`/`DashBoardRecentView`/`DashBoardTasksView`，均 `UserControl`）已删除。

## 调用方式与生命周期

**模块没有供业务代码调用的主动 API**；全部交互是"被调用"：

1. 宿主把模块加进目录：`moduleCatalog.AddModule<DashBoardModule>()`（Modules/Workstation/WorkstationApplication.cs:17）。
2. 启动序列在模块加载前显示启动台：`Container.Resolve<DashBoardWindow>()`（WorkstationApplication.cs:40，`CreateSplashWindow` 重写）；ViewModel 由 `prism:ViewModelLocator.AutoWireViewModel="True"`（DashBoardWindow.axaml:3）按约定装配，构造时完成事件订阅。
3. 启动序列逐模块发布 `StartupProgressEvent`/`ModuleLoadFailedEvent`，ViewModel 回调更新属性；用户点"继续/退出"时 ViewModel 发布 `StartupFailureActionEvent`。
4. 模块加载时 Prism 调 `RegisterTypes` 注册贡献；shell 收集渲染。

**消费事件的发布方**（反向依赖）：`StartupProgressEvent`/`ModuleLoadFailedEvent` 由 Core/Framework 的 `FrameworkApplication.RunStartupSequenceAsync` 发布；`StartupFailureActionEvent` 由同一处订阅等待（`WaitForFailureActionAsync`）。`OpenMainViewEvent` 由 shell（MainWindowViewModel）订阅。详见 docs/analysis/Core/Framework/ 与 docs/analysis/Core/Models/ 文档。

## 对外公开的数据结构

本模块**不定义**任何 DTO/record/枚举；对外数据完全由以下两类承载：

- 接口贡献类的属性（上表），字符串 Id；
- 事件负载（定义在 Core/Models/Events，本模块只消费/回传）：`StartupProgress{Phase, ModuleName, ModuleIndex, ModuleCount}`、`ModuleLoadFailure{ModuleName, ModuleIndex, ModuleCount, ErrorMessage}`、`StartupFailureAction{Continue, Exit}`。
