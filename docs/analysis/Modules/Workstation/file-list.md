# Workstation — 文件结构与功能

相对 `Modules/Workstation/` 的目录树（不含 `obj/`、`Output/` 构建产物）：

```
Workstation.csproj                  项目文件：net10.0；引用 Framework/Resource/UIPackage/DashBoard；3 条 DependentUpon
WorkstationApplication.cs           应用入口：WorkstationApplication : FrameworkApplication<MainWindow>
MainWindow.axaml                    主窗口 XAML（47 行）：FrameworkWindow，只留应用级 chrome（窗口标题/快捷键）
MainWindow.axaml.cs                 主窗口 code-behind：构造函数接线 Opened → EnsureContributionsLoaded
MainWindowViewModel.cs              主窗口 ViewModel（358 行）：布局状态机驱动 + 面板对齐档位 + 贡献收集 + 视图缓存
NavigationItemViewModel.cs          ActivityBar 导航项呈现模型（ObservableObject，IsSelected）
PanelTabViewModel.cs                面板 tab 呈现模型（ObservableObject，IsActive）
StatusBarItemViewModel.cs           状态栏条目呈现模型（普通类）
Contributions/
  SettingsNavigationItem.cs         "设置"导航项贡献（ActivityBar 底部，Order 0）
  PropertiesPanelTab.cs             AuxiliaryPanel"属性"tab 贡献（Order 10）
  OutlinePanelTab.cs                AuxiliaryPanel"大纲"tab 贡献（Order 20）
  OutputPanelTab.cs                 BottomPanel"输出"tab 贡献（Order 10）
  LogPanelTab.cs                    BottomPanel"日志"tab 贡献（Order 20）
  ReadyStatusBarItem.cs             状态栏"就绪"贡献（Order 10）
Menus/
  FileMenus.cs                      文件菜单类（[MenuGroup("MenuFileTitle", Group="Application", GroupOrder=1000, Order=100)]，Exit 方法 Shutdown()）
  ViewPanelMenus.cs                 视图菜单 Panels 组类（三个显隐切换方法发布 TogglePanelVisibilityEvent）
  ViewAlignmentMenus.cs             视图菜单 Alignment 组类（四个对齐方法发布 SetPanelAlignmentEvent）
  HelpMenus.cs                      帮助菜单类（[MenuGroup("MenuHelpTitle", Order=300)]，About 方法 ShowDialog<AboutWindow>）
Views/
  EmptyStateView.axaml(.cs)         shell 内置空状态页：快捷键提示（Ctrl+B/J、Ctrl+Alt+B），不依赖任何模块
  SettingsView.axaml(.cs)           "设置"占位 UserControl（一行 TextBlock"设置（占位）"）
  PropertiesView.axaml(.cs)         "属性"占位 UserControl
  OutlineView.axaml(.cs)            "大纲"占位 UserControl
  OutputView.axaml(.cs)             "输出"占位 UserControl
  LogView.axaml(.cs)                "日志"占位 UserControl
  AboutWindow.axaml(.cs)            "关于"对话框：360×160 不可调大小、CenterOwner，硬编码中文文案
```

