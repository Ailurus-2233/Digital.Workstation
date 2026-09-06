# Workstation — 模块简述

## 模块做什么

`Modules/Workstation` 是 Digital.Workstation 的**应用宿主与 shell（工作台外壳）**：它提供主窗口 `MainWindow`（VS Code 式布局：ActivityBar + SideBar + MainContent + BottomPanel + AuxiliaryPanel + 状态栏 + 标题栏内嵌菜单）、主窗口 ViewModel `MainWindowViewModel`（驱动整个工作区布局状态）、应用入口 `WorkstationApplication`（继承 `FrameworkApplication<MainWindow>`），以及一组 shell 预置的界面贡献（`Shell/` 目录：设置导航项、四个演示面板 tab、退出/关于/三个面板显隐切换菜单项、就绪状态栏项）和内置视图（`Views/`：空状态页、设置/属性/大纲/输出/日志占位视图、关于窗口）。功能模块（如 DashBoard）不直接引用本模块的 UI，只按 Abstractions 的贡献契约注册，由本模块收集并渲染。

## 核心设计逻辑

- **单一不可变状态源**：整个工作区布局由 `MainWindowViewModel.State`（`ShellLayoutState`，Framework 的 `sealed record`，`MainWindowViewModel.cs:38`）驱动。所有显隐/选中/tab 激活/尺寸变更都是对该 record 的纯函数转换后整体替换（`State = State.SelectActivity(...)` 等），XAML 全部绑定 `State.*`（如 `IsVisible="{Binding State.SideBar.Visible}"`，`MainWindow.axaml:242`）。ViewModel 不直接存任何面板显隐标志。
- **贡献收集推迟到首次显示**：模块贡献在 Prism 模块初始化阶段（晚于 shell 创建）才注册进容器，因此 `MainWindowViewModel` 构造函数**不**收集贡献，而是由 `MainWindow.axaml.cs:39-43` 的 `OnOpened` 调 `EnsureContributionsLoaded()`（`MainWindowViewModel.cs:102`），用 `_contributionsLoaded` 布尔保证只收集一次。
- **面板显隐的唯一路径**：快捷键（`MainWindow.axaml:159-163` KeyBindings）、视图菜单项（`Shell/TogglePanelContribution.cs` 发布 `TogglePanelVisibilityEvent`）、面板收起按钮（`MainWindow.axaml:288-295、351-358`）三路全部汇聚到 `MainWindowViewModel.TogglePanel(TogglePanelTarget)`（`MainWindowViewModel.cs:249-257`）——菜单项与 ViewModel 之间经 `IEventAggregator` 解耦，菜单贡献类不需要引用 ViewModel。
- **视图实例缓存**：SideBar 内容（`_sideBarContents`）、MainContent 主视图（`_mainViewContents`）、两个面板的 tab 内容（`_auxTabContents`/`_bottomTabContents`）全部按字符串 Id 缓存（`MainWindowViewModel.cs:18-24`），首次经 `_containerProvider.Resolve(类型)` 创建后复用，切换再切回不丢状态；缓存永不失效，视图实例寿命 = 应用寿命。
- **拖拽只产生增量**：`PanelResizer`（`PanelResizer.cs:9`）**继承 `GridSplitter`**（复用其拖拽手势、方向光标与 ControlTheme）但重写 `GetParentGrid()` 返回 `null`，使原生列重排短路，只剩 Thumb 的 DragStarted/DragDelta/DragCompleted 拖拽手势事件；增量由 `MainWindow.axaml.cs` 的三个 handler 交给 `MainWindowViewModel.ResizePanel` → `ShellLayoutState.Resize`（含 clamp）。面板尺寸的唯一来源是 `ShellLayoutState`，XAML 里 `Width="{Binding State.SideBar.Width}"` 单向绑定。
- **贡献收集推迟到首次显示**：模块贡献在 Prism 模块初始化阶段（晚于 shell 创建）才注册进容器，因此 `MainWindowViewModel` 构造函数**不**收集贡献，而是由 `MainWindow.axaml.cs:39-43` 的 `OnOpened` 调 `EnsureContributionsLoaded()`（`MainWindowViewModel.cs:102`），用 `_contributionsLoaded` 布尔保证只收集一次。收集来源是构造注入的 **`ShellContributionCollector`（`_collector` 字段，:15）**——导航项、主视图、面板 tab、菜单项、状态栏项五类贡献全部经它的 `Get*` 方法拉取。贡献与视图的流通管道是 Prism 容器：注册侧用 `IContainerRegistry`（本模块在 `WorkstationApplication.RegisterCustomService`、各模块在自身 `RegisterTypes`），解析侧用 `IContainerProvider`（`MainWindowViewModel` 的 `_containerProvider` 按贡献声明的视图类型 `Resolve` 实例）。

## 状态流转

