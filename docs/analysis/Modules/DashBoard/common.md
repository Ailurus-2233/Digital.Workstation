# DashBoard — 模块简述

## 模块做什么

DashBoard（程序集 `DigitalWorkstation.DashBoard`，`DashBoard.csproj` 目标 `net10.0`）是 Digital.Workstation 的**启动台模块**：`DashBoardWindow`（Views/Windows/DashBoardWindow.axaml）在应用启动序列期间显示核心服务初始化与逐模块加载进度（阶段名 + 当前模块名 + `i/N`）；某模块加载失败时显示错误详情，并提供"继续（跳过该模块）/退出"两个决策按钮，把用户选择经事件发布回启动序列。另向 shell 状态栏贡献一个"启动台"条目（`DashBoardStatusBarItem`，仅标题+图标）。原"Shell 贡献通路验证（tracer bullet）"职责的演示贡献（ActivityBar"启动台"导航视图、概览/最近项目两个 MainContent 主视图、BottomPanel"任务"tab）已删除。

模块由 `Modules/Workstation` 宿主的 `WorkstationApplication.ConfigureModuleCatalog`（WorkstationApplication.cs:18 `moduleCatalog.AddModule<DashBoardModule>()`）注册进模块目录。

## 核心设计逻辑

- **启动台窗口不由模块自己打开**：`DashBoardModule.OnInitialized`（DashBoardModule.cs:16-19）是**有意的空方法**，注释写明「启动台窗口由 shell 启动序列在模块加载前显示（ADR-0004），模块自身不再开窗」。原因：启动台必须在模块加载开始前就可见（用来显示模块加载进度本身），此时模块系统尚未初始化本模块，所以由启动序列直接 `Container.Resolve<DashBoardWindow>()`（WorkstationApplication.cs:43）显示。推论：`DashBoardWindow` 与其 ViewModel 不能依赖任何"模块已初始化"的状态。
- **启动台与启动序列之间零直接引用**：进度与失败全部经 `IEventAggregator` 事件解耦——`DashBoardWindowViewModel` 构造函数（ViewModels/Windows/DashBoardWindowViewModel.cs:16-21）订阅 `StartupProgressEvent` → `OnProgress`、`ModuleLoadFailedEvent` → `OnModuleFailed`（均 `ThreadOption.UIThread, keepSubscriberReferenceAlive: true`）；用户决策经 `[RelayCommand] Continue()`/`Exit()`（第 70、79 行）发布 `StartupFailureActionEvent` 回传。理由：Framework 不依赖具体启动台窗口类型（见 Framework 文档 common.md"事件驱动的进度/失败协议"），DashBoard 是事件的订阅/发布方。
- **贡献只剩状态栏项**：原演示性贡献（ActivityBar"启动台"导航视图、概览/最近项目主视图、BottomPanel"任务"tab）已删除；唯一保留的 shell 贡献是 `DashBoardStatusBarItem`（`IStatusBarItemContribution` 纯属性类，`Order=20` 刻意排在 shell 预置"就绪"(10) 之后，类注释明示）。`RegisterTypes` 仍保留一行 `RegisterToolViews(Assembly)`（ADR-0002）——当前程序集无 `[ToolView]` 标注类，扫描注册为空，保留以覆盖将来新增；工具视图机制为 `[ToolView]` attribute 标在 View 类上由 Framework 扫描生成 `ToolViewContribution` 元数据并注册 View，主视图/状态栏仍是只有属性无方法的接口贡献类。
- **ViewModel 绑定靠约定**：`DashBoardWindow.axaml` 第 3 行 `prism:ViewModelLocator.AutoWireViewModel="True"`，Prism 按 `Views.Windows.DashBoardWindow` ↔ `ViewModels.Windows.DashBoardWindowViewModel` 的命名/目录约定自动装配 DataContext，窗口与 ViewModel 代码中无任何互相引用。
- **依赖面收窄到 Core**：`DashBoard.csproj`（第 17-20 行）只引用 Core 下的四个项目——`Abstractions`（`IStatusBarItemContribution` 贡献契约于 `Abstractions/Contributions/`）、`Framework`（`RegisterToolViews` 扩展于 `Framework/Contributions/`）、`Resource`（`Language`）、`UIPackage`（`Icons`）——**不引用 shell 宿主 Workstation**。模块对 shell 的集成全靠 Core/Abstractions 的贡献机制（状态栏实现 `IStatusBarItemContribution` 接口；工具视图走 View 类标 `[ToolView]`，当前无实例）加 `IEventAggregator` 事件，因此模块编译期与 shell 实现完全解耦。

## 状态流转

### 启动台链（启动期间）

