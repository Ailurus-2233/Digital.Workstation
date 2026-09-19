# Workstation — 模块简述

## 模块做什么

`Modules/Workstation` 是应用宿主与 shell：提供 `WorkstationApplication`、`MainWindow`、驱动布局与持久化接线的 `MainWindowViewModel`。五区布局、菜单栏与命令面板机制在 Framework，本模块负责应用级 chrome 与贡献收集。当前无工具视图实例；预置贡献为 `ReadyStatusBarItem`、六个菜单类（`FileMenus`、`FileNavigationMenus`、`ViewPanelMenus`、`ViewAlignmentMenus`、`ViewLayoutMenus`、`HelpMenus`）及两个命令宿主（`ViewCommands`、`FileNavigationMenus`），由 attribute 扫描注册。内置视图为启动主页 `EmptyStateView` 与关于窗口。设置页由 Settings 模块贡献，文件菜单“首选项”与左下角“设置”按钮均通过 `OpenMainViewEvent(WellKnownViews.Settings)` 打开；功能模块只依赖 Abstractions 契约，不引用本模块 UI。

## 核心设计逻辑

### 主页与文件菜单导航

- 主页是启动时的 `EmptyStateView`，不是 DashBoard 主视图贡献。VM 将启动时解析的实例保存在 `_homeContent`，回到主页不重新解析。
- 下方左侧固定标题“已加载模块”位于 TreeView 外。树内保留“系统核心”“自定义模块”两个可折叠分组，初始展开；前者枚举已加载的 DigitalWorkstation.Core.* 程序集（排除动态和卫星程序集），后者仅保留 Initialized 且入口没有 PluginAttribute 的内置模块。各组按名称排序，空提示不生成可选的虚假模块。
- 下方右侧固定标题为“已加载插件”，列表只显示 Initialized 且入口具有 PluginAttribute 的插件，无插件时显示本地化空提示。ModuleType 通过已加载程序集快照和显式 assemblyResolver 解析，保留 Release 插件上下文，不为主页重新加载 DLL；插件不会重复出现在左栏。
- 左侧叶子与右侧插件共用 `LoadedComponentTemplate`，名称绑定 `LoadedComponentItem.Name`，条目悬浮提示绑定 `Description`。说明优先使用非空 `AssemblyDescriptionAttribute.Description`；缺省显示 Core 程序集完整身份或模块入口类型全名，类型解析不到时保留模块名称。此列表不打开模块视图、不改变 ShellLayoutState、不写配置。
- `EmptyStateView` 使用无参构造，Prism 按 `Views.EmptyStateView → ViewModels.EmptyStateViewModel` 自动关联；`IModuleCatalog` 注入 ViewModel。每次挂载只清空树选择并调用 `Refresh()` 更新三个列表快照，不订阅选择变化或后台加载事件。
- 模块与插件两栏按 1:1 等宽分配，中间固定间距为 16px。两个固定标题位于灰色内容卡片上方，共用 Grid 的标题行，卡片位于下一行；标题不属于卡片内容。主页树局部覆盖 Semi 的 `TreeViewItemIndent` 为 12px，缩小每级缩进，不改变全局 TreeView 主题。
- 主页品牌区保持左右布局：左列 112px 图标靠右，右列产品名与副标题全部左对齐，二者间隔 24px；图文整体垂直居中，文字随可用宽度换行。高度不足时整页滚动。
- 主页图标为 `Launcher/Assets/AppIcon.svg` 导出的 256px `AppIcon.png`，由 Workstation 项目以链接资源嵌入 `Assets/AppIcon.png`，不依赖运行时当前目录。
- `Menus/FileNavigationMenus.cs` 声明文件菜单 Navigation 组（GroupOrder 100）：“回到主页”（Order 100）、“首选项”（Order 200）；原 `FileMenus` 的 Application 组（GroupOrder 1000）保持“退出”，组间自动插分隔线。
- `FileNavigationMenus` 的两个导航方法均标注 `[MenuItem]` 与 `[Command]`，菜单与命令面板执行同一方法。`ReturnHome` 发布无负载 `ReturnHomeEvent`，不声明快捷键；`OpenPreferences` 发布 `OpenMainViewEvent(WellKnownViews.Settings)`，通过 `Gesture = "Ctrl+OemComma"` 注册 Ctrl+,。左下角设置入口保留。
- `MainWindowViewModel.ReturnHome` 仅清除 `State.MainContent.ActiveView` 并恢复 `_homeContent`；面板布局、对齐、持久化文件及主视图缓存均不变。已在主页且活动主视图为空时直接返回。设置页再次打开复用原实例。

### 布局与贡献机制

