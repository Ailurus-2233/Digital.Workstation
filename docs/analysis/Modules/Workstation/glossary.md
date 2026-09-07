# Workstation — 术语表

| 术语 | 定义 | 首次/主要代码位置 |
|---|---|---|
| **Shell（外壳）** | 本模块即应用 shell：主窗口 + 布局 + 预置贡献的合称；`Shell/` 目录专放"shell 预置"的贡献类（Id 均以 `shell.` 前缀） | `Shell/` 目录；各贡献类 Id（如 `shell.settings`） |
| **ActivityBar** | 窗口最左 48px 竖条，放导航项按钮；顶部为模块贡献项、底部为设置类入口（VS Code 同源概念） | `Core/Framework/Shell/FrameworkWindowTheme.axaml:23`（ShellActivityBar 部件模板） |
| **SideBar** | ActivityBar 右侧的侧边栏，显示选中导航项的内容视图；`State.SideBar`（`SideBarState`：Visible/Width/ContentFor，默认收起、宽 240、clamp 120-480） | `Core/Framework/Shell/FrameworkWindowTheme.axaml:39`（ShellSideBar）；`MainWindowViewModel.SelectActivity`（:166） |
| **MainContent** | 中央主区，单视图切换；初始为 `EmptyStateView`，被 `OpenMainViewEvent` 整体替换 | `Core/Framework/Shell/FrameworkWindowTheme.axaml:55`（ShellMainContent）；`MainWindowViewModel._mainContent`（:37、:57） |
| **BottomPanel** | 底部面板（tab 栏 + 内容），Ctrl+J 切换；`State.BottomPanel`（默认可见、高 160、clamp 80-480）；水平跨度由面板对齐档位决定（见 CONTEXT.md 与 Framework 文档） | `Core/Framework/Shell/FrameworkWindowTheme.axaml:116`（ShellBottomPanel） |
| **AuxiliaryPanel** | 右侧辅助面板（tab 栏 + 内容），Ctrl+Alt+B 切换；`State.AuxiliaryPanel`（默认可见、宽 280、clamp 120-480） | `Core/Framework/Shell/FrameworkWindowTheme.axaml:67`（ShellAuxiliaryPanel） |
| **Contribution（贡献）** | 模块向 shell 提供界面元素的契约实现：实现 `I*Contribution` 接口并注册进 Prism 容器，由 `ShellContributionCollector` 按 `Order` 收集排序 | `WorkstationApplication.RegisterCustomService`（:20-51）；`MainWindowViewModel.EnsureContributionsLoaded`（:126） |
| **ShellLayoutState** | Framework 定义的不可变布局状态 record；本模块 `MainWindowViewModel.State` 的唯一类型，显隐/尺寸/tab 变更都是它的纯函数转换（面板对齐档位不在其中，由 FrameworkWindow 依赖属性承载） | `MainWindowViewModel.cs:40` |
| **TogglePanelTarget** | 面板显隐目标枚举（Models/Events/：`SideBar`/`AuxiliaryPanel`/`BottomPanel`），`TogglePanelVisibilityEvent` 的负载 | `MainWindowViewModel.TogglePanel`（:293）；`Shell/TogglePanelContribution.cs:15` |
| **PanelResizeTarget** | 拖拽调尺寸目标枚举（Framework/Shell/，同名三成员），`ShellLayoutState.Resize` 的参数 | `MainWindowViewModel.ResizePanel`（:285）；`Core/Framework/Shell/PanelResizer.cs`（Target 属性） |
| **EmptyStateView（空状态页）** | shell 内置的 MainContent 初始内容：快捷键提示页，不依赖任何模块贡献 | `MainWindowViewModel.cs:37`；`Views/EmptyStateView.axaml` |
| **启动台 / Splash** | DashBoard 模块的进度窗（ADR-0004）：模块逐模块加载前由 shell 直接解析显示 | `WorkstationApplication.CreateSplashWindow`（:51-54） |
| **chrome-menu** | 标题栏内嵌的 VS Code 式紧凑顶层菜单样式类（文件/视图/帮助） | `MainWindow.axaml:41-57、67-85` |
| **nav-item / panel-tab / panel-collapse / status-item / region-title / placeholder / key-chip** | 各 UI 元素的样式类约定 | `Core/Framework/Shell/FrameworkWindowTheme.axaml:458` 起（key-chip 在 `Views/EmptyStateView.axaml:13-26`） |
| **sash（分隔条）** | `PanelResizer` 的 UI 概念名：8px 拖拽热区，悬停/拖拽高亮 `ChromeSashHoverBrush` | `Core/Framework/Shell/FrameworkWindowTheme.axaml:499-508`（GridSplitter 样式）；`Core/Framework/Shell/PanelResizer.cs` |
| **呈现模型（ViewModel 包装）** | `NavigationItemViewModel`/`MenuItemViewModel`/`PanelTabViewModel`/`StatusBarItemViewModel`：把贡献接口包装成可绑定对象并预解析 `Icon` 几何 | 各文件类注释 |
| **Order** | 贡献排序权重（int，升序）；shell 预置项取值：导航 0、tab 10/20、关于 10、退出 100、状态栏 10、面板切换 10/20/30、面板对齐 40/50/60/70 | 各贡献类 `Order` 属性 |
| **选中（IsSelected）vs 激活（IsActive）** | `IsSelected` 专用于 ActivityBar 导航项；`IsActive` 专用于面板 tab——两者不同名，勿混用 | `NavigationItemViewModel.cs:30`；`PanelTabViewModel.cs:30` |

## 与通用概念的区别

- 本模块的 **"Shell"** 不是操作系统 shell，而是 VS Code 意义上的应用外壳（workbench shell）。
- **"Settings"（设置）** 目前只是占位：`SettingsNavigationItem` 指向的 `SettingsView` 只有一行"设置（占位）"文本，不是真实设置系统。
- **"Contribution"** 与 Prism 的 Module 不同层级：Module（`IModule`）是加载单元，Contribution 是模块向 shell 注册的界面元素单元；一个模块可贡献多个 Contribution。
- **TogglePanelTarget 与 PanelResizeTarget** 成员同名（SideBar/AuxiliaryPanel/BottomPanel）但定义在不同程序集（Models vs Framework），用途不同（显隐事件负载 vs 拖拽参数），using 时注意不要引错命名空间。
