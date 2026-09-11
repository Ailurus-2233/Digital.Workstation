# Framework — 模块简述

## 模块做什么

Core/Framework 是 Digital.Workstation 桌面应用的**应用框架层**，位于抽象层（Abstractions/Common/Models/UIPackage）之上、业务模块（Modules/*）之下，提供六块能力：

1. **应用引导与启动序列**：抽象基类 `FrameworkApplication<TWindow>`（`FrameworkApplication.cs:17`）继承 Prism.DryIoc 的 `PrismApplication`，装载主题、注册框架服务、执行"启动台 → 逐模块异步加载 → 显示主窗口"的三阶段启动序列（ADR-0004）。
2. **窗口管理实现**：`FrameworkWindowManager`（`WindowManager/FrameworkWindowManager.cs:12`）实现 Abstractions 定义的 `IWindowManager` 与 `IMainWindowManager`，维护"窗口类型 → 窗口实例"映射，负责窗口的显示/对话/隐藏/关闭与主窗口登记。
3. **Shell 布局状态机**：不可变 record `ShellLayoutState`（`Layout/ShellLayoutState.cs:9`）+ 四个区域状态 record（`SideBarState`、`AuxiliaryPanelState`、`BottomPanelState`、`MainContentState`），以 reducer 风格的纯转换方法描述工作区五区域（ActivityBar/SideBar/MainContent/AuxiliaryPanel/BottomPanel）的布局状态流转，含跨 Bar 拖拽迁移转换 `MoveTab`（ADR-0002；ActivityBar 顶部段顺序以 `ActivityBarItems` 进入状态）。
4. **Shell 贡献收集与菜单/工具视图/命令建树注册**：`ShellContributionCollector`（`Contributions/ShellContributionCollector.cs:9`）从 DI 容器收集各模块注册的 shell 贡献（工具视图、主视图、菜单项、状态栏项、命令），`GetToolViews()`/`GetStatusBarItems()` 按 `Order` 排序后交给 shell 渲染（工具视图的三处 Bar 分派由消费方按 `Placement`/`AllowMove` 决定，ADR-0002）；`GetCommands()`（:41，ADR-0005）按 `Order`/标题排序并做 Id 冲突去重（保留先注册者）；菜单贡献不过滤不排序，交给纯函数 `MenuTreeBuilder`（`Menus/MenuTreeBuilder.cs:13`）按路径/分组模型建树（ADR-0001）。模块侧菜单经 `MenuRegistration.RegisterMenus` 扩展（`Menus/MenuRegistration.cs:14`）以 attribute 扫描方式注册：菜单类注册为 singleton，每个合法 `MenuItemAttribute` 方法生成一个 `IMenuItemContribution` 注册进容器；命令经 `CommandRegistration.RegisterCommands` 扩展（`Commands/CommandRegistration.cs:13`，ADR-0005）同构扫描：免类级 attribute，任何类的合法 `CommandAttribute` 方法生成一个 `ICommandContribution` 注册进容器；工具视图同构：模块经 `ToolViewRegistration.RegisterToolViews` 扩展（`Contributions/ToolViewRegistration.cs:14`，ADR-0002）扫描 `[ToolView]` View 类，每个合法类注册 View 类型本身并生成一个 `ToolViewContribution` 元数据 singleton。
5. **窗口基类与基础布局**：抽象基类 `FrameworkWindow`（`Windows/FrameworkWindow.cs:22`，继承 UrsaWindow）+ `FrameworkWindowTheme`（`Windows/FrameworkWindowTheme.cs:12`）内置 VS Code 式五区 shell 的布局模板（四档面板对齐）、共享部件模板与 shell 样式；`FrameworkWindow` 构造函数同时内置**标题栏菜单栏**（代码创建的 `Menu` + `MenuItemViewModel` 项模板，ADR-0001）与**命令面板**（`Windows/CommandPalette.cs:18`，ADR-0005：代码创建的顶部浮层叠在布局宿主之上，ItemsSource 宽松绑定 ViewModel 的 `Commands`，Ctrl+P KeyBinding 直接开关；`RegisterCommandGestures` 为带 Gesture 的命令生成窗口级 KeyBinding）；`PanelResizer`（`Layout/PanelResizer.cs:13`）提供声明式分隔条；`ToolViewButton`/`ToolViewBar`（`Layout/ToolViewButton.cs:14`、`Layout/ToolViewBar.cs:19`）提供工具视图拖拽手势与投放目标（ADR-0002，控件模式同 PanelResizer：只做手势与视觉，状态转换经命令交给 ViewModel）。Modules/Workstation 的 `MainWindow` 继承之，axaml 只保留应用级 chrome（标题、快捷键）。
6. **布局持久化**：`LayoutPersistence`（`Layout/LayoutPersistence.cs:12`，ADR-0002）负责 %AppData%/Digital.Workstation/layout.json 的读/防抖写/删，`ShellLayoutDto` 族（`Layout/ShellLayoutDto.cs:10`）是落盘专用 DTO record（独立于 `ShellLayoutState`）；机制在 Framework、接线在 shell 模块——由 `RegisterFrameworkServices` 注册单例（`FrameworkApplication.cs:156`），真实的捕获/恢复/重置接线在 Modules/Workstation 的 `MainWindowViewModel`。

## 核心设计逻辑

- **ADR-0004 启动序列**：Prism 默认在框架初始化阶段同步 `InitializeModules()` 并把 MainWindow 直接设为桌面生命周期主窗口。本框架用三个空覆盖切断默认行为——`OnInitialized()`（`FrameworkApplication.cs:45`）与 `InitializeModules()`（`FrameworkApplication.cs:52`）置空阻止提前显示主窗口与同步加载模块，`OnFrameworkInitializationCompleted()`（`FrameworkApplication.cs:36`）不调用 base（base 会把尚未完成模块加载的 MainWindow 直接设为桌面生命周期主窗口），改为 fire-and-forget 调用私有 `RunStartupSequenceAsync()`（`FrameworkApplication.cs:65`）。理由：模块加载可能慢/失败，需要启动台呈现进度并让用户对失败模块做"继续/退出"决策；权衡是放弃了 Prism 内建的同步模块加载路径，自行用 `IModuleCatalog`/`IModuleManager` 逐模块 `await Task.Run(() => moduleManager.LoadModule(name))`（第 89 行）把加载移出 UI 线程。
- **事件驱动的进度/失败协议**：启动序列与启动台之间不直接引用，全部走 Models 模块的 `IEventAggregator` 事件：`StartupProgressEvent`（进度）、`ModuleLoadFailedEvent`（失败）、`StartupFailureActionEvent`（用户决策回传）。`WaitForFailureActionAsync`（`FrameworkApplication.cs:118`）把 PubSub 事件当一次性 RPC 用（订阅 → `TaskCompletionSource` await → 退订）。理由：Framework 不依赖任何具体启动台窗口类型，子类经抽象方法 `CreateSplashWindow()`（第 59 行）注入启动台，解耦框架与具体模块。
- **不可变布局状态（reducer 模式）**：`ShellLayoutState` 是 `sealed record`，所有转换方法（`SelectActivity`、`ToggleSideBar`、`Resize` 等）返回新实例，非法操作（面板收起时激活 tab）返回等值状态（`return this`，见 `ShellLayoutState.cs:80、93`）。理由：布局状态单一来源、可单测（这正是 UnitTest/Framework 唯一测试对象的由来）、UI 只绑定状态不做决策；代价是每次转换产生新对象，但状态极小（五个嵌套 record），开销可忽略。
- **拖拽控件只做手势与视觉，语义全在状态机**（ADR-0002）：`ToolViewButton`（拖拽源）与 `ToolViewBar`（投放目标）仿 `PanelResizer` 模式——控件不感知 ViewModel 类型，`DragTabId`/`CanDrag`/`TargetBar`/`Orientation`/`MoveCommand` 全部宽松绑定；落点包装为 `ToolViewMove` 记录经命令交给 shell ViewModel，真正的迁移/重排/显隐/激活语义集中在 `ShellLayoutState.MoveTab` 纯函数（跨 Bar 激活目标、源面板拖空收起、活动项被拖走回退到前一项、同 Bar 纯重排含索引修正、钉住项天然拒绝——钉住项不在任何 Bar 列表中）。落点视觉是 `ToolViewBar` ControlTheme 模板里的 `PART_InsertionLine` 占位线（2px 蓝线），由控件按指针位置移动，不改现有 tab 样式。`ToolViewDragSession` 静态信号在 `DoDragDropAsync` 期间置 `IsActive`，shell 借此临时显露隐藏面板作为投放区（「向隐藏面板拖入则自动显示」由此成为可能）。
- **一个窗口管理器实例注册两个接口**：`RegisterFrameworkServices`（`FrameworkApplication.cs:143`）中 `new FrameworkWindowManager()` 一次，`RegisterSingleton<IMainWindowManager>(() => windowManager)` 与 `RegisterSingleton<IWindowManager>(() => windowManager)` 共享同一实例（第 148-150 行）。理由：主窗口操作与普通窗口操作共享 `_windowMap` 与 `_mainWindow` 状态，拆成两个实例会出现状态分裂。
- **固定 Dark 主题**：`Initialize()`（第 22-30 行）硬编码 `RequestedThemeVariant = ThemeVariant.Dark`，注释说明"当前设计目标为 VS Code Dark+ 单一色调，未做亮色适配"。
- **ViewModel 约定式定位**：`ConfigureViewModelLocator()`（第 209 行）把 `Views` 命名空间替换为 `ViewModels`，`Window`/`Page` 后缀补 `ViewModel`、`View` 后缀补 `Model`，免注册。理由：统一约定消灭样板注册代码；代价是命名/目录偏离约定时定位静默失败（返回 null）。
- **布局模板整体切换（四档面板对齐）**：`FrameworkWindow.PanelAlignment` 的每个枚举值对应 `FrameworkWindowTheme` 里一份**静态**布局模板（`WindowLayoutLeft/Right/Center/Justify`），切换即整体替换 `ContentTemplate`（`FrameworkWindow.cs:94-107`），不在一份模板上做动态调整。理由：四档差异只在 BottomPanel 及其分隔条的 `Grid.Column`/`ColumnSpan` 和侧栏的 `Grid.RowSpan`（非两端对齐时侧栏通高到底，不留空挡；ActivityBar 恒通高），静态模板直观且零运行时布局逻辑；代价是四份模板结构大量重复，修改五区结构要同步四份。配套的两个解耦决策：① Framework 不引用具体 ViewModel 类型，布局模板全部走宽松反射绑定（`{Binding ResizePanelCommand}` 等），ViewModel 契约靠约定；② 主题不走 x:Class code-behind，改走 `StyleInclude` + 构造时强制 `Loaded`（`FrameworkWindowTheme.cs:22`）——Avalonia.Generators 源生成器在本项目不产出 InitializeComponent（最小探针复现失败，Workstation 项目正常），StyleInclude 是 Semi/Ursa 主题同款机制，可绕过该问题。
- **菜单栏内置于窗口基类**：`FrameworkWindow` 构造函数把菜单栏作为标题栏 `LeftContent` 内置（`FrameworkWindow.cs:47-52`：代码创建的 `Menu`，`Classes=chrome-menu`，`ItemsSource` 宽松绑定 ViewModel 的 `MenuBarItems`），菜单项模板经 `DataTemplates.Add(new FuncDataTemplate<MenuItemViewModel>(...))` 在**窗口 DataTemplates** 代码注册（:53）——放窗口级是为了子菜单任意深度都能经模板查找递归复用同一模板（无 x:Key 的 DataTemplate 不能放 `Styles.Resources`，AVLN3000）；配套样式（紧凑行高、Popup 偏移、ItemsSource/Command 样式绑定、图标前景色）在 `FrameworkWindowTheme.axaml:611-628`。理由（ADR-0001 后续）：菜单栏是每个 FrameworkWindow 子类都需要的 chrome，放基类后子类 axaml 零菜单样板；代价是 ViewModel 契约（`MenuBarItems` 集合、`MenuItemViewModel` 形状）仍是约定而非类型约束。
- **命令面板自包含、VM 零交互逻辑**（ADR-0005）：`CommandPalette`（`Windows/CommandPalette.cs:18`）把控件模式推到菜单栏同款位置——搜索框 + 列表 + 子串过滤（不区分大小写、匹配本地化标题）+ ↑↓/Enter/Esc 导航 + MRU 内存置顶全部内聚在控件内，数据源经 `ItemsSource` 宽松绑定 ViewModel 的 `Commands` 集合（同 `MenuBarItems` 惯例）；控件寿命 = 窗口寿命 = 应用寿命，内存 MRU 因此成立（重启即清，持久化留作后续）。理由：过滤文本、选中索引、MRU 都是面板私有瞬时 UI 状态，放进已承载全部布局状态的 shell ViewModel 只会继续撑大它。浮层以 `Panel` 叠在布局宿主之上由构造函数代码创建，**不动四份静态布局模板**。手势 KeyBinding 分层同 `LayoutPersistence`：机制是 `FrameworkWindow.RegisterCommandGestures`（`KeyGesture.Parse` 失败记日志跳过），接线在 shell 模块收集命令后调用一次。
- **菜单建树是纯函数、扫描只发生一次**：菜单管线分三段且职责互不渗透——`MenuRegistration.RegisterMenus`（`Menus/MenuRegistration.cs:20`）在模块 `RegisterTypes` 时扫**调用方传入的程序集**一次（不做全局扫描），菜单类注册 singleton、每个合法方法注册一个 `IMenuItemContribution` 工厂；`ShellContributionCollector.GetMenuItems()`（`Contributions/ShellContributionCollector.cs:31`）只从容器收集、不过滤不排序；`MenuTreeBuilder.Build`（`Menus/MenuTreeBuilder.cs:18`）是纯函数，输入扁平贡献列表输出排好序插好分隔线的 `MenuTreeSubmenu` 树，标题在建树时经 `Language.Get` 一次性解析。理由（ADR-0001）：路径/分组模型下菜单位次由多处声明合并而来，排序/分组/分隔线规则集中在一个无状态纯函数里最易验证；扫描与实例化各只发生一次（注册期扫描、建树期经容器解析 singleton），运行期零反射开销；代价是 `Title` 在注册/建树时解析，切换 UI 区域性需重建菜单树。工具视图管线与之同构（ADR-0002）：`ToolViewRegistration.RegisterToolViews`（`Contributions/ToolViewRegistration.cs:20`）同样在注册期扫调用方程序集一次，扫描 `[ToolView]` View 类、标题经 `Language.Get` 解析后落成 `ToolViewContribution` 元数据 singleton；`GetToolViews()` 只收集排序，三处 Bar 与钉住区的分派不在收集器而在消费方（MainWindowViewModel 按 `Placement`/`AllowMove` 分派）。
- **持久化 DTO 独立于布局状态机，读写皆容错**：`ShellLayoutDto` 族（`Layout/ShellLayoutDto.cs`）不复用 `ShellLayoutState`——状态机的 record 管流转语义（且含 `Tabs` 等运行期数据），落盘格式需要 `Version` 版本字段（`CurrentVersion`，不识别即整份丢弃）与独立的演进自由，两者由消费方（MainWindowViewModel 的 `CaptureLayout`/`RestoreLayout`）单向转换。配套三条机制设计：① 写防抖——`ScheduleSave`（`LayoutPersistence.cs:73`）用 `System.Threading.Timer` 把 500ms 内的连续布局变更合并为最后一次落盘，拖拽分隔条这类高频事件不会刷盘；② 读容错——`Load`（:37）对文件缺失/内容为空/版本不识别/反序列化失败一律返回 null，调用方静默按默认布局启动；③ `Delete`（:86）先在锁内作废 pending 防抖保存（清 `_pending`、停 Timer）再删文件，否则在途回调会把刚删的 layout.json 重建。全部失败路径只记 `Logger.Warning` 不打断应用；`Flush` 是 Timer 回调，异常无人处理会拖垮进程，故必须就地全捕获（:118 注释）。

## 状态流转

```
Avalonia 启动
  → FrameworkApplication.Initialize()            [主题：Dark + WorkstationTheme + VSCodePalette.ApplyTo(Resources)]
  → Prism 容器构建
  → RegisterTypes()                              [FrameworkApplication.cs:175]
      → RegisterFrameworkServices(): IoC.Initialize(registry, Container)；
        注册 FrameworkWindowManager 双接口单例、ShellContributionCollector 单例、LayoutPersistence 单例
      → ResolveFrameworkServices(): 解析 IEventAggregator、IMainWindowManager 存入字段
      → RegisterCustomService()（子类钩子）
  → CreateShell(): Container.Resolve<TWindow>()   [主窗口实例由容器创建]
  → OnFrameworkInitializationCompleted()
      → RunStartupSequenceAsync()                 [异步，fire-and-forget]
          阶段 1 CoreServices: _windowManager.HandleMainWindow()（登记主窗口）
                              → ShowWindow(CreateSplashWindow())（显示启动台）
                              → Publish StartupProgress(CoreServices, null, 0, 0)
                              → IModuleCatalog.Initialize()
          阶段 2 LoadingModules: 逐模块 Publish StartupProgress(LoadingModules, name, i+1, total)
                              → await Task.Run(LoadModule)
                              → 失败：Logger.Error + Publish ModuleLoadFailedEvent
                                     → WaitForFailureActionAsync()
                                       Continue → 跳过该模块继续；Exit → Shutdown()
          阶段 3 Ready: Publish StartupProgress(Ready, null, total, total)
                     → ShowMainWindow(): lifetime.MainWindow = window
                                       → _windowManager.ShowMainWindow()
                                       → _windowManager.CloseWindowsExceptMain()（关掉启动台）
          序列自身异常：Logger.Fatal + Shutdown()
```

副作用集中点：`IoC.Initialize`（一次性全局容器引用）、`FrameworkWindowManager._windowMap`（窗口注册表）、`ApplicationLifetime.MainWindow` 赋值、事件发布。`ShellLayoutState` 一侧无任何副作用——纯数据转换，由消费方（Modules/Workstation 的 `MainWindowViewModel._state`）持有当前实例并整体替换。

## 常见修改场景

1. **要调整某区域的默认尺寸或 clamp 区间**：改对应区域 record 的常量/默认值——`Layout/SideBarState.cs`（`MinWidth=120`、`MaxWidth=480`、`Width=240`）、`Layout/AuxiliaryPanelState.cs`（`Width=280`）、`Layout/BottomPanelState.cs`（`MinHeight=80`、`MaxHeight=480`、`Height=160`）；clamp 逻辑在 `ShellLayoutState.Resize`（`Layout/ShellLayoutState.cs:244`）。同步更新 `UnitTest/Framework/ShellLayoutStateResizeTests.cs` 中断言边界的用例。
2. **要加启动阶段或改失败处理策略**：核心逻辑在 `FrameworkApplication.RunStartupSequenceAsync`（`FrameworkApplication.cs:65`）；新增阶段需同步扩展 Models 模块的 `StartupPhase` 枚举并在启动台 ViewModel 中处理；改失败策略看 `WaitForFailureActionAsync`（第 118 行）与 catch 块（第 91-101 行）。
3. **要新增一种 shell 贡献类型**（如工具栏项）：在 Abstractions 定义新贡献类型 → 在 `ShellContributionCollector`（`Contributions/ShellContributionCollector.cs`）加一个 `Get*` 收集方法（仿照 `GetToolViews`/`GetStatusBarItems` 的 Resolve→OrderBy→ToArray 模式；菜单的 `GetMenuItems` 是例外——无过滤无排序，建树由 `MenuTreeBuilder` 负责；命令的 `GetCommands` 多一步 Id 冲突去重）→ 消费方在 Modules/Workstation。注意：**新增一个工具视图或一个命令不属于此场景**——工具视图只需在 View 类上标 `[ToolView]`、命令只需在方法上标 `[Command]`（ADR-0005），并保证所在模块 `RegisterTypes` 调了对应 `RegisterToolViews`/`RegisterCommands`，收集器与 shell 侧零改动。
4. **要改窗口显示语义**（如允许同类型多实例窗口）：改 `FrameworkWindowManager.InitializeWindow`（`WindowManager/FrameworkWindowManager.cs:45`）的 `_windowMap.TryAdd` 拒绝重复注册逻辑，注意 `CloseWindow`/`HideWindow` 按类型索引的前提会随之失效。
5. **要支持亮色主题**：改 `FrameworkApplication.Initialize`（第 24-28 行）的硬编码 Dark 与 `VSCodePalette.ApplyTo` 的写死色值（色值在 UIPackage 模块）。
6. **要改 View/ViewModel 命名约定**：改 `ConfigureViewModelLocator`（`FrameworkApplication.cs:209`）的 `SetDefaultViewTypeToViewModelTypeResolver` 委托。
7. **要新增/修改布局档位**（面板对齐）：三处同步改——`Layout/PanelAlignment.cs` 枚举加/改档；`Windows/FrameworkWindowTheme.axaml` 加一份 `WindowLayout*` 静态布局模板（仿现有四份，差异点在 BottomPanel 及其分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`）并同步四份模板的公共结构；`FrameworkWindow.UpdateLayoutTemplate`（`Windows/FrameworkWindow.cs:96-102`）的枚举→资源键映射。注意 Center 是 switch 兜底分支（`_ => "WindowLayoutCenter"`），新档必须显式加分支；消费侧（Modules/Workstation 的 `Menus/ViewAlignmentMenus.cs`）还要为新档加一个 `[MenuItem]` 方法。
8. **要改 layout.json 落盘格式**：三处同步改——`Layout/ShellLayoutDto.cs` 的 DTO 字段/默认值；`ShellLayoutDto.CurrentVersion` 升版本（旧文件版本不识别被 `Load` 整体丢弃、回默认布局，刻意不做迁移逻辑）；消费方 `MainWindowViewModel` 的 `CaptureLayout`（写入端，`Modules/Workstation/MainWindowViewModel.cs:504`）与 `RestoreLayout`/`LoadToolViews`（恢复端，:220、:178）。`LayoutPersistence` 本身无需改动——它只对 `ShellLayoutDto` 整体序列化/反序列化。
9. **要改命令面板行为**（过滤规则、MRU、键盘导航、空态）：全部在 `Windows/CommandPalette.cs`（ADR-0005）——交互逻辑自包含在控件内；外观（宽度/位置/颜色）在 `FrameworkWindowTheme.axaml:630-662` 的 `windows|CommandPalette` 样式族。要改数据源排序/去重才去 `ShellContributionCollector.GetCommands`（:41）；要给命令加快捷键改模块侧 `[Command(Gesture = "...")]`，接线（shell 调 `RegisterCommandGestures`）已在 `MainWindow.axaml.cs` 的 `OnOpened`。
10. **要改拖拽语义**（迁移/重排/显隐联动/回退规则）：只改 `ShellLayoutState.MoveTab`（`Layout/ShellLayoutState.cs:109`）——语义全在这个纯函数里；控件侧（`ToolViewButton`/`ToolViewBar`）只管手势与落点视觉，不要往里加逻辑。要改落点视觉（占位线样式/插入序号计算）才去 `ToolViewBar` 与其 ControlTheme（`FrameworkWindowTheme.axaml:492`）。