```
启动：WorkstationApplication（Prism 初始化）
  → FrameworkApplication.CreateShell() 解析 MainWindow → MainWindowViewModel 构造
      · 订阅 OpenMainViewEvent / TogglePanelVisibilityEvent（:32-33）
      · MainContent = EmptyStateView（:34）；State = ShellLayoutState.Initial
  → 模块逐模块加载（DashBoardModule 等注册各自贡献进容器）
  → MainWindow.Opened → EnsureContributionsLoaded()（:102）
      · ShellContributionCollector 五个收集方法（返回均已按 Order 排序）：
        GetNavigationItems(Top/Bottom) → TopNavigationItems/BottomNavigationItems（+ _itemsById）
        GetMainViews() → 按 Id 存入 _mainViewsById
        GetPanelTabs(Auxiliary/Bottom) → AuxiliaryTabs/BottomTabs（LoadPanelTabs 把 tab Id 列表
        与首个活动 tab 写入 State，:289-299）
        GetMenuItems(File/View/Help) → 三个菜单集合；GetStatusBarItems() → StatusBarItems
```

运行时四条交互路径：

1. **点导航项**（`SelectActivity`，:133）：`State.SelectActivity(id)`（再点已选中项会收起 SideBar）→ 同步所有导航项 `IsSelected` → 若 SideBar 可见且有 `ContentFor`，按 Id 从 `_sideBarContents` 取/建内容视图，更新 `SideBarTitle`/`SideBarContent`。
2. **打开主视图**（`OpenMainView`，:159，由 `OpenMainViewEvent` 触发，如 DashBoard 导航视图发布）：`_mainViewsById` 查 Id（查不到**静默返回**）→ `State.OpenMainView(viewId)` → 按 Id 从 `_mainViewContents` 取/建视图 → 整体替换 `MainContent`（单视图切换）。
3. **面板显隐**（`TogglePanel`，:249）：事件/快捷键/按钮 → `State.ToggleSideBar()/ToggleAuxiliaryPanel()/ToggleBottomPanel()` 之一，独立翻转可见性，选中项、活动 tab、尺寸记录全部保留。
4. **拖拽调尺寸**（`ResizePanel`，:241）：`PanelResizer.DragDelta` → code-behind 换算方向（AuxiliaryPanel/BottomPanel 取反）→ `State.Resize(target, delta)` clamp 到 Min/Max → XAML 绑定自动跟随。

副作用：除 `State` 与各 `ObservableCollection`/`[ObservableProperty]` 外，唯一副作用是容器解析视图实例（创建对象）与 `TogglePanelContribution` 发布事件；不读写磁盘、不碰线程（全部假定 UI 线程）。

## 常见修改场景

1. **新增一个 shell 预置菜单项**（如"文件>新建"）：在 `Shell/` 下新建类实现 `IMenuItemContribution`（参照 `ExitMenuItem.cs`：Id/Title 走 `Language`/Icons、`Order` 决定排序、`Menu` 选 `MenuPlacement.File`、`Command` 用 `DelegateCommand`），然后在 `WorkstationApplication.RegisterCustomService`（`WorkstationApplication.cs:19-46`）加一行 `RegisterSingleton<IMenuItemContribution, 新类>()`。如需本地化文案，同步在 Core/Resource 的 `Language.cs` 与两个 resx 加键。
2. **改面板显隐行为**（如让 Ctrl+B 同时收起 AuxiliaryPanel）：关键逻辑在 `MainWindowViewModel.TogglePanel`（:249-257）与 Framework 的 `ShellLayoutState.Toggle*` 转换方法；本模块只决定调哪个转换，语义（独立翻转、保留记录）在 Framework。注意 `TogglePanelContribution` 的 switch 默认分支（见 pitfalls.md）。
3. **新增/替换 shell 预置面板 tab**：新建 `XxxPanelTab : IPanelTabContribution`（参照 `Shell/OutputPanelTab.cs`，`Panel` 选 Auxiliary/Bottom、`ContentViewType` 指向视图）+ 占位视图放 `Views/`，然后在 `RegisterCustomService` 同时 `RegisterSingleton<IPanelTabContribution, XxxPanelTab>()` 和 `Register<XxxView>()`（`WorkstationApplication.cs:26-33`）。tab 默认激活首个（`LoadPanelTabs`，`MainWindowViewModel.cs:288`）。
4. **改布局外观**（圆角、间隙、状态栏高度、分隔条热区）：全部在 `MainWindow.axaml`——卡片间隙由容器 `Padding="2"` + 卡片 `Margin="2"` 合成恒 4px（:218-219 注释）；分隔条 8px 热区负边距覆盖间隙（:251-262、324-335、387-398 注释）；状态栏 24px（:197-201）。
5. **让某模块启动后自动打开一个主视图**：该模块在初始化后发布 `OpenMainViewEvent(viewId)`（`IEventAggregator`），`MainWindowViewModel.OpenMainView`（:159）接收——前提是 viewId 精确等于其 `IMainViewContribution.Id`，否则静默无反应。
