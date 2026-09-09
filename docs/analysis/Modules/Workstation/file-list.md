# Workstation — 文件结构与功能

相对 `Modules/Workstation/` 的目录树（不含 `obj/`、`Output/` 构建产物）：

```
Workstation.csproj                  项目文件：net10.0；引用 Framework/Resource/UIPackage/DashBoard；3 条 DependentUpon
WorkstationApplication.cs           应用入口：WorkstationApplication : FrameworkApplication<MainWindow>
MainWindow.axaml                    主窗口 XAML（47 行）：FrameworkWindow，只留应用级 chrome（窗口标题/快捷键）
MainWindow.axaml.cs                 主窗口 code-behind：构造函数接线 Opened → EnsureContributionsLoaded
MainWindowViewModel.cs              主窗口 ViewModel（554 行）：布局状态机驱动 + 面板对齐档位 + 贡献收集 + 视图缓存 + 布局持久化接线
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
Views/
  EmptyStateView.axaml(.cs)         shell 内置空状态页：快捷键提示（Ctrl+B/J、Ctrl+Alt+B），不依赖任何模块
  SettingsView.axaml(.cs)           "设置"占位 UserControl + [ToolView("shell.settings", ActivityBar, AllowMove=false)]（钉住项，:10-11）
  PropertiesView.axaml(.cs)         "属性"占位 UserControl + [ToolView("shell.properties", Order=10)]（AuxiliaryPanel 缺省，:10）
  OutlineView.axaml(.cs)            "大纲"占位 UserControl + [ToolView("shell.outline", Order=20)]（AuxiliaryPanel 缺省，:10）
  OutputView.axaml(.cs)             "输出"占位 UserControl + [ToolView("shell.output", BottomPanel, Order=10)]（:10-11）
  LogView.axaml(.cs)                "日志"占位 UserControl + [ToolView("shell.log", BottomPanel, Order=20)]（:10-11）
  AboutWindow.axaml(.cs)            "关于"对话框：360×160 不可调大小、CenterOwner，硬编码中文文案
```

