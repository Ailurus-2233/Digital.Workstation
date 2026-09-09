# Workstation — 模块简述

## 模块做什么

`Modules/Workstation` 是 Digital.Workstation 的**应用宿主与 shell（工作台外壳）**：它提供主窗口 `MainWindow`（继承 Framework 的 `FrameworkWindow`，VS Code 式五区布局与标题栏菜单栏由基类及 `FrameworkWindowTheme.axaml`/`FrameworkWindow.cs` 提供；本模块只加应用级 chrome：窗口标题、快捷键、面板对齐档位）、主窗口 ViewModel `MainWindowViewModel`（驱动整个工作区布局状态，并把布局变更经 Framework 的 `LayoutPersistence` 防抖落盘）、应用入口 `WorkstationApplication`（继承 `FrameworkApplication<MainWindow>`），以及一组 shell 预置的界面贡献：**工具视图（Tool View，ADR-0002）**不再手写贡献类，改为在 `Views/` 的占位 View 类上标 `[ToolView]` attribute 声明（`SettingsView` ActivityBar 钉住项、`PropertiesView`/`OutlineView` AuxiliaryPanel、`OutputView`/`LogView` BottomPanel），由 Framework `RegisterToolViews` 扫描注册；`Contributions/` 目录只剩就绪状态栏项 `ReadyStatusBarItem`；`Menus/` 目录：五个 attribute 菜单类——`FileMenus` 文件>退出、`ViewPanelMenus` 视图>三个面板显隐切换、`ViewAlignmentMenus` 视图>四档面板对齐、`ViewLayoutMenus` 视图>重置布局、`HelpMenus` 帮助>关于，经 `[MenuGroup]`/`[MenuItem]` 标注由 Framework 扫描注册，ADR-0001）和内置视图（`Views/`：空状态页、五个工具视图占位 UserControl、关于窗口）。功能模块（如 DashBoard）不直接引用本模块的 UI，只按 Abstractions 的贡献契约注册（工具视图同样走 `[ToolView]` + `RegisterToolViews`），由本模块收集并渲染。

## 核心设计逻辑

