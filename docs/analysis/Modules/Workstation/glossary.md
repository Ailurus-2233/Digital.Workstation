# Workstation — 术语表

| 术语 | 定义 | 首次/主要代码位置 |
|---|---|---|
| **Shell（外壳）** | 本模块即应用 shell：主窗口 + 布局 + 预置贡献的合称；预置类分两目录——`Contributions/`（六个贡献类，Id 均以 `shell.` 前缀）与 `Menus/`（四个 attribute 菜单类，无 Id） | `Contributions/`、`Menus/` 目录；各贡献类 Id（如 `shell.settings`） |
| **ActivityBar** | 窗口最左 48px 竖条，放导航项按钮；顶部为模块贡献项、底部为设置类入口（VS Code 同源概念） | `Core/Framework/Windows/FrameworkWindowTheme.axaml:23`（ShellActivityBar 部件模板） |
| **SideBar** | ActivityBar 右侧的侧边栏，显示选中导航项的内容视图；`State.SideBar`（`SideBarState`：Visible/Width/ContentFor，默认收起、宽 240、clamp 120-480） | `Core/Framework/Windows/FrameworkWindowTheme.axaml:39`（ShellSideBar）；`MainWindowViewModel.SelectActivity`（:151） |
| **MainContent** | 中央主区，单视图切换；初始为 `EmptyStateView`，被 `OpenMainViewEvent` 整体替换 | `Core/Framework/Windows/FrameworkWindowTheme.axaml:55`（ShellMainContent）；`MainWindowViewModel._mainContent`（:38、:58） |
| **BottomPanel** | 底部面板（tab 栏 + 内容），Ctrl+J 切换；`State.BottomPanel`（默认可见、高 160、clamp 80-480）；水平跨度由面板对齐档位决定（见 CONTEXT.md 与 Framework 文档） | `Core/Framework/Windows/FrameworkWindowTheme.axaml:116`（ShellBottomPanel） |
| **AuxiliaryPanel** | 右侧辅助面板（tab 栏 + 内容），Ctrl+Alt+B 切换；`State.AuxiliaryPanel`（默认可见、宽 280、clamp 120-480） | `Core/Framework/Windows/FrameworkWindowTheme.axaml:67`（ShellAuxiliaryPanel） |
| **Contribution（贡献）** | 模块向 shell 提供界面元素的契约：导航/主视图/面板 tab/状态栏实现 `I*Contribution` 接口并注册进 Prism 容器，由 `ShellContributionCollector` 按 `Order` 收集排序；菜单贡献（ADR-0001）改为 attribute 菜单类（`[MenuGroup]` 类 + `[MenuItem]` 方法），由 Framework `MenuRegistration.RegisterMenus` 扫描注册、`MenuTreeBuilder` 建树排序 | `WorkstationApplication.RegisterCustomService`（:19-40）；`MainWindowViewModel.EnsureContributionsLoaded`（:119） |
| **ShellLayoutState** | Framework 定义的不可变布局状态 record（Framework/Layout/）；本模块 `MainWindowViewModel.State` 的唯一类型，显隐/尺寸/tab 变更都是它的纯函数转换（面板对齐档位不在其中，由 FrameworkWindow 依赖属性承载） | `MainWindowViewModel.cs:43` |
| **TogglePanelTarget** | 面板显隐目标枚举（Models/Events/：`SideBar`/`AuxiliaryPanel`/`BottomPanel`），`TogglePanelVisibilityEvent` 的负载 | `MainWindowViewModel.TogglePanel`（:278）；`Menus/ViewPanelMenus.cs`（三个 `[MenuItem]` 方法各发布一值） |
| **PanelResizeTarget** | 拖拽调尺寸目标枚举（Framework/Layout/，同名三成员），`ShellLayoutState.Resize` 的参数 | `MainWindowViewModel.ResizePanel`（:270）；`Core/Framework/Layout/PanelResizer.cs`（Target 属性） |
| **EmptyStateView（空状态页）** | shell 内置的 MainContent 初始内容：快捷键提示页，不依赖任何模块贡献 | `MainWindowViewModel.cs:38`；`Views/EmptyStateView.axaml` |
| **启动台 / Splash** | DashBoard 模块的进度窗（ADR-0004）：模块逐模块加载前由 shell 直接解析显示 | `WorkstationApplication.CreateSplashWindow`（:45-48） |
| **chrome-menu** | 标题栏内嵌的 VS Code 式紧凑菜单样式类：菜单栏是 `FrameworkWindow` 的内置行为（`Core/Framework/Windows/FrameworkWindow.cs:35-41` 代码创建 `Menu` 并宽松绑定 `MenuBarItems`），样式在 Framework 主题 | `Core/Framework/Windows/FrameworkWindowTheme.axaml:551-569` |
| **nav-item / panel-tab / panel-collapse / status-item / region-title / placeholder / key-chip** | 各 UI 元素的样式类约定 | `Core/Framework/Windows/FrameworkWindowTheme.axaml:458` 起（key-chip 在 `Views/EmptyStateView.axaml:13-26`） |
| **sash（分隔条）** | `PanelResizer` 的 UI 概念名：8px 拖拽热区，悬停/拖拽高亮 `ChromeSashHoverBrush` | `Core/Framework/Windows/FrameworkWindowTheme.axaml:499-508`（GridSplitter 样式）；`Core/Framework/Layout/PanelResizer.cs` |
| **呈现模型（ViewModel 包装）** | `NavigationItemViewModel`/`PanelTabViewModel`/`StatusBarItemViewModel`：把贡献接口包装成可绑定对象并预解析 `Icon` 几何；菜单呈现模型 `MenuItemViewModel` 已迁入 Framework（`Core/Framework/Menus/MenuItemViewModel.cs`，由菜单树经 `FromSubmenu` 递归转换） | 各文件类注释 |
| **Order** | 贡献排序权重（int，升序）：导航/tab/状态栏贡献按 collector 的 Order 排序；菜单（ADR-0001）有三级位次——`NodeOrder`（顶层/末端子菜单节点）、`GroupOrder`（子菜单内分组）、`Order`（组内条目），同值取最小声明/字典序。shell 预置取值：导航 0、tab 10/20、状态栏 10；菜单：文件顶层 100/视图 200/帮助 300，退出与关于项 Order 100，面板切换 100/200/300，对齐 100/200/300/400 | 各贡献类 `Order` 属性与菜单类 attribute |
| **选中（IsSelected）vs 激活（IsActive）** | `IsSelected` 专用于 ActivityBar 导航项；`IsActive` 专用于面板 tab——两者不同名，勿混用 | `NavigationItemViewModel.cs:30`；`PanelTabViewModel.cs:30` |

## 与通用概念的区别

- 本模块的 **"Shell"** 不是操作系统 shell，而是 VS Code 意义上的应用外壳（workbench shell）。
- **"Settings"（设置）** 目前只是占位：`SettingsNavigationItem` 指向的 `SettingsView` 只有一行"设置（占位）"文本，不是真实设置系统。
- **"Contribution"** 与 Prism 的 Module 不同层级：Module（`IModule`）是加载单元，Contribution 是模块向 shell 注册的界面元素单元；一个模块可贡献多个 Contribution。
- **TogglePanelTarget 与 PanelResizeTarget** 成员同名（SideBar/AuxiliaryPanel/BottomPanel）但定义在不同程序集（Models vs Framework），用途不同（显隐事件负载 vs 拖拽参数），using 时注意不要引错命名空间。