- **单一不可变状态源**：整个工作区布局由 `MainWindowViewModel.State`（`ShellLayoutState`，Framework 的 `sealed record`，`MainWindowViewModel.cs:60`）驱动。所有显隐/选中/tab 激活/尺寸/拖拽迁移变更都是对该 record 的纯函数转换后整体替换（`State = State.SelectActivity(...)` 等），布局 XAML 全部绑定 `State.*` 或其派生属性（如 `IsVisible="{Binding State.SideBar.Visible}"`，Framework 的 `FrameworkWindowTheme.axaml:65`）。ViewModel 不直接存任何面板显隐标志。**面板对齐档位不在 `State` 里**（区别于显隐/尺寸/tab）：它是 `FrameworkWindow.PanelAlignment` 依赖属性，`MainWindowViewModel.PanelAlignment`（:85）只是与之双向绑定的镜像属性。
- **基础布局在 Framework，菜单按平台呈现**：VS Code 式五区 shell、状态栏与菜单机制由 Framework 的 `FrameworkWindow` 提供。macOS 上窗口不再把菜单塞进 Ursa `LeftContent`，而是在 `MainWindow.PrepareContributions` 完成贡献收集后调用 `RegisterNativeMenu(MenuBarItems)`，把 File/View/Help 菜单放入屏幕顶部系统菜单栏；其他平台仍使用标题栏内的 `Menu.chrome-menu`。产品名通过 `Core/Resource` 的 `SharedResources.ProductName` 本地化：中文「数字工作站」，英文 `Digital Workstation`；`Application.Name` 由 Framework 在载入语言设置后赋值，`WorkstationApplication` 构造期不提前固化产品名。
- **标题视觉居中**：主窗口的 Ursa `TitleBar` 高度与窗口顶部预留统一为 32px；`TitleBarContent` 使用同高 Grid 承载标题，文本垂直居中并向下做 2px 光学校正，补偿 macOS 字体度量造成的视觉上浮。
- **贡献在 Ready 前准备呈现**：VM 构造不读贡献。Framework 完成模块贡献批次准备后调用 WorkstationApplication.PrepareShell → MainWindow.PrepareContributions → EnsureContributionsLoaded，再注册原生菜单和命令手势。全部成功后才设置 _contributionsLoaded；随后 Framework 发布 Ready 并显示窗口。Opened 不再负责初始化。
- **面板显隐的唯一路径**：快捷键（`Commands/ViewCommands.cs` 三个命令的 `Gesture`，经 `MainWindow.axaml.cs:22` 的 `RegisterCommandGestures` 接线生成窗口级 KeyBinding，与命令面板共用同一命令对象）、视图菜单项（`Menus/ViewPanelMenus.cs` 的 `[MenuItem]` 方法发布 `TogglePanelVisibilityEvent`）、命令面板命令（`Commands/ViewCommands.cs` 的 `[Command]` 方法，同一事件，[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）、面板收起按钮（Framework 的 `FrameworkWindowTheme.axaml`，调 VM 的 `ToggleAuxiliaryPanelCommand`/`ToggleBottomPanelCommand`）四路全部汇聚到 `MainWindowViewModel.TogglePanel(TogglePanelTarget)`（`MainWindowViewModel.cs:488-497`）——菜单项/命令与 ViewModel 之间经 `IEventAggregator` 解耦，菜单类/命令类不需要引用 ViewModel。**面板对齐同理**：视图菜单对齐项（`Menus/ViewAlignmentMenus.cs`）发布 `SetPanelAlignmentEvent`，ViewModel 订阅后写入 `PanelAlignment` 镜像属性；对齐组与显隐组之间的分隔线由建树器按组自动生成。**重置布局亦同理**：视图菜单 Layout 组（`Menus/ViewLayoutMenus.cs`）与命令面板的"重置布局"命令发布 `ResetLayoutEvent`，ViewModel 订阅后删除持久化文件并全默认重建。**拖拽迁移也走状态机**：Framework 的 `ToolViewBar` 落放 → `MoveTabCommand`（:440）→ `ShellLayoutState.MoveTab`。
- **视图实例缓存**：工具视图内容统一按 Id 单实例缓存 `_toolViewContents`（:37，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)——原 `_sideBarContents`/`_auxTabContents`/`_bottomTabContents` 三个缓存已合并，跨 Bar 迁移时实例随 tab 走），MainContent 主视图（`_mainViewContents`）按 Id 缓存；首次经 `_containerProvider.Resolve(类型)` 创建后复用，切换再切回不丢状态；缓存永不失效，视图实例寿命 = 应用寿命（重置布局也不清这两个缓存，见 `ResetLayout` :503-521）。
- **布局单一提交出口**：选择、激活、显隐、尺寸、移动、对齐都调用 ApplyLayout。它先准备目标内容，清空所有将改变的旧宿主，然后提交 State/PanelAlignment、同步四个呈现集合和高亮、挂入三个内容宿主，最后保存 ShellLayoutConfiguration.Capture 的快照。恢复与重置也走此出口但不保存。配置默认值/过滤/恢复和捕获集中在 Framework 的 ShellLayoutConfiguration，文件写入归 LayoutPersistence。
- **拖拽只产生增量**：`PanelResizer` 已迁入 Framework（`Core/Framework/Layout/PanelResizer.cs:13`）——仍**继承 `GridSplitter`**（复用其拖拽手势、方向光标与 ControlTheme）并重写 `GetParentGrid()` 返回 `null` 使原生列重排短路，只剩 Thumb 拖拽事件；但方向换算（AuxiliaryPanel/BottomPanel 取反）已内聚进其 `OnDragDelta`（:54-63），经 `Target` + `ResizeCommand` 属性把增量包装为 `PanelResize` 声明式发给 `MainWindowViewModel.ResizePanelCommand`（`MainWindowViewModel.cs:429`）→ `ShellLayoutState.Resize`（含 clamp）→ `ApplyLayout` 同步呈现并落盘。code-behind 不再参与拖拽。面板尺寸的唯一来源仍是 `ShellLayoutState`（BottomPanel 高度由布局模板绑 `State.BottomPanel.Height`，列宽经 `SideBarColumnWidth`/`AuxiliaryColumnWidth` 单向绑定）。**工具视图拖拽同构**（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：`ToolViewButton` 发起 `DoDragDropAsync`、`ToolViewBar` 算落点并显示占位线，Drop 包装 `ToolViewMove` 经 `MoveCommand` 发给 `MoveTabCommand`；控件只做手势与视觉，语义全在 `ShellLayoutState.MoveTab`。

