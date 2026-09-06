# DashBoard — 模块简述

## 模块做什么

DashBoard（程序集 `DigitalWorkstation.DashBoard`，`DashBoard.csproj` 目标 `net10.0`）是 Digital.Workstation 的**启动台模块**，同时承担两个职责：

1. **启动台进度窗**：`DashBoardWindow`（Views/Windows/DashBoardWindow.axaml）在应用启动序列期间显示核心服务初始化与逐模块加载进度（阶段名 + 当前模块名 + `i/N`）；某模块加载失败时显示错误详情，并提供"继续（跳过该模块）/退出"两个决策按钮，把用户选择经事件发布回启动序列。
2. **Shell 贡献通路验证（tracer bullet）**：模块向 shell 的五个扩展点各贡献一个条目——ActivityBar 导航项（`DashBoardNavigationItem`）、两个 MainContent 主视图（`DashBoardOverviewMainView`/`DashBoardRecentMainView`）、BottomPanel 面板 tab"任务"（`DashBoardTasksPanelTab`）、文件菜单"打开启动台"项（`OpenDashBoardMenuItem`）、状态栏项（`DashBoardStatusBarItem`）——以此验证"模块 → 贡献接口 → shell 渲染"的完整通路。README 第 24 行把这两点并列为模块职责。

模块由 `Modules/Workstation` 宿主的 `WorkstationApplication.ConfigureModuleCatalog`（WorkstationApplication.cs:16 `moduleCatalog.AddModule<DashBoardModule>()`）注册进模块目录。

## 核心设计逻辑

- **启动台窗口不由模块自己打开**：`DashBoardModule.OnInitialized`（DashBoardModule.cs:22-25）是**有意的空方法**，注释写明「启动台窗口由 shell 启动序列在模块加载前显示（ADR-0004），模块自身不再开窗」。原因：启动台必须在模块加载开始前就可见（用来显示模块加载进度本身），此时模块系统尚未初始化本模块，所以由启动序列直接 `Container.Resolve<DashBoardWindow>()`（WorkstationApplication.cs:53）显示。推论：`DashBoardWindow` 与其 ViewModel 不能依赖任何"模块已初始化"的状态。
- **启动台与启动序列之间零直接引用**：进度与失败全部经 `IEventAggregator` 事件解耦——`DashBoardWindowViewModel` 构造函数（ViewModels/Windows/DashBoardWindowViewModel.cs:16-21）订阅 `StartupProgressEvent` → `OnProgress`、`ModuleLoadFailedEvent` → `OnModuleFailed`（均 `ThreadOption.UIThread, keepSubscriberReferenceAlive: true`）；用户决策经 `[RelayCommand] Continue()`/`Exit()`（第 70、79 行）发布 `StartupFailureActionEvent` 回传。理由：Framework 不依赖具体启动台窗口类型（见 Framework 文档 common.md"事件驱动的进度/失败协议"），DashBoard 是事件的订阅/发布方。
- **贡献是纯声明**：五个贡献类只有属性（`Id`/`Title`/`IconPath`/`Order`/定位枚举/`ContentViewType` 或 `ViewType`），无方法（`OpenDashBoardMenuItem` 例外，持有一个 `Command`）。视图以 `Type` 而非实例交出，实例化延迟到 shell 侧经容器解析（支持 DI）。`Order` 值刻意插进 shell 预置项之间——面板 tab `Order=15` 介于预置"输出"(10) 与"日志"(20) 之间、菜单项 `Order=10` 小于预置"退出"(100)、状态栏 `Order=20` 排在预置"就绪"(10) 之后——用排序结果验证统一排序逻辑（各文件类注释明示此意图）。
- **ViewModel 绑定靠约定**：`DashBoardWindow.axaml` 第 3 行 `prism:ViewModelLocator.AutoWireViewModel="True"`，Prism 按 `Views.Windows.DashBoardWindow` ↔ `ViewModels.Windows.DashBoardWindowViewModel` 的命名/目录约定自动装配 DataContext，窗口与 ViewModel 代码中无任何互相引用。
- **依赖面收窄到 Core**：`DashBoard.csproj`（第 20-23 行）只引用 Core 下的四个项目——`Abstractions`（贡献契约与 `IWindowManager`）、`Framework`、`Resource`（`Language`）、`UIPackage`（`Icons`）——**不引用 shell 宿主 Workstation**。模块对 shell 的集成全靠实现 `Core/Abstractions/Shell` 的 `I*Contribution` 接口（`IMainViewContribution`、`IPanelTabContribution` 等）加 `IEventAggregator` 事件（`OpenMainViewEvent` 等），因此模块编译期与 shell 实现完全解耦。
- **导航视图的双构造函数**：`DashBoardNavigationView`（Views/DashBoardNavigationView.axaml.cs:18-26）同时提供无参构造（转发到 `IoC.Provider.Resolve<IEventAggregator>()`，注释说明"XAML runtime loader 需要无参构造"）和注入构造——服务定位器是为 XAML 加载器妥协，容器创建时走注入构造。

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
DashBoardModule.RegisterTypes（DashBoardModule.cs:8-20）
  → 容器注册 6 个 I*Contribution 单例 + 4 个视图类型
  → ShellContributionCollector（Core/Framework）收集、按 Placement/Menu/Panel 分组、按 Order 排序
  → shell 渲染：ActivityBar 出现"启动台"项、BottomPanel 出现"任务"tab、文件菜单出现"打开启动台"、状态栏出现条目