- **单一不可变状态源**：整个工作区布局由 `MainWindowViewModel.State`（`ShellLayoutState`，Framework 的 `sealed record`，`MainWindowViewModel.cs:47`）驱动。所有显隐/选中/tab 激活/尺寸变更都是对该 record 的纯函数转换后整体替换（`State = State.SelectActivity(...)` 等），布局 XAML 全部绑定 `State.*`（如 `IsVisible="{Binding State.SideBar.Visible}"`，Framework 的 `FrameworkWindowTheme.axaml:45`）。ViewModel 不直接存任何面板显隐标志。**面板对齐档位不在 `State` 里**（区别于显隐/尺寸/tab）：它是 `FrameworkWindow.PanelAlignment` 依赖属性，`MainWindowViewModel.PanelAlignment`（:54）只是与之双向绑定的镜像属性。
- **基础布局在 Framework**：VS Code 式五区 shell + 状态栏 + 标题栏菜单栏由 Framework 的 `FrameworkWindow`（`Core/Framework/Windows/FrameworkWindow.cs:19`）提供——`PanelAlignment` 每个枚举值对应 `FrameworkWindowTheme.axaml` 里一份静态布局模板（`WindowLayoutLeft/Right/Center/Justify`），切换档位即整体替换模板，不做动态调整；菜单栏（`Menu` + 项模板 + `MenuBarItems` 宽松绑定）由 `FrameworkWindow` 构造函数在代码中内置创建（:35-41），样式在 `Core/Framework/Windows/FrameworkWindowTheme.axaml:551-569`；Framework 不引用具体 ViewModel 类型，布局模板里全部是宽松反射绑定。`MainWindow.axaml` 只剩应用级 chrome（窗口标题、快捷键），列宽经 VM 的 `SideBarColumnWidth`/`AuxiliaryColumnWidth`（:110-117）暴露给模板，面板隐藏时归零、BottomPanel 跨度随之自然伸缩。
- **贡献收集推迟到首次显示**：模块贡献在 Prism 模块初始化阶段（晚于 shell 创建）才注册进容器，因此 `MainWindowViewModel` 构造函数**不**收集贡献，而是由 `MainWindow.axaml.cs:13-17` 的 `OnOpened` 调 `EnsureContributionsLoaded()`（`MainWindowViewModel.cs:123`），用 `_contributionsLoaded` 布尔保证只收集一次。菜单部分（ADR-0001）：`ShellContributionCollector.GetMenuItems()` 一次拉出全部菜单贡献（不过滤不排序），交给 Framework 的 `MenuTreeBuilder.Build` 建树——顶层按 `(NodeOrder, 标题)` 排序不分组，子菜单按组分组、组间自动插分隔线——再经 Framework 的 `MenuItemViewModel.FromSubmenu` 递归转换为 `MenuBarItems`（呈现模型与渲染样式均已入 Framework，`FrameworkWindow` 内置菜单栏宽松绑定该集合）；旧版"视图菜单显隐组与对齐组之间手工插 Separator"的特判已删除，分隔线由分组自然表达。
- **面板显隐的唯一路径**：快捷键（`MainWindow.axaml:32-36` KeyBindings）、视图菜单项（`Menus/ViewPanelMenus.cs` 的 `[MenuItem]` 方法发布 `TogglePanelVisibilityEvent`）、面板收起按钮（Framework 的 `FrameworkWindowTheme.axaml`）三路全部汇聚到 `MainWindowViewModel.TogglePanel(TogglePanelTarget)`（`MainWindowViewModel.cs:384-393`）——菜单项与 ViewModel 之间经 `IEventAggregator` 解耦，菜单类不需要引用 ViewModel。**面板对齐同理**：视图菜单对齐项（`Menus/ViewAlignmentMenus.cs`）发布 `SetPanelAlignmentEvent`，ViewModel 订阅后写入 `PanelAlignment` 镜像属性；对齐组与显隐组之间的分隔线由建树器按组自动生成。**重置布局亦同理**：视图菜单 Layout 组（`Menus/ViewLayoutMenus.cs`）发布 `ResetLayoutEvent`，ViewModel 订阅后删除持久化文件并全默认重建。
- **视图实例缓存**：SideBar 内容（`_sideBarContents`）、MainContent 主视图（`_mainViewContents`）、两个面板的 tab 内容（`_auxTabContents`/`_bottomTabContents`）全部按字符串 Id 缓存（`MainWindowViewModel.cs:23-24、:27-28`），首次经 `_containerProvider.Resolve(类型)` 创建后复用，切换再切回不丢状态；缓存永不失效，视图实例寿命 = 应用寿命（重置布局也不清这四个缓存，见 `ResetLayout` :399-417）。
- **布局持久化接线（机制在 Framework、接线在本模块）**：落盘机制（DTO、500ms 防抖、文件路径 %AppData%/Digital.Workstation/layout.json、容错）全部在 Framework 的 `LayoutPersistence`（`Core/Framework/Layout/LayoutPersistence.cs`，`FrameworkApplication` 注册为单例）；本模块 VM 负责三件事——**捕获**（`CaptureLayout` :423-470 把当前布局快照为 `ShellLayoutDto`）、**恢复**（`EnsureContributionsLoaded` 里 `LoadToolViews(_persistence.Load())` :132，配置优先、默认兜底）、**重置**（`ResetLayoutEvent` → `ResetLayout` :399-417，删文件后 `LoadToolViews(null)` 全默认重建）。六个布局变更点（`SelectActivity`/`ActivateAuxTab`/`ActivateBottomTab`/`SetPanelAlignment`/`ResizePanel`/`TogglePanel`）末尾统一调 `ScheduleSave()`（:474-477）防抖落盘——新增变更路径时必须挂上，否则该变更不持久化（见 pitfalls.md）。
- **拖拽只产生增量**：`PanelResizer` 已迁入 Framework（`Core/Framework/Layout/PanelResizer.cs:13`）——仍**继承 `GridSplitter`**（复用其拖拽手势、方向光标与 ControlTheme）并重写 `GetParentGrid()` 返回 `null` 使原生列重排短路，只剩 Thumb 拖拽事件；但方向换算（AuxiliaryPanel/BottomPanel 取反）已内聚进其 `OnDragDelta`（:54-63），经 `Target` + `ResizeCommand` 属性把增量包装为 `PanelResize` 声明式发给 `MainWindowViewModel.ResizePanelCommand`（`MainWindowViewModel.cs:375`）→ `ShellLayoutState.Resize`（含 clamp）→ `ScheduleSave()` 落盘。code-behind 不再参与拖拽。面板尺寸的唯一来源仍是 `ShellLayoutState`（BottomPanel 高度由布局模板绑 `State.BottomPanel.Height`，列宽经 `SideBarColumnWidth`/`AuxiliaryColumnWidth` 单向绑定）。
- **贡献收集推迟到首次显示**（同上的补充）：收集来源是构造注入的 **`ShellContributionCollector`（`_collector` 字段，:18）**——工具视图、主视图、菜单项、状态栏项四类贡献全部经它的 `Get*` 方法拉取。工具视图（ADR-0002）由 `GetToolViews()` **一次**拉出（按 `Order` 升序）存入 `_toolViews` 字段缓存（:30、:131），再由 `LoadToolViews` 按持久化配置/attribute 默认分派到三处 Bar 与钉住区（钉住项 `AllowMove=false` 恒在 ActivityBar 底部段）；`GetMainViews`/`GetMenuItems`/`GetStatusBarItems` 不变。贡献与视图的流通管道是 Prism 容器：注册侧用 `IContainerRegistry`（本模块在 `WorkstationApplication.RegisterCustomService`、各模块在自身 `RegisterTypes`，工具视图一行 `RegisterToolViews(Assembly)` 同时注册 View 类型与元数据），解析侧用 `IContainerProvider`（`MainWindowViewModel` 的 `_containerProvider` 按贡献声明的视图类型 `Resolve` 实例）。
## 状态流转

