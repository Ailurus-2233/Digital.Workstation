# Workstation — 文件结构与功能

相对 `Modules/Workstation/` 的目录树（不含 `obj/`、`Output/` 构建产物）：

```
Workstation.csproj                  项目文件：net10.0；引用 Framework/Resource/UIPackage/DashBoard/Settings；3 条 DependentUpon
WorkstationApplication.cs           应用入口：WorkstationApplication : FrameworkApplication<MainWindow>
MainWindow.axaml                    主窗口 XAML（47 行）：FrameworkWindow，只留应用级 chrome（窗口标题/快捷键）
MainWindow.axaml.cs                 主窗口 code-behind：构造函数接线 Opened → EnsureContributionsLoaded + RegisterCommandGestures（命令手势接线，ADR-0005）
MainWindowViewModel.cs              主窗口 ViewModel（712 行）：布局状态机驱动 + 面板对齐档位 + 贡献收集（含命令 Commands，ADR-0005）+ 统一视图缓存（ADR-0002）+ 布局持久化接线 + 拖拽落放（MoveTabCommand）+ ActivityBar"设置"导航按钮（OpenSettingsCommand/SettingsIcon/SettingsTitle，ADR-0006 决策 6）
NavigationItemViewModel.cs          ActivityBar 导航项呈现模型（ObservableObject，IsSelected）
PanelTabViewModel.cs                面板 tab 呈现模型（ObservableObject，IsActive）
StatusBarItemViewModel.cs           状态栏条目呈现模型（普通类）
Contributions/
  ReadyStatusBarItem.cs             状态栏"就绪"贡献（Order 10；Contributions/ 唯一贡献类——工具视图已改 [ToolView] attribute，ADR-0002）
Menus/
  FileMenus.cs                      文件菜单类（[MenuGroup("MenuFileTitle", Group="Application", GroupOrder=1000, Order=100)]，Exit 方法 Shutdown()）
  ViewPanelMenus.cs                 视图菜单 Panels 组类（三个显隐切换方法发布 TogglePanelVisibilityEvent）
  ViewAlignmentMenus.cs             视图菜单 Alignment 组类（四个对齐方法发布 SetPanelAlignmentEvent）
  ViewLayoutMenus.cs                 视图菜单 Layout 组类（GroupOrder=300，单项"重置布局"发布 ResetLayoutEvent）
  HelpMenus.cs                      帮助菜单类（[MenuGroup("MenuHelpTitle", Order=300)]，About 方法 ShowDialog<AboutWindow>）
Commands/
  ViewCommands.cs                   shell 预置命令类（ADR-0005）：四个 [Command] 方法（三面板显隐切换 + 重置布局，复用视图菜单标题键与事件通路；三面板命令带 Icons.PanelLeft/PanelBottom/PanelRight 图标，与对应菜单项一致）
Views/
  EmptyStateView.axaml(.cs)         shell 内置空状态页：快捷键提示（Ctrl+B/J、Ctrl+Alt+B），不依赖任何模块
  PropertiesView.axaml(.cs)         "属性"占位 UserControl + [ToolView("shell.properties", Order=10)]（AuxiliaryPanel 缺省，:10）
  OutlineView.axaml(.cs)            "大纲"占位 UserControl + [ToolView("shell.outline", Order=20)]（AuxiliaryPanel 缺省，:10）
  OutputView.axaml(.cs)             "输出"占位 UserControl + [ToolView("shell.output", BottomPanel, Order=10)]（:10-11）
  LogView.axaml(.cs)                "日志"占位 UserControl + [ToolView("shell.log", BottomPanel, Order=20)]（:10-11）
  AboutWindow.axaml(.cs)            "关于"对话框：360×160 不可调大小、CenterOwner，硬编码中文文案
```

