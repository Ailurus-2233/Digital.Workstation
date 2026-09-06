# Models — 术语表

| 术语 | 定义 | 首次出现/代码位置 |
|---|---|---|
| **PubSubEvent&lt;T&gt;** | Prism 事件聚合器的事件基类（`Prism.Events` 命名空间），提供 `Publish`/`Subscribe`/`Unsubscribe`。本模块的 5 个事件类全部是无成员体的空子类，只为贡献类型身份 | 经 obj/Debug/Models.GlobalUsings.g.cs 第 6 行 global using 引入；5 个事件文件第 7 行 |
| **IEventAggregator** | Prism 发布/订阅中枢；`GetEvent<TEvent>()` 按类型身份返回事件单例，发布方与订阅方借此对接。本模块定义的就是其上流转的事件类型 | 消费方代码（FrameworkApplication.cs:68、MainWindowViewModel.cs:32 等）；本模块注释未直接出现 |
| **启动台（Splash）** | 启动期间显示的进度窗口，即 DashBoard 模块的 `DashBoardWindow`；订阅 `StartupProgressEvent`/`ModuleLoadFailedEvent` 显示进度，发布 `StartupFailureActionEvent` 回报用户决策 | ModuleLoadFailedEvent.cs、StartupFailureActionEvent.cs 注释；实现在 Modules/DashBoard |
| **启动序列** | `FrameworkApplication.OnFrameworkInitializationCompleted` 中的三阶段流程：初始化核心服务 → 逐模块异步加载 → 就绪；本模块的启动三事件为它服务 | StartupProgressEvent.cs 注释；实现在 Core/Framework/FrameworkApplication.cs |
| **ADR-0004** | 架构决策记录编号：抑制 Prism 同步 InitializeModules，模块改由启动序列逐模块异步加载，进度/失败经事件呈现。**文档本体不在仓库中**（悬空引用） | StartupPhase.cs、StartupProgressEvent.cs、ModuleLoadFailedEvent.cs 注释；README 第 47-51 行 |
| **StartupPhase / 启动阶段** | 启动序列的阶段划分：`CoreServices`（核心服务初始化）→ `LoadingModules`（逐模块加载）→ `Ready`（就绪）。注意：Launcher 引导（Avalonia 启动前）刻意不在其中 | Events/StartupPhase.cs:6 |
| **i/N 进度** | 逐模块加载时的"第 i 个/共 N 个"计数，`ModuleIndex`（从 1 开始）/`ModuleCount` 承载 | StartupProgress.cs、ModuleLoadFailure.cs 参数注释 |
| **MainContent** | 主窗口中央内容区，当前承载一个主视图（View）；`OpenMainViewEvent` 到达后整体替换 | OpenMainViewEvent.cs 注释；实现在 MainWindowViewModel（`MainContent` 属性） |
| **主视图 Id / ViewId** | `IMainViewContribution.Id` 的字符串值，是 `OpenMainViewEvent` 的负载；发布方用主视图类的静态 `ViewId`（如 `DashBoardOverviewMainView.ViewId`），与贡献注册的 `Id` 必须精确匹配 | OpenMainViewEvent.cs 注释；契约定义在 Core/Abstractions/Shell/IMainViewContribution.cs |
| **SideBar / AuxiliaryPanel / BottomPanel** | 工作区三块可显隐翻转的区域（侧边栏/辅助面板/底部面板），`TogglePanelTarget` 的三成员。注意 `SideBar` 在此既是"面板"枚举成员也是导航交互发生的区域，语境不同 | Events/TogglePanelTarget.cs:6 |
| **ActivityBar** | 与 SideBar 并列的导航条；其导航切换**不**发布 `OpenMainViewEvent`、MainContent 保持不变——这是与 SideBar 交互的关键行为区分 | OpenMainViewEvent.cs 注释 |
| **TogglePanelContribution** | shell 预置的视图菜单项（`Modules/Workstation/Shell/TogglePanelContribution.cs`），每个 `TogglePanelTarget` 注册一个，点击发布 `TogglePanelVisibilityEvent`；与快捷键共用 `MainWindowViewModel.TogglePanel` 状态转换 | TogglePanelVisibilityEvent.cs 注释 |
| **StartupFailureAction / 用户决策** | 模块加载失败后的二选一：`Continue`（跳过失败模块继续）/`Exit`（终止应用） | Events/StartupFailureAction.cs:6 |
| **ModuleLoadFailure / 失败负载** | 模块加载失败事件携带的详情 record；注意只保留 `ex.Message` 字符串，不含异常对象/堆栈 | Events/ModuleLoadFailure.cs:10 |
| **DigitalWorkstation.Core.Models** | 程序集名/根命名空间（由 `Build/Base.props` 的 `_InCore` 分支生成）；源码实际命名空间为 `DigitalWorkstation.Core.Models.Events` | Build/Base.props 第 39-42 行；各源码文件第 1 行 |
