# Workstation — 文件结构与功能

相对 `Modules/Workstation/` 的目录树（不含 `obj/`、`Output/` 构建产物）：

```
Workstation.csproj                  项目文件：net10.0；引用 Framework/Resource/UIPackage/DashBoard；3 条 DependentUpon
WorkstationApplication.cs           应用入口：WorkstationApplication : FrameworkApplication<MainWindow>
MainWindow.axaml                    主窗口 XAML（95 行）：FrameworkWindow，只留应用级 chrome（菜单/标题/快捷键）
MainWindow.axaml.cs                 主窗口 code-behind：构造函数接线 Opened → EnsureContributionsLoaded
MainWindowViewModel.cs              主窗口 ViewModel（400 行）：布局状态机驱动 + 面板对齐档位 + 贡献收集 + 视图缓存
NavigationItemViewModel.cs          ActivityBar 导航项呈现模型（ObservableObject，IsSelected）
MenuItemViewModel.cs                菜单项呈现模型（普通类，透传 Command）
PanelTabViewModel.cs                面板 tab 呈现模型（ObservableObject，IsActive）
StatusBarItemViewModel.cs           状态栏条目呈现模型（普通类）
Shell/
  TogglePanelContribution.cs        视图菜单"面板显隐切换"贡献（IEventAggregator 发布 TogglePanelVisibilityEvent）
  PanelAlignmentContribution.cs     视图菜单"面板对齐"贡献 ×4 实例（IEventAggregator 发布 SetPanelAlignmentEvent）
  PropertiesPanelTab.cs             AuxiliaryPanel"属性"tab 贡献（Order 10）
  OutlinePanelTab.cs                AuxiliaryPanel"大纲"tab 贡献（Order 20）
  OutputPanelTab.cs                 BottomPanel"输出"tab 贡献（Order 10）
  LogPanelTab.cs                    BottomPanel"日志"tab 贡献（Order 20）
  ExitMenuItem.cs                   文件菜单"退出"贡献（Shutdown()，Order 100）
  AboutMenuItem.cs                  帮助菜单"关于"贡献（IWindowManager.ShowDialog<AboutWindow>，Order 10）
  ReadyStatusBarItem.cs             状态栏"就绪"贡献（Order 10）
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

- **WorkstationApplication.cs**：`ConfigureModuleCatalog`（:14-17）注册 `DashBoardModule`；`RegisterCustomService`（:19-46）注册全部 shell 预置贡献与内置视图（含 `TogglePanelTarget` 三值循环注册 `TogglePanelContribution`）；`CreateSplashWindow`（:51-54）返回 `DashBoardWindow`。
- **MainWindow.axaml**：`shell:FrameworkWindow`（1280×800，最小 960×600，`Padding="0,32,0,0"`；`xmlns:shell` 指向 `DigitalWorkstation.Core.Framework.Shell`，assembly=DigitalWorkstation.Core.Framework，:18）。**五区布局、状态栏与 nav-item/panel-tab/panel-collapse/GridSplitter/region-title/placeholder/status-item 样式、`NavigationItemTemplate` 已全部迁出**到 `Core/Framework/Shell/FrameworkWindowTheme.axaml`（注释 :23-24 指明此分工），本文件只留应用级 chrome。新增 `PanelAlignment="{Binding PanelAlignment, Mode=TwoWay}"`（:11）——面板对齐档位与 VM 镜像属性双向绑定。`KeyBindings`（:60-64）：Ctrl+B→`ToggleSideBarCommand`、Ctrl+J→`ToggleBottomPanelCommand`、Ctrl+Alt+B→`ToggleAuxiliaryPanelCommand`。`LeftContent`（:67-85）三个 `chrome-menu` 顶层菜单（文件/视图/帮助，绑定 `FileMenuItems`/`ViewMenuItems`/`HelpMenuItems`，`ItemTemplate` 用 `MenuItemHeaderTemplate`；子菜单项容器由 `ItemsSource` 生成，其 `Command` 与 `AutomationProperties.Name` 都经样式选择器 `MenuItem.chrome-menu MenuItem` 的 Setter 绑定（:51-54：`Command={Binding Command}`、`AutomationProperties.Name={Binding Title}`））。`TitleBarContent`（:86-94）：居中标题文本，绑 `$parent[u:UrsaWindow].Title`（:92）。`Styles`（:36-58）：`u|TitleBar` 底色与窗口背景同色（:38-40）、`chrome-menu` 紧凑行高（:42-45）、弹出层 `VerticalOffset=-8` 贴合标题栏下缘（:47-49）、子菜单项命令绑定（:51-54）、菜单 `PathIcon` 前景色（:55-57）。`Resources`（:26-34）仅留 `MenuItemHeaderTemplate`——横向 `StackPanel`（间距 6）里 14×14 `PathIcon Data="{Binding Icon}"` + `TextBlock Text="{Binding Title}"`。
- **MainWindow.axaml.cs**：`MainWindow : FrameworkWindow`（原 `UrsaWindow`）。构造函数（:7-11）在 `InitializeComponent()` 后接线 **`Opened += OnOpened`**——这是 `EnsureContributionsLoaded` 的触发链路起点。`OnOpened`（:13-17）调 `EnsureContributionsLoaded()`（注释说明模块贡献在 Prism 模块初始化即晚于 shell 创建时才注册，首次显示时再收集）。原三个 `OnXxxResizerDragDelta` handler 已删除——方向换算内聚进 Framework 的 `PanelResizer`，code-behind 不再参与拖拽。
- **MainWindowViewModel.cs**：构造（:29-38）；`EnsureContributionsLoaded`（:126-152，含视图菜单分隔符插入 :148-156）；命令/方法 `SelectActivity`（:166）、`OpenMainView`（:192）、`ActivateAuxTab`（:213）、`ActivateBottomTab`（:230）、`ToggleSideBar`（:247）、`ToggleAuxiliaryPanel`（:256）、`ToggleBottomPanel`（:265）、`SetPanelAlignment`（:274）、`ResizePanel(PanelResize)`（:285）、`TogglePanel`（:293）；私有装载 `LoadChrome`（:303）、`LoadPanelTabs`（:315）、`SyncActiveTab`（:351）、`LoadItems`（:374）。字段与缓存字典清单见 reference.md。
- **四个呈现模型**：结构同构——构造接收贡献接口、`Icon = StreamGeometry.Parse(contribution.IconPath)`、透传 `Id`/`Title`；差异仅基类与选中态属性（见 api.md 第 3 节）。
- **Shell/ 十个贡献类**：每个 ~15-30 行的纯属性实现，属性矩阵见 api.md 第 5 节（`PanelAlignmentContribution` 与 `TogglePanelContribution` 同构，按档位/目标各注册多实例）。
- **Views/**：`EmptyStateView` 是唯一的静态内容页（键帽样式 `Border.key-chip`、说明样式 `TextBlock.shortcut-desc`，EmptyStateView.axaml:13-26）；其余五个 UserControl 各 14 行占位；`AboutWindow` 17 行静态窗口。