## 状态流转

```text
WorkstationApplication 登记宿主菜单/命令/状态栏和模块目录
  → Prism 创建 MainWindow + VM（主页实例就位，不收集贡献）
  → Framework 逐模块加载并准备贡献，失败批次不可见
  → PrepareShell → MainWindow.PrepareContributions
      → EnsureContributionsLoaded：收集工具视图、恢复布局、建立主视图索引、
        建菜单树、状态栏和命令集合；成功后设置收集标志
      → RegisterNativeMenu / RegisterCommandGestures
  → Ready → 显示主窗口
```

布局动作先调用 ShellLayoutState 转换，再经 ApplyLayout 提交。目标内容解析失败时尚未修改 State 和宿主；发生跨区迁移时，所有改变的旧宿主先置空，再安装目标，避免单个缓存对象同时挂两处。空面板同样参与同步。高亮和四个集合最终以 State 中的有序 Id 为准，钉住项按贡献默认归属呈现。

LoadToolViews 将当前贡献和可选 DTO 交给 ShellLayoutConfiguration.Restore；配置优先、默认兜底，恢复选中/活动项、显隐/尺寸/对齐并过滤孤儿。重置走 LoadToolViews(null)，保留当前 MainContent 状态和实例，成功后 Delete 配置。返回主页仍清空 ActiveView 并恢复同一主页实例；打开主视图先解析成功，再更新 ActiveView 与 MainContent。

列宽由 Framework 的 ShellLayoutMetrics.PanelColumn 投影，使用与卡片模板相同的 CardMargin；Workstation 不维护 +4 的主题知识。

## 常见修改场景