## 逐文件说明
- **WorkstationApplication.cs**：`ConfigureModuleCatalog`（:14-17）注册 `DashBoardModule`；`RegisterCustomService`（:19-40）注册全部 shell 预置贡献与内置视图（菜单为一行 `RegisterMenus(typeof(WorkstationApplication).Assembly)`，:35——attribute 扫描注册，ADR-0001）；`CreateSplashWindow`（:45-48）返回 `DashBoardWindow`。
- **MainWindow.axaml**：`win:FrameworkWindow`（1280×800，最小 960×600，`Padding="0,32,0,0"`；`xmlns:win` 指向 `DigitalWorkstation.Core.Framework.Windows`，assembly=DigitalWorkstation.Core.Framework，:18）。**五区布局、状态栏、菜单栏与全部样式已迁出**——布局与 nav-item/panel-tab/panel-collapse/GridSplitter/region-title/placeholder/status-item/chrome-menu 样式在 `Core/Framework/Windows/FrameworkWindowTheme.axaml`，菜单栏（`Menu` + 项模板 + `MenuBarItems` 宽松绑定）由 `FrameworkWindow` 构造函数在代码中内置创建（`Core/Framework/Windows/FrameworkWindow.cs:35-41`）；头部注释（:22-23）指明此分工，本文件只留应用级 chrome。`PanelAlignment="{Binding PanelAlignment, Mode=TwoWay}"`（:11）——面板对齐档位与 VM 镜像属性双向绑定。`Styles`（:25-30）只剩 `u|TitleBar` 底色与窗口背景同色（:27-29）。`KeyBindings`（:32-36）：Ctrl+B→`ToggleSideBarCommand`、Ctrl+J→`ToggleBottomPanelCommand`、Ctrl+Alt+B→`ToggleAuxiliaryPanelCommand`。`TitleBarContent`（:38-46）：居中标题文本，绑 `$parent[u:UrsaWindow].Title`（:44）。
- **MainWindow.axaml.cs**：`MainWindow : FrameworkWindow`（原 `UrsaWindow`）。构造函数（:7-11）在 `InitializeComponent()` 后接线 **`Opened += OnOpened`**——这是 `EnsureContributionsLoaded` 的触发链路起点。`OnOpened`（:13-17）调 `EnsureContributionsLoaded()`（注释说明模块贡献在 Prism 模块初始化即晚于 shell 创建时才注册，首次显示时再收集）。原三个 `OnXxxResizerDragDelta` handler 已删除——方向换算内聚进 Framework 的 `PanelResizer`，code-behind 不再参与拖拽。
- **MainWindowViewModel.cs**：构造（:30-39）；`EnsureContributionsLoaded`（:119-145，菜单为 `MenuTreeBuilder.Build(_collector.GetMenuItems())` 建树 + `MenuItemViewModel.FromSubmenu` → `MenuBarItems`，:137-140）；命令/方法 `SelectActivity`（:151）、`OpenMainView`（:177）、`ActivateAuxTab`（:198）、`ActivateBottomTab`（:215）、`ToggleSideBar`（:232）、`ToggleAuxiliaryPanel`（:241）、`ToggleBottomPanel`（:250）、`SetPanelAlignment`（:259）、`ResizePanel(PanelResize)`（:270）、`TogglePanel`（:278）；私有装载 `LoadPanelTabs`（:291）、`SyncActiveTab`（:327）、`LoadItems`（:350）。字段与缓存字典清单见 reference.md。
- **三个呈现模型**（`NavigationItemViewModel`/`PanelTabViewModel`/`StatusBarItemViewModel`）：结构同构——构造接收贡献接口、`Icon = StreamGeometry.Parse(contribution.IconPath)`、透传 `Id`/`Title`（见 api.md 第 3 节）。`MenuItemViewModel` 已迁入 Framework（`Core/Framework/Menus/MenuItemViewModel.cs`），本模块经 `using DigitalWorkstation.Core.Framework.Menus` 解析。
- **Contributions/ + Menus/ 十个类**：六个 `I*Contribution` 实现类（`Contributions/`，命名空间 `DigitalWorkstation.Workstation.Contributions`，每个 ~15-30 行纯属性实现）+ 四个 attribute 菜单类（`Menus/`，命名空间 `DigitalWorkstation.Workstation.Menus`，`[MenuGroup]` 类 + `[MenuItem]` 方法），属性/attribute 矩阵见 api.md 第 5 节。
- **Views/**：`EmptyStateView` 是唯一的静态内容页（键帽样式 `Border.key-chip`、说明样式 `TextBlock.shortcut-desc`，EmptyStateView.axaml:13-26）；其余五个 UserControl 各 14 行占位；`AboutWindow` 17 行静态窗口。
