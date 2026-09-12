# Models — 模块简述

## 职责

`Core/Models`（程序集 `DigitalWorkstation.Core.Models`，仅含命名空间 `DigitalWorkstation.Core.Models.Events`）是**跨模块共享的事件契约与负载 DTO 层**。它不实现任何行为，只定义 Prism 事件聚合器（`IEventAggregator`）上流转的强类型事件与负载：

- 启动序列三件套（ADR-0004）：`StartupProgressEvent`（启动进度）、`ModuleLoadFailedEvent`（模块加载失败）、`StartupFailureActionEvent`（用户对失败的"继续/退出"决策），配套负载 record `StartupProgress`/`ModuleLoadFailure` 与枚举 `StartupPhase`/`StartupFailureAction`；
- 工作区交互四件套：`OpenMainViewEvent`（SideBar 内交互或 shell"设置"导航按钮请求 MainContent 打开指定主视图，负载为主视图 Id 字符串，ADR-0006 决策 6）、`TogglePanelVisibilityEvent`（翻转指定面板可见性，负载为 `TogglePanelTarget` 枚举）、`ResetLayoutEvent`（请求重置布局（ADR-0002），无负载；视图菜单"重置布局"项发布，主窗口订阅后删除持久化配置并重建 State）、`SettingChangedEvent`（设置项变更广播，负载为 `SettingChanged(SettingId, NewValue)`，`SettingsService.Set` 发布，ADR-0006 决策 3）。

模块共 13 个源码文件：7 个空的事件子类（6 个 `PubSubEvent<T>` + 1 个无负载的非泛型 `PubSubEvent`）、3 个负载 record、3 个枚举，全部在 `Core/Models/Events/` 下。

## 核心设计逻辑

1. **契约独立成项目**：发布方（`Core/Framework` 的 `FrameworkApplication`）与订阅/发布方（`Modules/DashBoard`、`Modules/Workstation`）分属不同程序集。事件类型必须放在双方都引用的下游项目中，否则 shell 与模块无法在同一事件类型上对接。Prism 的 `EventAggregator.GetEvent<T>()` 按**类型身份**撮合发布/订阅，类型重复定义（哪怕同名同结构）不会互通，所以每个事件只能有唯一定义点——本模块就是这个定义点。
2. **空子类承载语义**：7 个事件类都是无成员体的声明（如 `Events/StartupProgressEvent.cs` 第 7 行 `public class StartupProgressEvent : PubSubEvent<StartupProgress>;`；`Events/ResetLayoutEvent.cs` 第 7 行继承非泛型 `PubSubEvent`，连负载类型都没有）。这是 Prism 的惯例：`PubSubEvent<T>` 已提供全部机制（`Publish`/`Subscribe`/`Unsubscribe`、线程选项），子类只贡献**类型身份**，事件的业务语义完全写在 XML 注释里。这避免了任何重复机制代码。
3. **负载用 record 而非 class**：`StartupProgress`（Events/StartupProgress.cs 第 10 行）、`ModuleLoadFailure`（Events/ModuleLoadFailure.cs 第 10 行）与 `SettingChanged`（Events/SettingChanged.cs 第 9 行）是位置 record——不可变、值相等、构造即定型，适合"一次性消息"语义；可空标注（`string? ModuleName`、`object? NewValue`）表达"仅 LoadingModules 阶段携带模块名""新值类型由设置项声明的 ValueType 决定"的约定。
4. **权衡——可空 + 约定而非类型区分阶段**：`StartupProgress` 用一个 record 通吃三个阶段，`ModuleName` 仅在 `StartupPhase.LoadingModules` 时非 null（注释明确约定，类型系统不强制）。代价是订阅方（如 `DashBoardWindowViewModel.OnProgress`）必须自己判阶段；收益是阶段切换不引入新类型，发布/订阅双方改动面最小。

## 状态流转

本模块无内部状态，状态全部在发布方/订阅方之间流动：

