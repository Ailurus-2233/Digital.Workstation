# Workstation — 文件结构与功能

相对 `Modules/Workstation/` 的目录树（不含 `obj/`、`Output/` 构建产物）：

```
Workstation.csproj                  项目文件：net10.0；引用 Framework/Resource/UIPackage/DashBoard；3 条 DependentUpon
WorkstationApplication.cs           应用入口：WorkstationApplication : FrameworkApplication<MainWindow>
MainWindow.axaml                    主窗口 XAML（402 行）：UrsaWindow，VS Code 式五区布局 + 样式 + 快捷键
MainWindow.axaml.cs                 主窗口 code-behind：三个 PanelResizer 拖拽 handler + Opened → EnsureContributionsLoaded
MainWindowViewModel.cs              主窗口 ViewModel（340 行）：布局状态机驱动 + 贡献收集 + 视图缓存
PanelResizer.cs                     面板分隔条：GridSplitter 子类，禁用原生重排只留 DragDelta
NavigationItemViewModel.cs          ActivityBar 导航项呈现模型（ObservableObject，IsSelected）
MenuItemViewModel.cs                菜单项呈现模型（普通类，透传 Command）
PanelTabViewModel.cs                面板 tab 呈现模型（ObservableObject，IsActive）
StatusBarItemViewModel.cs           状态栏条目呈现模型（普通类）
Shell/
  TogglePanelContribution.cs        视图菜单"面板显隐切换"贡献（IEventAggregator 发布 TogglePanelVisibilityEvent）
  SettingsNavigationItem.cs         "设置"导航项贡献（ActivityBar 底部，ContentViewType=SettingsView）
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
- **MainWindow.axaml**：`UrsaWindow`（1280×800，最小 960×600，`Padding="0,32,0,0"`）。`KeyBindings`（:159-163）：Ctrl+B→`ToggleSideBarCommand`、Ctrl+J→`ToggleBottomPanelCommand`、Ctrl+Alt+B→`ToggleAuxiliaryPanelCommand`。`LeftContent`（:166-184）三个 `chrome-menu` 顶层菜单（文件/视图/帮助，绑定 `FileMenuItems`/`ViewMenuItems`/`HelpMenuItems`，`ItemTemplate` 用 `MenuItemHeaderTemplate`；子菜单项容器由 `ItemsSource` 生成，其 `Command` 与 `AutomationProperties.Name` 都经样式选择器 `MenuItem.chrome-menu MenuItem` 的 Setter 绑定（:137-140：`Command={Binding Command}`、`AutomationProperties.Name={Binding Title}`））。状态栏 24px（:197-217）：`Border`（`DockPanel.Dock="Bottom"`、`Padding="8,0"`、背景 `ChromeStatusBarBackground`）内 `ItemsControl` 绑定 `StatusBarItems`，`ItemsPanel` 为水平 `StackPanel`，`ItemTemplate` 是内联 `DataTemplate`——`StackPanel.status-item`（间距 4）里 `PathIcon Data="{Binding Icon}"` + `TextBlock Text="{Binding Title}"`，即"图标 + 文本"条目。主网格 `ColumnDefinitions="Auto,Auto,*,Auto"`（:220）逐列落位：**列 0** = ActivityBar（:222-233）：`Width="48"`、`Margin="0,0,2,0"` 的 `Border`，内部 `DockPanel` 中绑定 `BottomNavigationItems` 的 `ItemsControl` 声明 `DockPanel.Dock="Bottom"`，绑定 `TopNavigationItems` 的 `ItemsControl` 不声明 Dock 占剩余空间，两者共用 `NavigationItemTemplate`；**列 1** = SideBar 卡片（:236-249，`Grid.Column="1"`）与 SideBar 分隔条（:252-262，同 `Grid.Column="1"`、`HorizontalAlignment="Right"`、`Margin="0,0,-4,0"`、`ZIndex="1"`——与面板共享同列、叠加其上，不占独立布局列）；**列 2** = 主区嵌套 `Grid`（:264，`RowDefinitions="*,Auto"`，容纳 MainContent 卡片 :266-273、BottomPanel 卡片 :276-322 及其分隔条）；**列 3** = AuxiliaryPanel 卡片（:339-385，`Grid.Column="3"`）与其分隔条（:388-398，同列、`HorizontalAlignment="Left"`、`Margin="-4,0,0,0"`）。BottomPanel 分隔条（:325-335）声明在嵌套 Grid 内 `Grid.Row="1"`（与 BottomPanel 卡片同行、贴其上缘），属性 `Height="8"`、`ResizeDirection="Rows"`、`VerticalAlignment="Top"`、`Margin="0,-4,0,0"`（负上边距外探覆盖上方间隙）、`CornerRadius="4"`、`ZIndex="1"`。分隔条样式（:89-98）：`Style Selector="GridSplitter"` 默认 `Background=Transparent`（常态不可见），`GridSplitter:pointerover` 与 `GridSplitter:dragging` 均设 `Background={DynamicResource ChromeSashHoverBrush}`（交互时整条高亮为强调蓝），选择器注释明确仿 **VS Code `sash.hoverBorder`**。资源模板：`NavigationItemTemplate`（:22-32）——`Button`（`AutomationProperties.Name="{Binding Title}"`、`Classes="nav-item"`、`Classes.selected="{Binding IsSelected}"`、`ToolTip.Tip="{Binding Title}"`）经 `{Binding $parent[ItemsControl].((vm:MainWindowViewModel)DataContext).SelectActivityCommand}` 绑命令、`CommandParameter="{Binding}"` 绑导航项自身，内容为 `PathIcon Data="{Binding Icon}"`；`MenuItemHeaderTemplate`（:34-39）——横向 `StackPanel`（间距 6）里 14×14 `PathIcon Data="{Binding Icon}"` + `TextBlock Text="{Binding Title}"`。
- **MainWindow.axaml.cs**：`MainWindow : UrsaWindow`。构造函数（:9-13）在 `InitializeComponent()` 后接线 **`Opened += OnOpened`**——这是 `EnsureContributionsLoaded` 的触发链路起点。`OnSideBarResizerDragDelta`（:18，`+e.Vector.X`）；`OnAuxiliaryPanelResizerDragDelta`（:26，`-e.Vector.X`，向左拖增宽）；`OnBottomPanelResizerDragDelta`（:34，`-e.Vector.Y`，向上拖增高）；`OnOpened`（:39-43）调 `EnsureContributionsLoaded()`（注释说明模块贡献在 Prism 模块初始化即晚于 shell 创建时才注册，首次显示时再收集）。
- **MainWindowViewModel.cs**：构造（:27-35）；`EnsureContributionsLoaded`（:102-127）；命令/方法 `SelectActivity`（:133）、`OpenMainView`（:159）、`ActivateAuxTab`（:180）、`ActivateBottomTab`（:197）、`ToggleSideBar`（:214）、`ToggleAuxiliaryPanel`（:223）、`ToggleBottomPanel`（:232）、`ResizePanel`（:241）、`TogglePanel`（:249）；私有装载 `LoadChrome`（:259）、`LoadPanelTabs`（:271）、`SyncActiveTab`（:307）、`LoadItems`（:330）。字段与缓存字典清单见 reference.md。
- **PanelResizer.cs**：`StyleKeyOverride => typeof(GridSplitter)`（:14）继承主题；`GetParentGrid() => null`（:20-23）使原生 resize 初始化短路（`ResizeData` 为空），只剩 Thumb 拖拽事件。
- **四个呈现模型**：结构同构——构造接收贡献接口、`Icon = StreamGeometry.Parse(contribution.IconPath)`、透传 `Id`/`Title`；差异仅基类与选中态属性（见 api.md 第 3 节）。
- **Shell/ 九个贡献类**：每个 ~15-30 行的纯属性实现，属性矩阵见 api.md 第 5 节。
- **Views/**：`EmptyStateView` 是唯一的静态内容页（键帽样式 `Border.key-chip`、说明样式 `TextBlock.shortcut-desc`，EmptyStateView.axaml:13-26）；其余五个 UserControl 各 14 行占位；`AboutWindow` 17 行静态窗口。