```
启动：WorkstationApplication（Prism 初始化）
  → FrameworkApplication.CreateShell() 解析 MainWindow → MainWindowViewModel 构造
      · 订阅 OpenMainViewEvent / TogglePanelVisibilityEvent / SetPanelAlignmentEvent / ResetLayoutEvent（:38-41）
      · MainContent = EmptyStateView（:42）；State = ShellLayoutState.Initial
  → 模块逐模块加载（DashBoardModule 等注册各自贡献进容器）
  → MainWindow.Opened → EnsureContributionsLoaded()（:123）
      · _toolViews = GetToolViews() 一次缓存全部工具视图贡献（按 Order 升序，ADR-0002，:131）；
        LoadToolViews(_persistence.Load())（:132）按持久化布局分派：钉住项（AllowMove=false）→
        BottomNavigationItems；可移动项「配置优先、默认兜底」→ TopNavigationItems /
        AuxiliaryTabs / BottomTabs（均经 LoadItems 登记 _itemsById；LoadPanelTabs 恢复配置的活动
        tab，无配置默认首个，:482-515）；layout 非 null 时 RestoreLayout 恢复显隐/尺寸/选中/对齐
        （:188-246）；layout 为 null（缺失/损坏/版本不符）即全默认
        GetMainViews() → 按 Id 存入 _mainViewsById
        GetMenuItems() 一次（不过滤不排序，建树器负责）→ MenuTreeBuilder.Build 建树
        → MenuItemViewModel.FromSubmenu → MenuBarItems；GetStatusBarItems()（按 Order 排序）→ StatusBarItems
```

运行时五条交互路径：

1. **点导航项**（`SelectActivity`，:251）：`State.SelectActivity(id)`（再点已选中项会收起 SideBar）→ 同步所有导航项 `IsSelected` → **`ScheduleSave()` 防抖落盘**（:260）→ 若 SideBar 可见且有 `ContentFor`，按 Id 从 `_sideBarContents` 取/建内容视图（经 `Contribution.ViewType` 解析），更新 `SideBarTitle`/`SideBarContent`。
2. **打开主视图**（`OpenMainView`，:279，由 `OpenMainViewEvent` 触发，如 DashBoard 导航视图发布）：`_mainViewsById` 查 Id（查不到**静默返回**）→ `State.OpenMainView(viewId)` → 按 Id 从 `_mainViewContents` 取/建视图 → 整体替换 `MainContent`（单视图切换）。不落盘。
3. **面板显隐**（`TogglePanel`，:384）：事件/快捷键/按钮 → `State.ToggleSideBar()/ToggleAuxiliaryPanel()/ToggleBottomPanel()` 之一，独立翻转可见性，选中项、活动 tab、尺寸记录全部保留 → **`ScheduleSave()` 防抖落盘**（:392）。
4. **拖拽调尺寸**（`ResizePanelCommand`，:375）：Framework 的 `PanelResizer.DragDelta` 内部换算方向（AuxiliaryPanel/BottomPanel 取反）→ 经 `ResizeCommand` 执行 `ResizePanelCommand(new PanelResize(Target, delta))` → `State.Resize(target, delta)` clamp 到 Min/Max → **`ScheduleSave()` 防抖落盘**（:378）→ XAML 绑定自动跟随。code-behind 不参与。tab 激活（`ActivateAuxTab`/`ActivateBottomTab`，:300/:318）与对齐切换（`SetPanelAlignment`，:363）同样在状态变更后落盘。
5. **重置布局**（`ResetLayout`，:399-417，由视图菜单 Layout 组"重置布局"项发布 `ResetLayoutEvent` 触发）：`_persistence.Delete()` 删 layout.json（先作废 pending 防抖保存）→ 清空四个集合与三个索引字典 → `State = ShellLayoutState.Initial`、`PanelAlignment = Center`、SideBar 内容/标题清空 → `LoadToolViews(null)` 按 attribute 默认全量重建；视图实例缓存保留。