- **启动进度链**：`Core/Framework/FrameworkApplication.cs` 的启动序列发布 `StartupProgressEvent`——阶段 1 发布 `new StartupProgress(StartupPhase.CoreServices, null, 0, 0)`（第 78 行）；逐模块发布 `new StartupProgress(StartupPhase.LoadingModules, module.ModuleName, i + 1, total)`（第 89 行）；就绪发布 `new StartupProgress(StartupPhase.Ready, null, total, total)`（第 108 行）。订阅方 `Modules/DashBoard/ViewModels/Windows/DashBoardWindowViewModel.cs` 的 `OnProgress`（第 38 行）按 `StartupPhase` 切换阶段文案，仅 LoadingModules 阶段格式化模块 i/N 文案。
- **模块失败决策链**：启动序列捕获模块加载异常后发布 `ModuleLoadFailedEvent`（FrameworkApplication.cs 第 97-98 行，负载含模块名、1 起始序号、总数、`ex.Message`），随后 `WaitForFailureActionAsync`（第 121-129 行）一次性订阅 `StartupFailureActionEvent` 并 await；启动台（DashBoardWindowViewModel 第 72/81 行）的"继续/退出"命令发布决策，启动序列收到 `Continue` 跳过该模块继续，收到 `Exit` 调 `(ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown()`（第 101 行）终止应用。
- **主视图切换链**：shell 左下角"设置"导航按钮经 `Modules/Workstation/MainWindowViewModel.cs` 的 `OpenSettings`（第 303 行，`Publish` 在第 305 行，负载 `WellKnownViews.Settings`，ADR-0006 决策 6）发布 `OpenMainViewEvent`（原 DashBoard 导航视图"概览/最近项目"按钮的发布点已随演示视图删除）→ `MainWindowViewModel` 构造函数第 48 行订阅，经 `OpenMainView`（第 344 行）替换 `MainContent`。
- **面板显隐链**：`Modules/Workstation/Menus/ViewPanelMenus.cs` 的三个 `[MenuItem]` 方法（第 17/23/29 行）发布 `TogglePanelVisibilityEvent` → `MainWindowViewModel` 构造函数第 49 行订阅，`TogglePanel`（第 488-497 行）按 `TogglePanelTarget` 调 `State.ToggleSideBar()/ToggleAuxiliaryPanel()/ToggleBottomPanel()`；快捷键经 `Commands/ViewCommands.cs` 三命令的 `Gesture`（窗口级 KeyBinding 执行命令）发布同一事件汇入，面板收起按钮命令（`ToggleAuxiliaryPanel`/`ToggleBottomPanel`，第 401/410 行）直接调 `TogglePanel`，均走同一状态转换。
- **重置布局链**：`Modules/Workstation/Menus/ViewLayoutMenus.cs` 的 `ResetLayout` 方法（第 14-17 行，`[MenuItem("ResetLayoutTitle", Order = 100)]`，`Publish()` 在第 16 行）发布 `ResetLayoutEvent`（无负载）→ `MainWindowViewModel` 构造函数第 51 行订阅，`ResetLayout`（第 503-521 行）先 `_persistence.Delete()` 删除 layout.json，再清空四个集合与三个索引字典、重置 `State = ShellLayoutState.Initial` 与 `PanelAlignment = Center`，最后 `LoadToolViews(null)` 按 attribute 默认重建布局（视图实例缓存保留）。
- **设置变更链**（ADR-0006 决策 3）：`Core/Framework/Settings/SettingsService.cs` 的 `Set`（发布在第 128 行）更新内存并防抖落盘后发布 `SettingChangedEvent`，负载为 `SettingChanged(SettingId, NewValue)`（`NewValue` 为装箱后的设置值，类型由设置项声明的 `ValueType` 决定）；订阅方为设置页与需重启 UX 等消费方（工单 03 就位，当前源码尚无订阅调用点）。

## 常见修改场景

1. **要加一个新启动阶段**（如"预热缓存"）：改 `Events/StartupPhase.cs` 加枚举成员（XML 注释说明该阶段是否携带模块名）；改 `Core/Framework/FrameworkApplication.cs` 启动序列的发布点（参照第 78/89/108 行的三种发布形态）；改 `Modules/DashBoard/ViewModels/Windows/DashBoardWindowViewModel.cs` 的 `OnProgress`（第 41-47 行 switch）加阶段文案映射，并在 `Language` 资源加对应字符串。注意注释约束：`StartupPhase` 注释声明 Launcher 引导不在覆盖范围内，新阶段若发生在 Avalonia 启动前需重新评估该约定。
2. **要加一个新面板**（如 TopPanel）：改 `Events/TogglePanelTarget.cs` 加成员；`MainWindowViewModel.TogglePanel`（第 488-497 行）的 switch 会因"新枚举成员落入 `_` 分支"而**编译不报错但行为错**，必须加分支；新增对应菜单方法需仿照 `Modules/Workstation/Menus/ViewPanelMenus.cs` 的三个 `[MenuItem]` 方法（各带 Title/Icon/Order，`Publish` 对应 target），并在 `Core/Resource` 加标题资源键；同时需在 shell 侧 `ShellLayoutState`（不在本模块）加对应面板状态。
3. **要加一个新的跨模块共享事件**：在 `Core/Models/Events/` 新建文件，定义负载（record 或简单类型）+ 空 `PubSubEvent<T>` 子类，无负载则继承非泛型 `PubSubEvent`（照抄现有 7 个事件的形态，含说明"谁发布、谁订阅"的 XML 注释）；发布方与订阅方项目经 `Framework → Models` 的传递引用即可解析类型，无需新增 ProjectReference（除非消费方当前不引用 Framework）。
4. **要给启动进度加新字段**（如百分比）：改 `Events/StartupProgress.cs` 的位置参数列表，所有 `new StartupProgress(...)` 调用点（FrameworkApplication.cs 第 78/89/108 行）与所有读取点（DashBoardWindowViewModel.OnProgress/FormatModuleText）同步修改——record 位置参数没有默认值兜底，漏改即编译错误，这是 record 形态的安全网。