1. **新增一个 shell 预置菜单项**（如"文件>新建"）：在 `Menus/` 下新建菜单类（或扩展现有 `FileMenus`/`ViewPanelMenus`/`ViewAlignmentMenus`/`ViewLayoutMenus`/`HelpMenus`）——类上标 `[MenuGroup("shell.file", Group=…, GroupOrder=…, Order=…)]` 挂接文件菜单并声明分组位次，方法上标 `[MenuItem(typeof(WorkstationResources), nameof(WorkstationResources.标题键), Order=…, Icon=…)]`（仅支持无参 `void`/`Task` 方法，参照 `FileMenus.cs` 的 `Exit`）；注册无需改动，`WorkstationApplication.RegisterCustomService` 的 `containerRegistry.RegisterMenus(typeof(WorkstationApplication).Assembly)`（`WorkstationApplication.cs:31`）按 attribute 扫描自动覆盖。如需本地化文案，同步在 Modules/Workstation/Resources 的 `WorkstationResources.cs` 与两个同名 resx 加键（attribute 显式指定资源类型与 nameof 键）。
2. **改面板显隐行为**（如让 Ctrl+B 同时收起 AuxiliaryPanel）：关键逻辑在 `MainWindowViewModel.TogglePanel`（:488-497）与 Framework 的 `ShellLayoutState.Toggle*` 转换方法；本模块只决定调哪个转换，语义（独立翻转、保留记录）在 Framework。注意 `TogglePanel` switch 的默认分支（见 pitfalls.md）。
3. **新增/替换 shell 预置工具视图**（面板 tab 或 ActivityBar 项，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：在 `Views/` 新建视图（当前仓库无现存实例；机制见 [ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)），code-behind 的类上标 `[ToolView("shell.xxx", typeof(WorkstationResources), nameof(WorkstationResources.NewTitle), Icon = Icons.Xxx, Default = ToolViewPlacement.BottomPanel, Order = …)]`（`Default` 缺省 AuxiliaryPanel；`AllowMove = false` + `Default = ActivityBar` 即钉住项，固定在 ActivityBar 底部段——机制保留，当前模块内无内置钉住项实例）——**注册零改动**，`RegisterCustomService` 的一行 `RegisterToolViews(typeof(WorkstationApplication).Assembly)`（`WorkstationApplication.cs:27`）按 attribute 扫描自动覆盖（同时把 View 类型注册进容器，不再手写 `Register<XxxView>()`）。如需本地化文案，同步在 Core/Resource 加资源键。tab 默认激活首个或持久化配置的活动 tab（Framework 的 ShellLayoutConfiguration.Restore）。
4. **改布局外观**（圆角、间隙、状态栏高度、分隔条热区、菜单样式）：全部在 Framework 的 `Core/Framework/Windows/FrameworkWindowTheme.axaml`（五区布局与 nav-item/panel-tab/GridSplitter/status-item/chrome-menu 等样式已整体迁入）与 `FrameworkWindow.cs`（内置菜单栏创建）；`MainWindow.axaml` 只剩应用级 chrome（窗口标题）。细节见 `docs/analysis/Core/Framework/` 文档。
5. **让某模块启动后自动打开一个主视图**：该模块在初始化后发布 `OpenMainViewEvent(viewId)`（`IEventAggregator`），`MainWindowViewModel.OpenMainView`（:344）接收——前提是 viewId 精确等于其 `IMainViewContribution.Id`，否则静默无反应。
6. **新增一个 shell 预置命令**（如"切换主侧栏"之外的命令）：在 `Commands/ViewCommands.cs` 加方法（或新建命令类），方法上标 `[Command(typeof(WorkstationResources), nameof(WorkstationResources.标题键), Order=…, Icon=…, Gesture=…)]`（仅支持无参 `void`/`Task`；`Icon`/`Gesture` 可空，参照现有四个方法）——**注册零改动**，`RegisterCustomService` 的 `containerRegistry.RegisterCommands(typeof(WorkstationApplication).Assembly)`（`WorkstationApplication.cs:33`）按 attribute 扫描自动覆盖；命令与菜单是两套独立声明（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)），想让同一动作同时出现在菜单栏需另标 `[MenuItem]`。如需本地化文案，同步在 Modules/Workstation/Resources 加资源键；Gesture 的 KeyBinding 由 `MainWindow.axaml.cs:22` 的接线自动生成，无需手动注册。

## 文案所有权

本模块私有文案由 `DigitalWorkstation.Workstation.Resources.WorkstationResources` 与 `Modules/Workstation/Resources/WorkstationResources.resx`、`WorkstationResources.en-US.resx` 持有；共享产品名仅取 `Core/Resource` 的 `SharedResources.ProductName`。facade 调共享 `ResourceText.Get`，不删除 Core/Resource 依赖。主页实际位于本模块，Home*、关于标题、shell 菜单/命令/状态栏与设置导航均归 Workstation。菜单根路径固定为 `shell.file`、`shell.view`、`shell.help`；标题键不再充当路径。终端标题由注册器解析，建树器按稳定路径合并，重复终端声明首个标题生效、位次仍取最小值。

菜单根标题仅由 FileMenus（shell.file）、ViewPanelMenus（shell.view）、HelpMenus（shell.help）声明。FileNavigationMenus、ViewAlignmentMenus、ViewLayoutMenus 使用 reference-only MenuGroup，只挂接稳定路径；外部模块同样可用 `[MenuGroup("shell.file")]` 加入文件菜单，不导入 WorkstationResources、不复制根标题。reference-only 的 ResourceType/TitleKey/PathTitle 为 null，不参与首个标题决议；先挂接后声明时补齐真实标题，未声明节点显示稳定 Id。