## 逐文件说明
- **WorkstationApplication.cs**：`ConfigureModuleCatalog`（:15-18）注册 `DashBoardModule`；`RegisterCustomService`（:20-33）注册全部 shell 预置贡献与内置视图——工具视图为一行 `RegisterToolViews(typeof(WorkstationApplication).Assembly)`（:24，attribute 扫描注册 View 类型 + `ToolViewContribution` 元数据，ADR-0002），菜单为一行 `RegisterMenus(...)`（:28，ADR-0001），另有 `EmptyStateView`/`ReadyStatusBarItem`/`AboutWindow` 三条；`CreateSplashWindow`（:38-41）返回 `DashBoardWindow`。
- **MainWindow.axaml**：`win:FrameworkWindow`（1280×800，最小 960×600，`Padding="0,32,0,0"`；`xmlns:win` 指向 `DigitalWorkstation.Core.Framework.Windows`，assembly=DigitalWorkstation.Core.Framework，:18）。**五区布局、状态栏、菜单栏与全部样式已迁出**——布局与 nav-item/panel-tab/panel-collapse/GridSplitter/region-title/placeholder/status-item/chrome-menu 样式在 `Core/Framework/Windows/FrameworkWindowTheme.axaml`，菜单栏（`Menu` + 项模板 + `MenuBarItems` 宽松绑定）由 `FrameworkWindow` 构造函数在代码中内置创建（`Core/Framework/Windows/FrameworkWindow.cs:35-41`）；头部注释（:22-23）指明此分工，本文件只留应用级 chrome。`PanelAlignment="{Binding PanelAlignment, Mode=TwoWay}"`（:11）——面板对齐档位与 VM 镜像属性双向绑定。`Styles`（:25-30）只剩 `u|TitleBar` 底色与窗口背景同色（:27-29）。`KeyBindings`（:32-36）：Ctrl+B→`ToggleSideBarCommand`、Ctrl+J→`ToggleBottomPanelCommand`、Ctrl+Alt+B→`ToggleAuxiliaryPanelCommand`。`TitleBarContent`（:38-46）：居中标题文本，绑 `$parent[u:UrsaWindow].Title`（:44）。
- **MainWindow.axaml.cs**：`MainWindow : FrameworkWindow`（原 `UrsaWindow`）。构造函数（:7-11）在 `InitializeComponent()` 后接线 **`Opened += OnOpened`**——这是 `EnsureContributionsLoaded` 的触发链路起点。`OnOpened`（:13-17）调 `EnsureContributionsLoaded()`（注释说明模块贡献在 Prism 模块初始化即晚于 shell 创建时才注册，首次显示时再收集）。原三个 `OnXxxResizerDragDelta` handler 已删除——方向换算内聚进 Framework 的 `PanelResizer`，code-behind 不再参与拖拽。
- **MainWindowViewModel.cs**：构造（:32-43，四参注入含 `LayoutPersistence`，订阅 OpenMainView/TogglePanelVisibility/SetPanelAlignment/ResetLayout 四个事件）；`EnsureContributionsLoaded`（:123-145——`_toolViews = GetToolViews()` 一次缓存（:131），`LoadToolViews(_persistence.Load())`（:132）按持久化配置/默认兜底分派三处 Bar，菜单为 `MenuTreeBuilder.Build(_collector.GetMenuItems())` 建树 + `MenuItemViewModel.FromSubmenu` → `MenuBarItems`）；布局装载/恢复 `LoadToolViews`（:152-182）、`RestoreLayout`（:188-246）；命令/方法 `SelectActivity`（:251）、`OpenMainView`（:279）、`ActivateAuxTab`（:300）、`ActivateBottomTab`（:318）、`ToggleSideBar`（:336）、`ToggleAuxiliaryPanel`（:345）、`ToggleBottomPanel`（:354）、`SetPanelAlignment`（:363）、`ResizePanel(PanelResize)`（:375）、`TogglePanel`（:384）；持久化接线 `ResetLayout`（:399-417）、`CaptureLayout`（:423-470）、`ScheduleSave`（:474-477）；私有装载 `LoadPanelTabs`（:482，签名含 `preferredActiveTab`）、`SyncActiveTab`（:520）、`LoadItems`（:543）。字段与缓存字典清单见 reference.md。
- **三个呈现模型**（`NavigationItemViewModel`/`PanelTabViewModel`/`StatusBarItemViewModel`）：结构同构——构造接收贡献元数据、解析 `Icon` 几何、透传 `Id`/`Title`；前两者包装 `ToolViewContribution`（ADR-0002），`IconPath` 为 null 时 `Icon` 为 null（各 :15）；后者包装 `IStatusBarItemContribution`，`IconPath` 必填（:14）（见 api.md 第 3 节）。`MenuItemViewModel` 已迁入 Framework（`Core/Framework/Menus/MenuItemViewModel.cs`），本模块经 `using DigitalWorkstation.Core.Framework.Menus` 解析。
- **Contributions/ + Menus/ 六个类**：一个 `IStatusBarItemContribution` 实现类（`Contributions/ReadyStatusBarItem.cs`，命名空间 `DigitalWorkstation.Workstation.Contributions`，纯属性实现）+ 五个 attribute 菜单类（`Menus/`，命名空间 `DigitalWorkstation.Workstation.Menus`，`[MenuGroup]` 类 + `[MenuItem]` 方法），属性/attribute 矩阵见 api.md 第 5 节。工具视图不再是贡献类——声明在 `Views/` 五个 View 类的 `[ToolView]` attribute 上（ADR-0002）。
- **Views/**：`EmptyStateView` 是唯一的静态内容页（键帽样式 `Border.key-chip`、说明样式 `TextBlock.shortcut-desc`，EmptyStateView.axaml:13-26）；其余五个 UserControl 的 axaml 各 14 行占位、code-behind 各带一个 `[ToolView]` attribute（矩阵见 api.md 第 5 节）；`AboutWindow` 16 行静态窗口。