副作用：除 `State` 与各 `ObservableCollection`/`[ObservableProperty]` 外，副作用是容器解析视图实例（创建对象）、菜单类方法（`ViewPanelMenus`/`ViewAlignmentMenus`/`ViewLayoutMenus`）发布事件，以及**布局变更经 `LayoutPersistence` 防抖写磁盘 layout.json**（500ms 防抖只落最后一份；`ResetLayout` 删除该文件）；不碰线程（全部假定 UI 线程，落盘在 Framework 的 Timer 回调内自行容错）。

## 常见修改场景

1. **新增一个 shell 预置菜单项**（如"文件>新建"）：在 `Menus/` 下新建菜单类（或扩展现有 `FileMenus`/`ViewPanelMenus`/`ViewAlignmentMenus`/`ViewLayoutMenus`/`HelpMenus`）——类上标 `[MenuGroup("路径键", Group=…, GroupOrder=…, Order=…)]` 声明顶层菜单与分组位次，方法上标 `[MenuItem("标题键", Order=…, Icon=…)]`（仅支持无参 `void`/`Task` 方法，参照 `FileMenus.cs` 的 `Exit`）；注册无需改动，`WorkstationApplication.RegisterCustomService` 的 `containerRegistry.RegisterMenus(typeof(WorkstationApplication).Assembly)`（`WorkstationApplication.cs:28`）按 attribute 扫描自动覆盖。如需本地化文案，同步在 Core/Resource 的 `Language.cs` 与两个 resx 加键（attribute 里写的是资源键字符串）。
2. **改面板显隐行为**（如让 Ctrl+B 同时收起 AuxiliaryPanel）：关键逻辑在 `MainWindowViewModel.TogglePanel`（:384-393）与 Framework 的 `ShellLayoutState.Toggle*` 转换方法；本模块只决定调哪个转换，语义（独立翻转、保留记录）在 Framework。注意 `TogglePanel` switch 的默认分支（见 pitfalls.md）。
3. **新增/替换 shell 预置工具视图**（面板 tab 或 ActivityBar 项，ADR-0002）：在 `Views/` 新建占位视图（参照 `OutputView.axaml(.cs)`），code-behind 的类上标 `[ToolView("shell.xxx", "标题资源键", Icon = Icons.Xxx, Default = ToolViewPlacement.BottomPanel, Order = …)]`（`Default` 缺省 AuxiliaryPanel；`AllowMove = false` + `Default = ActivityBar` 即钉住项，固定在 ActivityBar 底部段，参照 `SettingsView.axaml.cs:10-11`）——**注册零改动**，`RegisterCustomService` 的一行 `RegisterToolViews(typeof(WorkstationApplication).Assembly)`（`WorkstationApplication.cs:24`）按 attribute 扫描自动覆盖（同时把 View 类型注册进容器，不再手写 `Register<XxxView>()`）。如需本地化文案，同步在 Core/Resource 加资源键。tab 默认激活首个或持久化配置的活动 tab（`LoadPanelTabs`，`MainWindowViewModel.cs:499-501`）。
4. **改布局外观**（圆角、间隙、状态栏高度、分隔条热区、菜单样式）：全部在 Framework 的 `Core/Framework/Windows/FrameworkWindowTheme.axaml`（五区布局与 nav-item/panel-tab/GridSplitter/status-item/chrome-menu 等样式已整体迁入）与 `FrameworkWindow.cs`（内置菜单栏创建）；`MainWindow.axaml` 只剩应用级 chrome（窗口标题/快捷键）。细节见 `docs/analysis/Core/Framework/` 文档。
5. **让某模块启动后自动打开一个主视图**：该模块在初始化后发布 `OpenMainViewEvent(viewId)`（`IEventAggregator`），`MainWindowViewModel.OpenMainView`（:279）接收——前提是 viewId 精确等于其 `IMainViewContribution.Id`，否则静默无反应。