用户点击 ActivityBar"启动台" → shell 显示 SideBar 内容 DashBoardNavigationView
  → 点击"概览"/"最近项目"按钮（Click=OpenOverview/OpenRecent，DashBoardNavigationView.axaml:31,34）
  → Publish OpenMainViewEvent(DashBoardOverviewMainView.ViewId | DashBoardRecentMainView.ViewId)
    （DashBoardNavigationView.axaml.cs:28-36，负载为字符串 "dashboard.overview"/"dashboard.recent"）
  → shell（MainWindowViewModel）解析对应 IMainViewContribution.ViewType 并整体替换 MainContent
用户点击文件菜单"打开启动台" → OpenDashBoardMenuItem.Command（DelegateCommand）
  → IWindowManager.ShowWindow<DashBoardWindow>()（OpenDashBoardMenuItem.cs:18）重新显示启动台窗口
```

副作用与状态修改：ViewModel 只修改自身四个可观察属性；`OpenMainViewEvent`/`StartupFailureActionEvent` 的发布是仅有的对外副作用；模块不持有任何可变共享状态。

## 常见修改场景

1. **要加一个新的 shell 贡献（如第二个面板 tab）**：新建实现 `IPanelTabContribution` 的类（参照 `DashBoardTasksPanelTab.cs`），选好 `Id`（模块名前缀 `dashboard.*`）、`Order`、定位枚举与 `ContentViewType`；在 `DashBoardModule.RegisterTypes`（DashBoardModule.cs:10-15 区块）加一行 `RegisterSingleton<IPanelTabContribution, 新类>()`；再建对应视图 `UserControl` 并 `containerRegistry.Register<新视图>()`。标题与图标分别在 Core/Resource 的 `Language` 与 Core/UIPackage 的 `Icons` 中新增。
2. **要改启动台的显示内容/行为**：进度文案映射在 `DashBoardWindowViewModel.OnProgress`（DashBoardWindowViewModel.cs:41-47）的 switch 与 `FormatModuleText`（第 61 行，格式 `$"{moduleName}（{index}/{count}）"`，全角括号）；失败呈现逻辑在 `OnModuleFailed`（第 53 行）；布局在 `Views/Windows/DashBoardWindow.axaml`（错误区是第 19-31 行的 `Border`，`IsVisible="{Binding IsFailed}"`）。新增绑定属性用 `[ObservableProperty]`，新增按钮动作用 `[RelayCommand]` 私有方法（命令属性名 = 方法名 + "Command"）。
3. **要改 SideBar 条目与主视图跳转**：按钮在 `Views/DashBoardNavigationView.axaml` 第 31-36 行（`Classes="sidebar-entry"` 样式定义在第 11-28 行）；点击处理 `OpenOverview`/`OpenRecent` 在 `DashBoardNavigationView.axaml.cs:28-36`，发布 `OpenMainViewEvent` 的负载必须是目标 `IMainViewContribution` 的 `ViewId` 常量（`DashBoardOverviewMainView.ViewId`/`DashBoardRecentMainView.ViewId`，DashBoardOverviewMainView.cs:11 / DashBoardRecentMainView.cs:11）。新增主视图：加 `IMainViewContribution` 实现 + 视图 + `RegisterTypes` 两行注册 + 一个按钮。
4. **要让启动台窗口在启动后也能再次打开**：已经有——文件菜单"打开启动台"（`OpenDashBoardMenuItem`）。改其菜单位置/排序改 `Menu`/`Order` 属性；改打开行为改构造函数中 `DelegateCommand` 包装的方法组（OpenDashBoardMenuItem.cs:16-19）。
5. **要改模块注册的内容**：全部集中在 `DashBoardModule.RegisterTypes`（DashBoardModule.cs:8-20）。注意不要在 `OnInitialized` 里加开窗逻辑——启动台由启动序列负责（ADR-0004，见 pitfalls.md）。