```
FrameworkApplication.RunStartupSequenceAsync（Core/Framework）
  → Publish StartupProgressEvent(StartupProgress{Phase, ModuleName, ModuleIndex, ModuleCount})
  → DashBoardWindowViewModel.OnProgress（UI 线程，DashBoardWindowViewModel.cs:38）
      → IsFailed = false
      → PhaseText = Phase switch { CoreServices→Language.SplashPhaseCoreServices,
                                    LoadingModules→Language.SplashPhaseLoadingModules,
                                    Ready→Language.SplashPhaseReady, _→保持原值 }
      → ModuleText = Phase==LoadingModules ? FormatModuleText(name, i, N) : string.Empty
  → 绑定刷新 DashBoardWindow.axaml 的 TextBlock{PhaseText}/{ModuleText}、ProgressBar.IsIndeterminate={Binding !IsFailed}

模块失败：Publish ModuleLoadFailedEvent(ModuleLoadFailure{ModuleName, ModuleIndex, ModuleCount, ErrorMessage})
  → DashBoardWindowViewModel.OnModuleFailed（第 53 行）
      → IsFailed=true（进度条停、错误 Border 显示）, PhaseText=Language.SplashPhaseFailed,
        ModuleText=FormatModuleText(...), ErrorMessage=failure.ErrorMessage
  → 用户点击"继续"/"退出"按钮（绑定 ContinueCommand/ExitCommand）
  → Publish StartupFailureActionEvent(StartupFailureAction.Continue|Exit)
  → 启动序列 WaitForFailureActionAsync 收到决策，跳过该模块继续或终止应用
```

### 贡献链（进入工作区后）

```
DashBoardModule.RegisterTypes（DashBoardModule.cs:8-14）
  → `RegisterToolViews(typeof(DashBoardModule).Assembly)`（第 12 行，ADR-0002）扫描程序集内 `[ToolView]` 类——当前无，注册为空；
    另注册 1 个 `IStatusBarItemContribution` 单例（第 13 行）
  → ShellContributionCollector（Core/Framework）收集：状态栏项按 Order 排序
  → shell 渲染：状态栏出现"启动台"条目（位于"就绪"之后）
```

副作用与状态修改：ViewModel 只修改自身四个可观察属性；`StartupFailureActionEvent` 的发布是仅有的对外副作用；模块不持有任何可变共享状态。

## 常见修改场景

1. **要加一个新的 shell 工具视图（如面板 tab，ADR-0002）**：不用新建贡献类——新建 `UserControl` View 并在类上标 `[ToolView("dashboard.xxx", "标题资源键", Icon = Icons.Xxx, Default = ToolViewPlacement.BottomPanel, Order = n)]`（模块内当前无实例；机制见 ADR-0002）；`RegisterTypes` 的 `RegisterToolViews` 行已保留，扫描时自动生成 `ToolViewContribution` 元数据并把 View 注册进容器，无需加行。主视图/状态栏贡献仍走接口：实现 `IMainViewContribution`/`IStatusBarItemContribution` 并在 `RegisterTypes` 加一行 `RegisterSingleton`（现存实例：`DashBoardStatusBarItem`）。标题与图标分别在 Core/Resource 的 `Language` 与 Core/UIPackage 的 `Icons` 中新增。
2. **要改启动台的显示内容/行为**：进度文案映射在 `DashBoardWindowViewModel.OnProgress`（DashBoardWindowViewModel.cs:41-47）的 switch 与 `FormatModuleText`（第 61 行，格式 `$"{moduleName}（{index}/{count}）"`，全角括号）；失败呈现逻辑在 `OnModuleFailed`（第 53 行）；布局在 `Views/Windows/DashBoardWindow.axaml`（错误区是第 19-31 行的 `Border`，`IsVisible="{Binding IsFailed}"`）。新增绑定属性用 `[ObservableProperty]`，新增按钮动作用 `[RelayCommand]` 私有方法（命令属性名 = 方法名 + "Command"）。
3. **要让启动台窗口在启动后也能再次打开**：当前没有通路（原"文件菜单打开启动台"项与同名命令面板的菜单/命令贡献已删除）。加回方式：菜单——新建 `[MenuGroup]` 菜单类加 `[MenuItem]` 方法调 `IWindowManager.ShowWindow<DashBoardWindow>()`，并在 `RegisterTypes` 补一行 `RegisterMenus(Assembly)`；命令——方法标 `[Command]` 并补 `RegisterCommands(Assembly)` 行（参照 Modules/Workstation 的 `ViewCommands`）。
4. **要改模块注册的内容**：全部集中在 `DashBoardModule.RegisterTypes`（DashBoardModule.cs:8-14）。注意不要在 `OnInitialized` 里加开窗逻辑——启动台由启动序列负责（ADR-0004，见 pitfalls.md）。
5. **要新增一个模块命令（出现在命令面板）**：不用走接口——在任何类的方法上标 `[Command("标题资源键", Order = n, Icon = …, Gesture = …)]`（`Icon` 取 `Icons` 常量、`Gesture` 可空，仅支持无参 `void`/`Task`，参照 Modules/Workstation `Commands/ViewCommands.cs`）；本模块当前未调 `RegisterCommands`，需先在 `RegisterTypes` 补一行扫描（命令宿主类与贡献工厂由扫描自动注册）。命令与菜单是两套独立声明（ADR-0005），想让同一动作同时出现在菜单栏需另加菜单类与 `RegisterMenus` 行。标题键在 Core/Resource 的 `Language` 加资源键同步本地化。
