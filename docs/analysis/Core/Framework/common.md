# Framework — 模块简述

## 模块做什么

Core/Framework 是 Digital.Workstation 桌面应用的**应用框架层**，位于抽象层（Abstractions/Common/Models/UIPackage）之上、业务模块（Modules/*）之下，提供七块能力：

1. **应用引导与启动序列**：抽象基类 `FrameworkApplication<TWindow>`（`FrameworkApplication.cs:20`）继承 Prism.DryIoc 的 `PrismApplication`，装载主题、注册框架服务、执行"启动台 → 逐模块异步加载 → 显示主窗口"的三阶段启动序列（[ADR-0004](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md)）。
2. **窗口管理实现**：`FrameworkWindowManager`（`WindowManager/FrameworkWindowManager.cs:12`）实现 Abstractions 定义的 `IWindowManager` 与 `IMainWindowManager`，维护"窗口类型 → 窗口实例"映射，负责窗口的显示/对话/隐藏/关闭与主窗口登记。
3. **Shell 布局状态机**：不可变 record `ShellLayoutState`（`Layout/ShellLayoutState.cs:9`）+ 四个区域状态 record（`SideBarState`、`AuxiliaryPanelState`、`BottomPanelState`、`MainContentState`），以 reducer 风格的纯转换方法描述工作区五区域（ActivityBar/SideBar/MainContent/AuxiliaryPanel/BottomPanel）的布局状态流转，含跨 Bar 拖拽迁移转换 `MoveTab`（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)；ActivityBar 顶部段顺序以 `ActivityBarItems` 进入状态）。
4. **Shell 贡献登记与收集**：扫描器保留按模块程序集扫描的入口，统一经 RegisterShellContribution 登记带启动批次的工厂。ShellContributionCatalog 在每模块加载后预构造贡献，成功才发布；失败批次不可见。ShellContributionCollector 消费目录并排序；菜单建树规则保持在 MenuTreeBuilder，设置声明解释集中在 SettingCatalog。
5. **窗口基类与基础布局**：抽象基类 `FrameworkWindow`（`Windows/FrameworkWindow.cs:22`，继承 UrsaWindow）+ `FrameworkWindowTheme`（`Windows/FrameworkWindowTheme.cs:12`）内置 VS Code 式五区 shell 的布局模板（四档面板对齐）、共享部件模板与 shell 样式；`FrameworkWindow` 构造函数同时内置**标题栏菜单栏**（代码创建的 `Menu` + `MenuItemViewModel` 项模板，[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）与**命令面板**（`Windows/CommandPalette.cs:18`，[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)：代码创建的顶部浮层叠在布局宿主之上，ItemsSource 宽松绑定 ViewModel 的 `Commands`，Ctrl+P KeyBinding 直接开关；`RegisterCommandGestures` 为带 Gesture 的命令生成窗口级 KeyBinding）；`PanelResizer`（`Layout/PanelResizer.cs:13`）提供声明式分隔条；`ToolViewButton`/`ToolViewBar`（`Layout/ToolViewButton.cs:14`、`Layout/ToolViewBar.cs:19`）提供工具视图拖拽手势与投放目标（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)，控件模式同 PanelResizer：只做手势与视觉，状态转换经命令交给 ViewModel）。Modules/Workstation 的 `MainWindow` 继承之，axaml 只保留应用级 chrome（标题、快捷键）。
6. **布局持久化**：`LayoutPersistence` 保留 layout.json 的容错读取和布局格式校验，把防抖写入与重置删除交给 `Persistence/DebouncedJsonFile<T>`；`ConfigurationPersistence` 统一管理设置、布局两个文件的写入生命周期。机制在 Framework，状态与 DTO 的转换集中在 ShellLayoutConfiguration，布局提交/恢复/重置接线在 MainWindowViewModel。
7. **设置注册与持久化**：`SettingRegistration.RegisterSettings` 扫调用方程序集一次，保存设置分组稳定 Id、显式资源来源与名称键，收集侧按 Id 合并（首个来源/名称生效、Order 取最小）。`SettingsService` 启动时 Load，读内存、未修改取声明默认值，写内存后防抖落盘并广播事件；它跟踪与启动值的偏离支撑重启标记。Framework 预置 `framework.general` 常规分组与语言项（zh-CN/en-US、需重启，LanguageSettingId 不变）；`ApplicationRestarter` 强制落盘后启动新进程并退出。

## 核心设计逻辑
- **[ADR-0004](../../../adr/0004-startup-sequence.md) 启动序列**：保留三个覆盖以阻止 Prism 默认提前显示。模块按 CompleteListWithDependencies 的依赖顺序串行加载；每次 LoadModule 在后台执行，返回 UI 线程后准备本批贡献，全部成功才把模块加入可用集合。依赖不可用的模块进入相同失败决策，且不调用 LoadModule。宿主无批次贡献最后准备，PrepareShell 完成呈现后才发布 Ready 并显示主窗口。
- **事件驱动的进度/失败协议**：启动与启动台仍只经 IEventAggregator 交流。失败时先拒绝贡献批次，再订阅一次性决策，然后发布 ModuleLoadFailedEvent，避免同步回复丢失。Continue 只继续加载其余独立模块；Exit 正常退出。隔离覆盖 Shell 贡献，不撤销普通 DI 注册、事件订阅或模块自行产生的其他副作用。
- **不可变布局状态（reducer 模式）**：`ShellLayoutState` 是 `sealed record`，所有转换方法（`SelectActivity`、`ToggleSideBar`、`Resize` 等）返回新实例，非法操作（面板收起时激活 tab）返回等值状态（`return this`，见 `ShellLayoutState.cs:80、93`）。理由：布局状态单一来源、可单测（这正是 UnitTest/Framework 唯一测试对象的由来）、UI 只绑定状态不做决策；代价是每次转换产生新对象，但状态极小（五个嵌套 record），开销可忽略。
- **一个窗口管理器实例注册两个接口**：`RegisterFrameworkServices`（`FrameworkApplication.cs:146`）中 `new FrameworkWindowManager()` 一次，`RegisterSingleton<IMainWindowManager>(() => windowManager)` 与 `RegisterSingleton<IWindowManager>(() => windowManager)` 共享同一实例（第 151-153 行）。理由：主窗口操作与普通窗口操作共享 `_windowMap` 与 `_mainWindow` 状态，拆成两个实例会出现状态分裂。
- **固定 Dark 主题**：`Initialize()`（第 25-33 行）硬编码 `RequestedThemeVariant = ThemeVariant.Dark`，注释说明"当前设计目标为 VS Code Dark+ 单一色调，未做亮色适配"。
- **ViewModel 约定式定位**：`ConfigureViewModelLocator()`（第 237 行）把 `Views` 命名空间替换为 `ViewModels`，`Window`/`Page` 后缀补 `ViewModel`、`View` 后缀补 `Model`，免注册。理由：统一约定消灭样板注册代码；代价是命名/目录偏离约定时定位静默失败（返回 null）。
- **布局模板整体切换（四档面板对齐）**：`FrameworkWindow.PanelAlignment` 的每个枚举值对应 `FrameworkWindowTheme` 里一份**静态**布局模板（`WindowLayoutLeft/Right/Center/Justify`），切换即整体替换 `ContentTemplate`（`FrameworkWindow.cs:94-107`），不在一份模板上做动态调整。理由：四档差异只在 BottomPanel 及其分隔条的 `Grid.Column`/`ColumnSpan` 和侧栏的 `Grid.RowSpan`（非两端对齐时侧栏通高到底，不留空挡；ActivityBar 恒通高），静态模板直观且零运行时布局逻辑；代价是四份模板结构大量重复，修改五区结构要同步四份。配套的两个解耦决策：① Framework 不引用具体 ViewModel 类型，布局模板全部走宽松反射绑定（`{Binding ResizePanelCommand}` 等），ViewModel 契约靠约定；② 主题不走 x:Class code-behind，改走 `StyleInclude` + 构造时强制 `Loaded`（`FrameworkWindowTheme.cs:22`）——Avalonia.Generators 源生成器在本项目不产出 InitializeComponent（最小探针复现失败，Workstation 项目正常），StyleInclude 是 Semi/Ursa 主题同款机制，可绕过该问题。
- **菜单栏按平台呈现**：macOS 不创建标题栏内 Menu，以免与 traffic-light 按钮重叠；shell 收集后把同一菜单呈现树递归映射为 NativeMenu，附加到窗口后进入系统菜单栏。其他平台使用 `LeftContent` 的 `Menu.chrome-menu`。`ApplyLanguageSetting` 在加载设置并切换区域性之后以 `SharedResources.ProductName` 设置 Application.Name，保证 macOS 左侧应用菜单标题使用已存语言。
- **顶层菜单选屏**：`FrameworkWindowTheme.MenuPopupPlacement` 按菜单按钮锚定矩形中心选择屏幕，将矩形裁剪到该屏幕 Bounds 后以 BottomLeft/BottomRight 向下展开。屏幕像素坐标经当前 TopLevel 转回 DIP；保留主题偏移及 Avalonia 的边界翻转/滑动/缩放约束。不修改嵌套子菜单或 macOS NativeMenu。原因与回归入口见 pitfalls.md 的“最大化菜单跨屏”。
- **命令面板自包含、VM 零交互逻辑**（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）：`CommandPalette`（`Windows/CommandPalette.cs:18`）把控件模式推到菜单栏同款位置——搜索框 + 列表 + 子串过滤（不区分大小写、匹配本地化标题）+ ↑↓/Enter/Esc 导航 + MRU 内存置顶全部内聚在控件内，数据源经 `ItemsSource` 宽松绑定 ViewModel 的 `Commands` 集合（同 `MenuBarItems` 惯例）；控件寿命 = 窗口寿命 = 应用寿命，内存 MRU 因此成立（重启即清，持久化留作后续）。理由：过滤文本、选中索引、MRU 都是面板私有瞬时 UI 状态，放进已承载全部布局状态的 shell ViewModel 只会继续撑大它。浮层以 `Panel` 叠在布局宿主之上由构造函数代码创建，**不动四份静态布局模板**。手势 KeyBinding 分层同 `LayoutPersistence`：机制是 `FrameworkWindow.RegisterCommandGestures`（`KeyGesture.Parse` 失败记日志跳过），接线在 shell 模块收集命令后调用一次。
- **快捷键标签与声明分离**：`CommandPalette` 列表项通过私有 `FormatGesture` 调 `KeyGesture.Parse(...).ToString("p", null)`，使用 Avalonia 平台格式化显示快捷键（例如 `Ctrl+OemComma` → `Ctrl+,`）。不修改贡献的 `Gesture` 或窗口 KeyBinding；空声明保持为空，无法解析的声明保持原文，警告仍由快捷键注册路径记录。
- **扫描一次、启动准备时构造宿主**：RegisterMenus/RegisterCommands 仍只扫描指定程序集，宿主为 singleton；贡献工厂由启动准备解析一次，已构造实例在收集与建树时复用，构造异常归属对应模块。菜单稳定路径、首个标题与最小位次规则不变。工具视图注册仍只生成元数据，内容视图按需解析；设置声明保存资源类型与键，不提前翻译。
- **持久化 DTO 独立于布局状态机，写入时序集中管理**：`ShellLayoutDto` 继续承载 Version 和独立落盘格式，`Load` 对缺失/损坏/未知版本回落默认布局。设置与布局各持有一个 `DebouncedJsonFile<T>`，500ms 防抖；文件锁覆盖待写快照、序列化、临时文件提交与删除。`Delete` 等待在途写入，作废未写快照再删除，旧回调不能在重置后重建文件。写入先完成同目录临时文件并刷新，再替换目标；失败记录英文 Warning、保留旧文件与待写快照。普通退出由 `IControlledApplicationLifetime.Exit` 调用统一 owner 的 Dispose，先保存再释放 Timer；可取消的 Closing/ShutdownRequested 不提前释放。
- **设置管线：扫描一次、读走内存、防抖落盘、事件广播**：`SettingsService` 构造时接收 `ConfigurationPersistence`，Load 一次性读入 `_values` 与启动值快照；Get/Set 的声明查询统一走 SettingCatalog，不保留私有声明缓存；同 Id 首个可见声明生效，无关查询不会覆盖它。Set 在内存锁内更新值、向文件写入模块提交独立字典快照并维护重启标记，锁外广播事件。后台写入不回调读取设置内存，避免锁顺序反转。立即重启统一调用 `ConfigurationPersistence.FlushPending()` 保存设置和布局，任一保存失败时记 Error 并留在当前进程。语言仍在模块注册前应用，当前与默认线程区域性同步设置，不做热切换。
- **资源归属**：Core/Resource 只承载共享产品名 `SharedResources` 和 `ResourceText.Get(Type, key)` 查询机制；Framework 私有文案放 `Resources/FrameworkResources.cs/.resx/.en-US.resx`（命令面板水印/空态、常规/语言设置与枚举名称）。模块私有资源随所属程序集，不回流全局键表。来源类型的全名与程序集定位资源，按 CurrentUICulture 查询、中性中文回退、缺键显示键名。


- **日志语言约束**：Framework 及其消费模块写入 `Logger` 的消息内容统一使用英文；类型名、Id、路径、异常类型等动态上下文保留原值。
## 状态流转

```text
Framework 注册 ShellContributionCatalog、SettingCatalog、收集器和配置持久化 owner
  → 框架设置登记 → SettingsService.Load → 应用语言
  → 宿主登记贡献 → 创建 Shell（不收集）
  → 显示启动台 → 校验模块目录与依赖排序
  → 每模块 BeginBatch → 后台 LoadModule → UI 线程 Prepare
      成功：关闭登记 → 构造本批所有贡献 → Commit → 记为可用
      失败：Reject → 先订阅决策再发布失败 → Continue / Exit
  → PrepareUnowned（宿主基础贡献）→ PrepareShell（布局/菜单/手势呈现）
  → Ready → 显示工作区 → 关闭启动台
```

ContributionBatch 经 AsyncLocal 传播到模块加载任务。构造器可以读本批设置，其他上下文只能读已提交模块；批次失败后设置查询也不再可见。登记检查与容器插入、关闭批次使用同一把锁，关闭后拒绝迟到登记。模块应同步完成 RegisterTypes，不自行嵌套启动其他模块。

布局链：ShellLayoutConfiguration.Restore 过滤孤儿、恢复归属/活动项并 clamp 尺寸 → Workstation.ApplyLayout 准备目标内容、解除旧宿主、同步状态与呈现 → ShellLayoutConfiguration.Capture 直接从 State 有序 Id 生成 DTO → LayoutPersistence 防抖保存。重置保留 MainContent 的状态和实例，默认布局成功提交后删除配置。CardMargin、ContainerPadding 与列宽换算共同读取 ShellLayoutMetrics。

## 常见修改场景

1. **调整默认尺寸或 clamp**：修改对应区域 record 常量/默认值，检查 ShellLayoutState.Resize 与 ShellLayoutConfiguration.Restore 使用相同边界，再按 testing.md 本机启动验证拖动和恢复。
2. **要加启动阶段或改失败处理策略**：核心逻辑在 `FrameworkApplication.RunStartupSequenceAsync`（`FrameworkApplication.cs:68`）；新增阶段需同步扩展 Models 模块的 `StartupPhase` 枚举并在启动台 ViewModel 中处理；改失败策略看 `WaitForFailureActionAsync`（第 121 行）与 catch 块（第 94-104 行）。
3. **新增 Shell 贡献**：契约放 Abstractions；扫描器或模块通过 RegisterShellContribution<T>(factory) 登记，手写实现可用 RegisterShellContribution<T,TImplementation>()。收集器从 ShellContributionCatalog.Get<T>() 取已准备实例再排序，不能重新 Resolve<IEnumerable<T>>()，否则会绕过失败隔离且引入具体类型兜底实例。
4. **要改窗口显示语义**（如允许同类型多实例窗口）：改 `FrameworkWindowManager.InitializeWindow`（`WindowManager/FrameworkWindowManager.cs:45`）的 `_windowMap.TryAdd` 拒绝重复注册逻辑，注意 `CloseWindow`/`HideWindow` 按类型索引的前提会随之失效。
5. **要支持亮色主题**：改 `FrameworkApplication.Initialize`（第 27-31 行）的硬编码 Dark 与 `VSCodePalette.ApplyTo` 的写死色值（色值在 UIPackage 模块）。
6. **要改 View/ViewModel 命名约定**：改 `ConfigureViewModelLocator`（`FrameworkApplication.cs:237`）的 `SetDefaultViewTypeToViewModelTypeResolver` 委托。
7. **要新增/修改布局档位**（面板对齐）：三处同步改——`Layout/PanelAlignment.cs` 枚举加/改档；`Windows/FrameworkWindowTheme.axaml` 加一份 `WindowLayout*` 静态布局模板（仿现有四份，差异点在 BottomPanel 及其分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`）并同步四份模板的公共结构；`FrameworkWindow.UpdateLayoutTemplate`（`Windows/FrameworkWindow.cs:96-102`）的枚举→资源键映射。注意 Center 是 switch 兜底分支（`_ => "WindowLayoutCenter"`），新档必须显式加分支；消费侧（Modules/Workstation 的 `Menus/ViewAlignmentMenus.cs`）还要为新档加一个 `[MenuItem]` 方法。
8. **改 layout.json 格式**：同步 ShellLayoutDto、版本号、ShellLayoutConfiguration.Restore/Capture；Workstation 只提交状态和接线。当前仍为版本 1，旧数据兼容，不把 UI 集合当持久化事实来源。
9. **要改命令面板行为**：交互在 `Windows/CommandPalette.cs`，样式在 `FrameworkWindowTheme.axaml`，水印与空态在 `Resources/FrameworkResources.*`；排序/去重改 `ShellContributionCollector.GetCommands`。模块命令用 `[Command(typeof(ModuleResources), titleKey, Gesture = "...")]` 声明快捷键，shell 收集后调用 `RegisterCommandGestures` 完成接线。
10. **要改拖拽语义**（迁移/重排/显隐联动/回退规则）：只改 `ShellLayoutState.MoveTab`（`Layout/ShellLayoutState.cs:109`）——语义全在这个纯函数里；控件侧（`ToolViewButton`/`ToolViewBar`）只管手势与落点视觉，不要往里加逻辑。要改落点视觉（占位线样式/插入序号计算）才去 `ToolViewBar` 与其 ControlTheme（`FrameworkWindowTheme.axaml:492`）。