## 逐文件说明
- **WorkstationApplication.cs**：`ConfigureModuleCatalog`（:17-21）两处 `AddModule`——注册 `DashBoardModule`（:19）与 `SettingsModule`（:20）；`RegisterCustomService`（:23-38）注册全部 shell 预置贡献与内置视图——工具视图为一行 `RegisterToolViews(typeof(WorkstationApplication).Assembly)`（:27，attribute 扫描注册 View 类型 + `ToolViewContribution` 元数据，ADR-0002），菜单为一行 `RegisterMenus(...)`（:31，ADR-0001），命令为一行 `RegisterCommands(...)`（:33，ADR-0005），另有 `EmptyStateView`/`ReadyStatusBarItem`/`AboutWindow` 三条；`CreateSplashWindow`（:43-46）返回 `DashBoardWindow`。
- **MainWindow.axaml**：`win:FrameworkWindow`（1280×800，最小 960×600，`Padding="0,32,0,0"`；`xmlns:win` 指向 `DigitalWorkstation.Core.Framework.Windows`，assembly=DigitalWorkstation.Core.Framework，:18）。**五区布局、状态栏、菜单栏与全部样式已迁出**——布局与 nav-item/panel-tab/panel-collapse/GridSplitter/region-title/placeholder/status-item/chrome-menu 样式在 `Core/Framework/Windows/FrameworkWindowTheme.axaml`，菜单栏（`Menu` + 项模板 + `MenuBarItems` 宽松绑定）由 `FrameworkWindow` 构造函数在代码中内置创建（`Core/Framework/Windows/FrameworkWindow.cs:35-41`）；头部注释（:22-23）指明此分工，本文件只留应用级 chrome。`PanelAlignment="{Binding PanelAlignment, Mode=TwoWay}"`（:11）——面板对齐档位与 VM 镜像属性双向绑定。`Styles`（:25-30）只剩 `u|TitleBar` 底色与窗口背景同色（:27-29）。`KeyBindings`（:32-36）：Ctrl+B→`ToggleSideBarCommand`、Ctrl+J→`ToggleBottomPanelCommand`、Ctrl+Alt+B→`ToggleAuxiliaryPanelCommand`。`TitleBarContent`（:38-46）：居中标题文本，绑 `$parent[u:UrsaWindow].Title`（:44）。
- **MainWindow.axaml.cs**：`MainWindow : FrameworkWindow`（原 `UrsaWindow`）。构造函数（:7-11）在 `InitializeComponent()` 后接线 **`Opened += OnOpened`**——这是 `EnsureContributionsLoaded` 的触发链路起点。`OnOpened`（:13-23）先调 `EnsureContributionsLoaded()`（注释说明模块贡献在 Prism 模块初始化即晚于 shell 创建时才注册，首次显示时再收集），再调 `RegisterCommandGestures(viewModel.Commands)`（:22，ADR-0005——机制在 Framework、接线在本模块，为带 Gesture 的命令生成窗口级 KeyBinding）。原三个 `OnXxxResizerDragDelta` handler 已删除——方向换算内聚进 Framework 的 `PanelResizer`，code-behind 不再参与拖拽。
- **MainWindowViewModel.cs**：构造（:41-55，四参注入含 `LayoutPersistence`，订阅 OpenMainView/TogglePanelVisibility/SetPanelAlignment/ResetLayout 四个事件 + Framework `ToolViewDragSession.ActiveChanged`）；`EnsureContributionsLoaded`（:170-193——`_toolViews = GetToolViews()` 一次缓存（:178），`LoadToolViews(_persistence.Load())`（:179）按持久化配置/默认兜底分派三处 Bar（钉住项恒落 ActivityBar 底部段，当前无内置钉住项实例），菜单为 `MenuTreeBuilder.Build(_collector.GetMenuItems())` 建树 + `MenuItemViewModel.FromSubmenu(submenu, isTopLevel: true)` → `MenuBarItems`（根项标记顶层，菜单模板据此不预留图标槽位），命令为 `Commands = _collector.GetCommands()`（:192，Order/标题排序 + Id 去重，命令面板数据源与手势 KeyBinding 来源，ADR-0005））；布局装载链 `LoadToolViews`（:200-236，ActivityBar 顶部段顺序写入 `State.ActivityBarItems`）/`RestoreLayout`（:242-286）；交互路径 `SelectActivity`（:291）/`OpenSettings`（:303-306，ActivityBar"设置"纯导航按钮：发布 `OpenMainViewEvent(WellKnownViews.Settings)`，配套属性 `SettingsIcon`（:146）/`SettingsTitle`（:151），ADR-0006 决策 6）/`ActivateAuxTab`/`ActivateBottomTab`（:365/:382）/`TogglePanel`（:497）/`ResizePanel`（:438）/`MoveTab`（:449，拖拽落放唯一路径：拒绝钉住项→State.MoveTab 转换→内容实例先脱离源视觉树→SyncBarCollection 对齐三个 Bar 集合→SyncSideBarSelection/SyncPanelTab 同步高亮与内容→ScheduleSave 落盘）；持久化 `CaptureLayout`（:536）/`ScheduleSave`（:587）/`ResetLayout`（:512）。
- **三个呈现模型**（`NavigationItemViewModel`/`PanelTabViewModel`/`StatusBarItemViewModel`）：结构同构——构造接收贡献元数据、解析 `Icon` 几何、透传 `Id`/`Title`；前两者包装 `ToolViewContribution`（ADR-0002），`IconPath` 为 null 时 `Icon` 为 null（各 :15）；后者包装 `IStatusBarItemContribution`，`IconPath` 必填（:14）（见 api.md 第 3 节）。`MenuItemViewModel` 已迁入 Framework（`Core/Framework/Menus/MenuItemViewModel.cs`），本模块经 `using DigitalWorkstation.Core.Framework.Menus` 解析。
- **Contributions/ + Menus/ + Commands/ 七个类**：一个 `IStatusBarItemContribution` 实现类（`Contributions/ReadyStatusBarItem.cs`，命名空间 `DigitalWorkstation.Workstation.Contributions`，纯属性实现）+ 五个 attribute 菜单类（`Menus/`，命名空间 `DigitalWorkstation.Workstation.Menus`，`[MenuGroup]` 类 + `[MenuItem]` 方法）+ 一个 attribute 命令类（`Commands/ViewCommands.cs`，命名空间 `DigitalWorkstation.Workstation.Commands`，免类级 attribute，`[Command]` 方法，ADR-0005），属性/attribute 矩阵见 api.md 第 5 节。工具视图不再是贡献类——声明在 `Views/` 四个 View 类的 `[ToolView]` attribute 上（ADR-0002）。
- **Views/**：`EmptyStateView` 是唯一的静态内容页（键帽样式 `Border.key-chip`、说明样式 `TextBlock.shortcut-desc`，EmptyStateView.axaml:13-26）；其余四个 UserControl 的 axaml 各 14 行占位、code-behind 各带一个 `[ToolView]` attribute（矩阵见 api.md 第 5 节）；`AboutWindow` 16 行静态窗口。
