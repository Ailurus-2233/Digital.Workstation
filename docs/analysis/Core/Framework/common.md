# Framework — 模块简述

## 模块做什么

Core/Framework 是 Digital.Workstation 桌面应用的**应用框架层**，位于抽象层（Abstractions/Common/Models/UIPackage）之上、业务模块（Modules/*）之下，提供七块能力：

1. **应用引导与启动序列**：抽象基类 `FrameworkApplication<TWindow>`（`FrameworkApplication.cs:20`）继承 Prism.DryIoc 的 `PrismApplication`，装载主题、注册框架服务、执行"启动台 → 逐模块异步加载 → 显示主窗口"的三阶段启动序列（[ADR-0004](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md)）。
2. **窗口管理实现**：`FrameworkWindowManager`（`WindowManager/FrameworkWindowManager.cs:12`）实现 Abstractions 定义的 `IWindowManager` 与 `IMainWindowManager`，维护"窗口类型 → 窗口实例"映射，负责窗口的显示/对话/隐藏/关闭与主窗口登记。
3. **Shell 布局状态机**：不可变 record `ShellLayoutState`（`Layout/ShellLayoutState.cs:9`）+ 四个区域状态 record（`SideBarState`、`AuxiliaryPanelState`、`BottomPanelState`、`MainContentState`），以 reducer 风格的纯转换方法描述工作区五区域（ActivityBar/SideBar/MainContent/AuxiliaryPanel/BottomPanel）的布局状态流转，含跨 Bar 拖拽迁移转换 `MoveTab`（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)；ActivityBar 顶部段顺序以 `ActivityBarItems` 进入状态）。
4. **Shell 贡献收集与菜单/工具视图/命令建树注册**：`ShellContributionCollector`（`Contributions/ShellContributionCollector.cs:12`）从 DI 容器收集各模块注册的 shell 贡献（工具视图、主视图、菜单项、状态栏项、命令、设置分组与设置项），`GetToolViews()`/`GetStatusBarItems()` 按 `Order` 排序后交给 shell 渲染（工具视图的三处 Bar 分派由消费方按 `Placement`/`AllowMove` 决定，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）；`GetCommands()`（:42，[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）按 `Order`/标题排序并做 Id 冲突去重（保留先注册者）；菜单贡献不过滤不排序，交给纯函数 `MenuTreeBuilder`（`Menus/MenuTreeBuilder.cs:13`）按路径/分组模型建树（[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）。模块侧菜单经 `MenuRegistration.RegisterMenus` 扩展（`Menus/MenuRegistration.cs:14`）以 attribute 扫描方式注册：菜单类注册为 singleton，每个合法 `MenuItemAttribute` 方法生成一个 `IMenuItemContribution` 注册进容器；命令经 `CommandRegistration.RegisterCommands` 扩展（`Commands/CommandRegistration.cs:13`，[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）同构扫描：免类级 attribute，任何类的合法 `CommandAttribute` 方法生成一个 `ICommandContribution` 注册进容器。
5. **窗口基类与基础布局**：抽象基类 `FrameworkWindow`（`Windows/FrameworkWindow.cs:22`，继承 UrsaWindow）+ `FrameworkWindowTheme`（`Windows/FrameworkWindowTheme.cs:12`）内置 VS Code 式五区 shell 的布局模板（四档面板对齐）、共享部件模板与 shell 样式；`FrameworkWindow` 构造函数同时内置**标题栏菜单栏**（代码创建的 `Menu` + `MenuItemViewModel` 项模板，[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）与**命令面板**（`Windows/CommandPalette.cs:18`，[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)：代码创建的顶部浮层叠在布局宿主之上，ItemsSource 宽松绑定 ViewModel 的 `Commands`，Ctrl+P KeyBinding 直接开关；`RegisterCommandGestures` 为带 Gesture 的命令生成窗口级 KeyBinding）；`PanelResizer`（`Layout/PanelResizer.cs:13`）提供声明式分隔条；`ToolViewButton`/`ToolViewBar`（`Layout/ToolViewButton.cs:14`、`Layout/ToolViewBar.cs:19`）提供工具视图拖拽手势与投放目标（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)，控件模式同 PanelResizer：只做手势与视觉，状态转换经命令交给 ViewModel）。Modules/Workstation 的 `MainWindow` 继承之，axaml 只保留应用级 chrome（标题、快捷键）。
6. **布局持久化**：`LayoutPersistence`（`Layout/LayoutPersistence.cs:12`，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）负责 %AppData%/Digital.Workstation/layout.json 的读/防抖写/删，`ShellLayoutDto` 族（`Layout/ShellLayoutDto.cs:10`）是落盘专用 DTO record（独立于 `ShellLayoutState`）；机制在 Framework、接线在 shell 模块——由 `RegisterFrameworkServices` 注册单例（`FrameworkApplication.cs:159`），真实的捕获/恢复/重置接线在 Modules/Workstation 的 `MainWindowViewModel`。
7. **设置注册与持久化**：`SettingRegistration.RegisterSettings` 扫调用方程序集一次，保存设置分组稳定 Id、显式资源来源与名称键，收集侧按 Id 合并（首个来源/名称生效、Order 取最小）。`SettingsService` 启动时 Load，读内存、未修改取声明默认值，写内存后防抖落盘并广播事件；它跟踪与启动值的偏离支撑重启标记。Framework 预置 `framework.general` 常规分组与语言项（zh-CN/en-US、需重启，LanguageSettingId 不变）；`ApplicationRestarter` 强制落盘后启动新进程并退出。

## 核心设计逻辑
- **[ADR-0004](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md) 启动序列**：Prism 默认在框架初始化阶段同步 `InitializeModules()` 并把 MainWindow 直接设为桌面生命周期主窗口。本框架用三个空覆盖切断默认行为——`OnInitialized()`（`FrameworkApplication.cs:48`）与 `InitializeModules()`（`FrameworkApplication.cs:55`）置空阻止提前显示主窗口与同步加载模块，`OnFrameworkInitializationCompleted()`（`FrameworkApplication.cs:39`）不调用 base（base 会把尚未完成模块加载的 MainWindow 直接设为桌面生命周期主窗口），改为 fire-and-forget 调用私有 `RunStartupSequenceAsync()`（`FrameworkApplication.cs:68`）。理由：模块加载可能慢/失败，需要启动台呈现进度并让用户对失败模块做"继续/退出"决策；权衡是放弃了 Prism 内建的同步模块加载路径，自行用 `IModuleCatalog`/`IModuleManager` 逐模块 `await Task.Run(() => moduleManager.LoadModule(name))`（第 92 行）把加载移出 UI 线程。
- **事件驱动的进度/失败协议**：启动序列与启动台之间不直接引用，全部走 Models 模块的 `IEventAggregator` 事件：`StartupProgressEvent`（进度）、`ModuleLoadFailedEvent`（失败）、`StartupFailureActionEvent`（用户决策回传）。`WaitForFailureActionAsync`（`FrameworkApplication.cs:121`）把 PubSub 事件当一次性 RPC 用（订阅 → `TaskCompletionSource` await → 退订）。理由：Framework 不依赖任何具体启动台窗口类型，子类经抽象方法 `CreateSplashWindow()`（第 62 行）注入启动台，解耦框架与具体模块。
- **不可变布局状态（reducer 模式）**：`ShellLayoutState` 是 `sealed record`，所有转换方法（`SelectActivity`、`ToggleSideBar`、`Resize` 等）返回新实例，非法操作（面板收起时激活 tab）返回等值状态（`return this`，见 `ShellLayoutState.cs:80、93`）。理由：布局状态单一来源、可单测（这正是 UnitTest/Framework 唯一测试对象的由来）、UI 只绑定状态不做决策；代价是每次转换产生新对象，但状态极小（五个嵌套 record），开销可忽略。
- **一个窗口管理器实例注册两个接口**：`RegisterFrameworkServices`（`FrameworkApplication.cs:146`）中 `new FrameworkWindowManager()` 一次，`RegisterSingleton<IMainWindowManager>(() => windowManager)` 与 `RegisterSingleton<IWindowManager>(() => windowManager)` 共享同一实例（第 151-153 行）。理由：主窗口操作与普通窗口操作共享 `_windowMap` 与 `_mainWindow` 状态，拆成两个实例会出现状态分裂。
- **固定 Dark 主题**：`Initialize()`（第 25-33 行）硬编码 `RequestedThemeVariant = ThemeVariant.Dark`，注释说明"当前设计目标为 VS Code Dark+ 单一色调，未做亮色适配"。
- **ViewModel 约定式定位**：`ConfigureViewModelLocator()`（第 237 行）把 `Views` 命名空间替换为 `ViewModels`，`Window`/`Page` 后缀补 `ViewModel`、`View` 后缀补 `Model`，免注册。理由：统一约定消灭样板注册代码；代价是命名/目录偏离约定时定位静默失败（返回 null）。
- **布局模板整体切换（四档面板对齐）**：`FrameworkWindow.PanelAlignment` 的每个枚举值对应 `FrameworkWindowTheme` 里一份**静态**布局模板（`WindowLayoutLeft/Right/Center/Justify`），切换即整体替换 `ContentTemplate`（`FrameworkWindow.cs:94-107`），不在一份模板上做动态调整。理由：四档差异只在 BottomPanel 及其分隔条的 `Grid.Column`/`ColumnSpan` 和侧栏的 `Grid.RowSpan`（非两端对齐时侧栏通高到底，不留空挡；ActivityBar 恒通高），静态模板直观且零运行时布局逻辑；代价是四份模板结构大量重复，修改五区结构要同步四份。配套的两个解耦决策：① Framework 不引用具体 ViewModel 类型，布局模板全部走宽松反射绑定（`{Binding ResizePanelCommand}` 等），ViewModel 契约靠约定；② 主题不走 x:Class code-behind，改走 `StyleInclude` + 构造时强制 `Loaded`（`FrameworkWindowTheme.cs:22`）——Avalonia.Generators 源生成器在本项目不产出 InitializeComponent（最小探针复现失败，Workstation 项目正常），StyleInclude 是 Semi/Ursa 主题同款机制，可绕过该问题。
- **菜单栏按平台呈现**：macOS 不创建标题栏内 Menu，以免与 traffic-light 按钮重叠；shell 收集后把同一菜单呈现树递归映射为 NativeMenu，附加到窗口后进入系统菜单栏。其他平台使用 `LeftContent` 的 `Menu.chrome-menu`。`ApplyLanguageSetting` 在加载设置并切换区域性之后以 `SharedResources.ProductName` 设置 Application.Name，保证 macOS 左侧应用菜单标题使用已存语言。
- **命令面板自包含、VM 零交互逻辑**（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）：`CommandPalette`（`Windows/CommandPalette.cs:18`）把控件模式推到菜单栏同款位置——搜索框 + 列表 + 子串过滤（不区分大小写、匹配本地化标题）+ ↑↓/Enter/Esc 导航 + MRU 内存置顶全部内聚在控件内，数据源经 `ItemsSource` 宽松绑定 ViewModel 的 `Commands` 集合（同 `MenuBarItems` 惯例）；控件寿命 = 窗口寿命 = 应用寿命，内存 MRU 因此成立（重启即清，持久化留作后续）。理由：过滤文本、选中索引、MRU 都是面板私有瞬时 UI 状态，放进已承载全部布局状态的 shell ViewModel 只会继续撑大它。浮层以 `Panel` 叠在布局宿主之上由构造函数代码创建，**不动四份静态布局模板**。手势 KeyBinding 分层同 `LayoutPersistence`：机制是 `FrameworkWindow.RegisterCommandGestures`（`KeyGesture.Parse` 失败记日志跳过），接线在 shell 模块收集命令后调用一次。
- **快捷键标签与声明分离**：`CommandPalette` 列表项通过私有 `FormatGesture` 调 `KeyGesture.Parse(...).ToString("p", null)`，使用 Avalonia 平台格式化显示快捷键（例如 `Ctrl+OemComma` → `Ctrl+,`）。不修改贡献的 `Gesture` 或窗口 KeyBinding；空声明保持为空，无法解析的声明保持原文，警告仍由快捷键注册路径记录。
- **菜单建树不查资源、扫描只发生一次**：`RegisterMenus` 扫调用方程序集，注册菜单类与贡献 singleton 工厂；首次收集贡献时分别按菜单项/菜单组自己的 ResourceType 解析 Title/PathTitle，引用式 MenuGroup 的 PathTitle 为 null。`GetMenuItems` 只收集，`MenuTreeBuilder.Build` 仅按稳定路径归并、排序与插分隔线。每个末端节点首个非 null 标题生效，隐式祖先/仅引用节点先显示 Id，后到声明可补标题；位次合并独立取最小。外部模块 `[MenuGroup("shell.file")]` 可挂接菜单而不导入 WorkstationResources。工具视图则在 `RegisterToolViews` 扫描时按显式来源解析标题生成元数据；命令在首次收集时解析。三者已解析文本随 singleton 存活，语言改变下次启动生效，重建树不重新翻译旧贡献。
- **持久化 DTO 独立于布局状态机，读写皆容错**：`ShellLayoutDto` 族（`Layout/ShellLayoutDto.cs`）不复用 `ShellLayoutState`——状态机的 record 管流转语义（且含 `Tabs` 等运行期数据），落盘格式需要 `Version` 版本字段（`CurrentVersion`，不识别即整份丢弃）与独立的演进自由，两者由消费方（MainWindowViewModel 的 `CaptureLayout`/`RestoreLayout`）单向转换。配套三条机制设计：① 写防抖——`ScheduleSave`（`LayoutPersistence.cs:73`）用 `System.Threading.Timer` 把 500ms 内的连续布局变更合并为最后一次落盘，拖拽分隔条这类高频事件不会刷盘；② 读容错——`Load`（:37）对文件缺失/内容为空/版本不识别/反序列化失败一律返回 null，调用方静默按默认布局启动；③ `Delete`（:86）先在锁内作废 pending 防抖保存（清 `_pending`、停 Timer）再删文件，否则在途回调会把刚删的 layout.json 重建。全部失败路径只记 `Logger.Warning` 不打断应用；`Flush` 是 Timer 回调，异常无人处理会拖垮进程，故必须就地全捕获（:118 注释）。
- **设置管线：扫描一次、读走内存、防抖落盘、事件广播**：`SettingsService.Load` 一次性读入 `_values` 并复制为 `_sessionStartValues`；Get 未修改时通过按 Id 惰性缓存的贡献取得声明默认值，缓存未命中重新枚举以接纳后到模块。Set 更新内存、500ms Timer 防抖保存、通过 JsonElement.DeepEquals 跟踪偏离启动值的项并广播 SettingChangedEvent；改回启动值会移除 pending。`FlushPending` 停 Timer 并同步写盘，消除立即重启读取旧配置的窗口；Timer 写盘全 catch 记 Warning。设置分组/设置项元数据不在扫描时解析名称，而由设置页首次构造时从各自 ResourceType 查找；隐式分组直接显示 Id。`ApplyLanguageSetting` 必须先于模块 RegisterTypes，同时设置当前与默认线程区域性，覆盖阶段 2 Task.Run；没有持久化语言时使用声明默认值 zh-CN，与 OS 语言无关。
- **资源归属**：Core/Resource 只承载共享产品名 `SharedResources` 和 `ResourceText.Get(Type, key)` 查询机制；Framework 私有文案放 `Resources/FrameworkResources.cs/.resx/.en-US.resx`（命令面板水印/空态、常规/语言设置与枚举名称）。模块私有资源随所属程序集，不回流全局键表。来源类型的全名与程序集定位资源，按 CurrentUICulture 查询、中性中文回退、缺键显示键名。


- **日志语言约束**：Framework 及其消费模块写入 `Logger` 的消息内容统一使用英文；类型名、Id、路径、异常类型等动态上下文保留原值。
## 状态流转

```
Avalonia 启动
  → FrameworkApplication.Initialize()            [主题：Dark + WorkstationTheme + VSCodePalette.ApplyTo(Resources)]
  → Prism 容器构建
  → RegisterTypes()                              [FrameworkApplication.cs:189]
      → RegisterFrameworkServices(): IoC.Initialize(registry, Container)；
        注册 FrameworkWindowManager 双接口单例、ShellContributionCollector 单例、LayoutPersistence 单例；
        显式构造 SettingsService → Load() 一次性读入 settings.json → 注册 ISettingsService 单例；
        RegisterSettings(Framework 程序集) 注册「常规/语言」设置项；
        ApplyLanguageSetting(): 按已存语言设 DefaultThreadCurrentCulture/UICulture 与 Current，
        再按该语言设置 Application.Name（macOS 最左侧应用菜单标题）；
        必须先于模块 RegisterTypes——工具视图扫描时、菜单/命令首次收集时按显式资源来源解析
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

## 常见修改场景

1. **要调整某区域的默认尺寸或 clamp 区间**：改对应区域 record 的常量/默认值——`Layout/SideBarState.cs`（`MinWidth=120`、`MaxWidth=480`、`Width=240`）、`Layout/AuxiliaryPanelState.cs`（`Width=280`）、`Layout/BottomPanelState.cs`（`MinHeight=80`、`MaxHeight=480`、`Height=160`）；clamp 逻辑在 `ShellLayoutState.Resize`（`Layout/ShellLayoutState.cs:244`）。同步更新 `UnitTest/Framework/ShellLayoutStateResizeTests.cs` 中断言边界的用例。
2. **要加启动阶段或改失败处理策略**：核心逻辑在 `FrameworkApplication.RunStartupSequenceAsync`（`FrameworkApplication.cs:68`）；新增阶段需同步扩展 Models 模块的 `StartupPhase` 枚举并在启动台 ViewModel 中处理；改失败策略看 `WaitForFailureActionAsync`（第 121 行）与 catch 块（第 94-104 行）。
3. **要新增一种 shell 贡献类型**（如工具栏项）：在 Abstractions 定义新贡献类型 → 在 `ShellContributionCollector`（`Contributions/ShellContributionCollector.cs`）加一个 `Get*` 收集方法（仿照 `GetToolViews`/`GetStatusBarItems` 的 Resolve→OrderBy→ToArray 模式——若贡献类型是**可实例化具体类**而非接口，必须像 `GetToolViews` 一样加 `Where` 过滤 DryIoc 零注册幽灵实例，见 pitfalls.md；菜单的 `GetMenuItems` 是例外——无过滤无排序，建树由 `MenuTreeBuilder` 负责；命令的 `GetCommands` 多一步 Id 冲突去重）→ 消费方在 Modules/Workstation。注意：**新贡献类型的契约（接口或 attribute）放 Core/Abstractions**，收集器只负责收集与排序。
4. **要改窗口显示语义**（如允许同类型多实例窗口）：改 `FrameworkWindowManager.InitializeWindow`（`WindowManager/FrameworkWindowManager.cs:45`）的 `_windowMap.TryAdd` 拒绝重复注册逻辑，注意 `CloseWindow`/`HideWindow` 按类型索引的前提会随之失效。
5. **要支持亮色主题**：改 `FrameworkApplication.Initialize`（第 27-31 行）的硬编码 Dark 与 `VSCodePalette.ApplyTo` 的写死色值（色值在 UIPackage 模块）。
6. **要改 View/ViewModel 命名约定**：改 `ConfigureViewModelLocator`（`FrameworkApplication.cs:237`）的 `SetDefaultViewTypeToViewModelTypeResolver` 委托。
7. **要新增/修改布局档位**（面板对齐）：三处同步改——`Layout/PanelAlignment.cs` 枚举加/改档；`Windows/FrameworkWindowTheme.axaml` 加一份 `WindowLayout*` 静态布局模板（仿现有四份，差异点在 BottomPanel 及其分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`）并同步四份模板的公共结构；`FrameworkWindow.UpdateLayoutTemplate`（`Windows/FrameworkWindow.cs:96-102`）的枚举→资源键映射。注意 Center 是 switch 兜底分支（`_ => "WindowLayoutCenter"`），新档必须显式加分支；消费侧（Modules/Workstation 的 `Menus/ViewAlignmentMenus.cs`）还要为新档加一个 `[MenuItem]` 方法。
8. **要改 layout.json 落盘格式**：三处同步改——`Layout/ShellLayoutDto.cs` 的 DTO 字段/默认值；`ShellLayoutDto.CurrentVersion` 升版本（旧文件版本不识别被 `Load` 整体丢弃、回默认布局，刻意不做迁移逻辑）；消费方 `MainWindowViewModel` 的 `CaptureLayout`（写入端，`Modules/Workstation/MainWindowViewModel.cs:504`）与 `RestoreLayout`/`LoadToolViews`（恢复端，:220、:178）。`LayoutPersistence` 本身无需改动——它只对 `ShellLayoutDto` 整体序列化/反序列化。
9. **要改命令面板行为**：交互在 `Windows/CommandPalette.cs`，样式在 `FrameworkWindowTheme.axaml`，水印与空态在 `Resources/FrameworkResources.*`；排序/去重改 `ShellContributionCollector.GetCommands`。模块命令用 `[Command(typeof(ModuleResources), titleKey, Gesture = "...")]` 声明快捷键，shell 收集后调用 `RegisterCommandGestures` 完成接线。
10. **要改拖拽语义**（迁移/重排/显隐联动/回退规则）：只改 `ShellLayoutState.MoveTab`（`Layout/ShellLayoutState.cs:109`）——语义全在这个纯函数里；控件侧（`ToolViewButton`/`ToolViewBar`）只管手势与落点视觉，不要往里加逻辑。要改落点视觉（占位线样式/插入序号计算）才去 `ToolViewBar` 与其 ControlTheme（`FrameworkWindowTheme.axaml:492`）。
