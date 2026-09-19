# Framework — 术语表

| 术语 | 定义 | 首次出现/定义位置 |
|---|---|---|
| **Shell** | 应用主窗口的整体 UI 骨架：ActivityBar + SideBar + MainContent + AuxiliaryPanel + BottomPanel 五区域及菜单栏、状态栏。本模块中相关代码按职责分四个目录：`Layout/`（布局状态机与分隔条）、`Menus/`（菜单建树/注册/呈现模型）、`Contributions/`（贡献收集）、`Windows/`（窗口基类与主题）；类型名保留 Shell 前缀（`ShellLayoutState`、`ShellContributionCollector`、模板资源键 `ShellActivityBar` 等）只是历史命名。渲染层的应用级 chrome 在 Modules/Workstation | `Core/Framework/`（命名空间 `DigitalWorkstation.Core.Framework.{Layout,Menus,Contributions,Windows}`） |
| **ActivityBar** | 工作区最左侧竖向导航栏，条目来自各模块的工具视图（`[ToolView]` View 类，`ToolViewPlacement.ActivityBar`，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）；`ShellLayoutState.SelectedActivity` 记录当前选中项 Id，`ActivityBarItems` 记录顶部段有序 Id（钉住项在底部段、不入列）；底部段另有一枚 shell 内置"设置"导航按钮（纯导航，发布 `OpenMainViewEvent`，[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 6） | `ShellLayoutState.cs:14、20`（属性注释）；区域常量定义在 Abstractions `ShellRegions` |
| **SideBar** | ActivityBar 右侧容器，显示当前选中工具视图的内容视图；`SideBarState.ContentFor` 记录内容对应的工具视图 Id | `Layout/SideBarState.cs:6` |
| **MainContent** | 工作区中央主区域，单视图切换（无 tab）；`MainContentState.ActiveView` 为当前视图 Id（对应 `IMainViewContribution.Id`） | `Layout/MainContentState.cs:6` |
| **AuxiliaryPanel** | 工作区右侧 tab + 容器面板，tab 来自工具视图（`ToolViewPlacement.AuxiliaryPanel`） | `Layout/AuxiliaryPanelState.cs:6` |
| **BottomPanel** | 工作区底部 tab + 容器面板（输出、日志等），唯一调高度（而非宽度）的区域 | `Layout/BottomPanelState.cs:6` |
| **启动台 / Splash** | 启动期间显示的进度窗口：呈现模块加载进度（i/N）、单模块失败时提供"继续/退出"决策。由子类经 `CreateSplashWindow()` 提供（真实实现：DashBoard 模块的 `DashBoardWindow`）。与通用 "splash screen" 区别：它是**交互式**的（承载失败决策）。启动台只在启动序列显示，当前无重开通路（原菜单项已删除） | `FrameworkApplication.cs:62`（`CreateSplashWindow` 抽象方法注释） |
| **贡献（Contribution）** | 模块向 shell 提供的声明式条目（工具视图/主视图/菜单项/状态栏项）。工具视图在 View 类上标 `ToolViewAttribute` 由 `RegisterToolViews` 扫描生成 `ToolViewContribution`（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)），其余实现 Abstractions 的 `I*Contribution` 接口并注册到容器；本模块的 `ShellContributionCollector` 负责收集排序。与通用 "插件" 区别：贡献是纯声明 + 视图类型引用，无生命周期钩子 | `Contributions/ShellContributionCollector.cs:6-7` |
| **工具视图（Tool View）** | 带图标与标题的可停靠单元，View 类的 ToolViewAttribute 声明 Id/ResourceType/TitleKey/Icon/Default/Order/AllowMove；扫描时按显式来源解析标题、生成元数据并注册 View 类型。默认归属三处 Bar，用户移动后以持久化布局为准 | Abstractions/Contributions/ToolViewAttribute.cs；Contributions/ToolViewRegistration.cs |
| **钉住项（Pinned Item）** | `AllowMove = false` 且 `Default = ActivityBar` 的工具视图，固定渲染在 ActivityBar 底部段的钉住区，不参与拖拽迁移；机制保留但**当前全仓无钉住项实例**（原"设置"钉住项已改为 shell 内置纯导航按钮，[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md)） | `Core/Abstractions/Contributions/ToolViewAttribute.cs`（`AllowMove` 注释，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)） |
| **启动序列（Startup Sequence）** | [ADR-0004](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md) 定义的三阶段引导：CoreServices（登记主窗口、显示启动台、校验模块目录）→ LoadingModules（逐模块异步加载并发布进度）→ Ready（关启动台、显示工作区）。取代 Prism 默认的同步模块加载 | `FrameworkApplication.cs:68` `RunStartupSequenceAsync`；阶段枚举 `StartupPhase` 在 Models 模块 |
| **[ADR-0004](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md)** | 架构决策记录编号：确立"启动台 + 逐模块异步加载 + 失败可决策"的启动模型，是本模块三个空覆盖与整个 `RunStartupSequenceAsync` 的存在理由 | `FrameworkApplication.cs:35-38、44-47、52-54` 注释 |
| **主窗口（MainWindow）** | 泛型参数 `TWindow` 经 `CreateShell()` 解析出的工作区窗口（真实代码：`Modules/Workstation/MainWindow`）。`FrameworkWindowManager._mainWindow` 在 `HandleMainWindow()` 时捕获；与启动台是**两个不同窗口** | `FrameworkApplication.cs:20、224-227` |
| **窗口注册表（_windowMap）** | `FrameworkWindowManager` 内 `Dictionary<Type, Window>`：同类型窗口单实例的运行时索引，`Closing` 事件自动移除 | `FrameworkWindowManager.cs:17` |
| **reducer / 状态转换** | `ShellLayoutState` 的方法风格：输入当前状态 + 参数，返回新实例，无原地修改、无副作用；非法操作返回等值状态。借自 Redux 术语但无 action 类型层级，方法是直接挂在 record 上的 | `ShellLayoutState.cs:4-6` 注释 |
| **clamp** | `Resize` 把区域尺寸钳制在 `[Min, Max]` 区间的操作，区间常量在各自区域 record 上（如 `SideBarState.MinWidth/MaxWidth`） | `ShellLayoutState.cs:134`（私有 `Clamp` 方法） |
| **PrismApplication / Prism** | 本框架基类来源（`Prism.DryIoc.PrismApplication`）：提供容器（DryIoc）、模块目录（`IModuleCatalog`/`IModuleManager`）、事件聚合器（`IEventAggregator`）、Region 与 ViewModelLocator 基础设施；本模块大量"空覆盖"都是在改 Prism 默认行为 | `FrameworkApplication.cs:16、20` |
| **IoC（类）** | Common 模块的静态容器引用持有者；本模块在 `RegisterFrameworkServices` 里完成其一次性初始化，`FrameworkWindowManager.GetWindow` 经 `IoC.Provider` 解析窗口 | `FrameworkApplication.cs:148`、`FrameworkWindowManager.cs:37` |
| **ViewModel 定位约定** | `ConfigureViewModelLocator` 的命名映射规则：`Views`→`ViewModels` 命名空间替换 + `Window`/`Page`/`View` 后缀补 `ViewModel`/`Model`；View 侧需 `AutoWireViewModel="True"` | `FrameworkApplication.cs:237-269` |
| **面板对齐（Panel Alignment）** | BottomPanel 在窗口底部的水平跨度，类比文本对齐。四档：左（贴左，横跨 SideBar 与 MainContent 下方，AuxiliaryPanel 通高到底）、右（贴右，横跨 MainContent 与 AuxiliaryPanel 下方，SideBar 通高到底）、居中（仅占 MainContent 下方，默认，两侧栏通高）、两端（横跨三列全宽）。完整领域定义（含与"面板位置"的区分）见根目录 CONTEXT.md | `Layout/PanelAlignment.cs:7` |
| **FrameworkWindow** | 带基础布局的窗口基类（继承 UrsaWindow）：内置 VS Code 式五区 shell + 状态栏 + 标题栏菜单栏（代码创建的 `Menu` 宽松绑定 ViewModel 的 `MenuBarItems`，[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)），布局档位由 `PanelAlignment` 依赖属性决定，切换即整体替换布局模板。真实子类：Modules/Workstation 的 `MainWindow`。与"主窗口（MainWindow）"词条的区别：那是**角色**（启动序列登记的那个窗口实例），这是**类型基类** | `Windows/FrameworkWindow.cs:19` |
| **布局持久化（Layout Persistence）** | layout.json 的独立 DTO 记录工具视图位置、显隐、尺寸、选择和面板对齐；容错读取由 LayoutPersistence 负责，防抖写入与重置删除由统一配置写入模块负责。重置等待在途写入，正常 Exit 完成保存 | Layout/LayoutPersistence.cs、Persistence/ |
| **拖拽会话（Drag Session）** | 一次工具视图拖拽的存续期：`ToolViewButton` 在 `DoDragDropAsync` 期间把 `ToolViewDragSession.IsActive` 置 true 并广播 `ActiveChanged`；shell 借此临时显露隐藏面板作为投放区 | `Layout/ToolViewDragSession.cs:11` |
| **命令（Command）** | 以标题与可选快捷键（Gesture）声明的全局动作（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）：方法标 `CommandAttribute` 经 `RegisterCommands` 扫描生成 `ICommandContribution`（扁平模型，稳定 `Id` 默认「声明类全名.方法名」），与菜单体系互不相干；完整领域定义见根目录 CONTEXT.md | `Core/Abstractions/Commands/CommandAttribute.cs`；注册端 `Commands/CommandRegistration.cs:14` |
| **命令面板（Command Palette）** | 窗口顶部居中的命令检索浮层（Ctrl+P 唤起）：子串过滤、↑↓/Enter/Esc 导航、单击执行、失焦关闭、MRU 内存置顶。自包含控件 `CommandPalette`，VM 只暴露 `Commands` 集合（宽松绑定）；`FrameworkWindow` 构造时内置 | `Windows/CommandPalette.cs:18`、`Windows/FrameworkWindow.cs:36-44` |
| **MRU（最近使用）** | 命令面板内 `_recentIds` 记忆：最近执行的命令 Id（新者在前）浮到列表最前；只在内存中，重启即清（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md) 决策 8，持久化留作后续） | `Windows/CommandPalette.cs:29` |
| **命令手势（Gesture）** | 命令的快捷键文本（如 `"Ctrl+Shift+P"`）：`CommandAttribute.Gesture` 声明 → shell 收集后 `FrameworkWindow.RegisterCommandGestures` 解析为窗口级 KeyBinding。机制在 Framework、接线在 shell 模块（同 LayoutPersistence 惯例） | `Windows/FrameworkWindow.cs:114` |
| **占位线（Insertion Line）** | 拖拽落点指示：`ToolViewBar` ControlTheme 模板里的 `PART_InsertionLine`（2px、取分隔条悬停高亮色），DragOver 时按指针位置移到落点缝隙——横向 Bar 竖线、纵向 Bar 横线；不改现有 tab 的样式 | `Windows/FrameworkWindowTheme.axaml:492`（ControlTheme）、`Layout/ToolViewBar.cs:185`（`ShowInsertion`） |
| **菜单路径引用 / 标题声明** | `MenuGroup(path)` 只挂接稳定路径，不需要所有者资源；`MenuGroup(path, resourceType, titleKey)` 另为末端节点声明标题。建树采用首个非 null PathTitle，无声明时显示 Id 段 | Menus/MenuRegistration.cs、Menus/MenuTreeBuilder.cs |
| **FrameworkResources** | Framework 自己的资源所属类型，包含命令面板及常规/语言设置文案；由 ResourceText 查询，不是其他模块文案的共享仓库 | Resources/FrameworkResources.* |

## 类名 ↔ 业务概念对照

| 类/类型名 | 业务概念 |
|---|---|
| `FrameworkApplication<TWindow>` | 桌面应用骨架（主题、容器、模块加载、主窗口生命周期） |
| `FrameworkWindowManager` | 窗口管家（普通窗口 Show/Dialog/Hide/Close + 主窗口登记与显隐） |
| `ShellLayoutState` | 工作区五区域布局的唯一事实来源 |
| `SideBarState`/`AuxiliaryPanelState`/`BottomPanelState`/`MainContentState` | 各区域的可见性、尺寸、内容/活动项快照 |
| `PanelResizeTarget` | 分隔条拖拽的目标区域 |
| `ShellContributionCollector` | shell 装配工：把散落在各模块的贡献按位收齐排序 |
| `FrameworkWindow` / `FrameworkWindowTheme` | 自带五区骨架的窗口基类 / 它的布局模板与样式包 |
| `PanelAlignment` | 面板对齐：BottomPanel 的水平跨度档位 |
| `ToolViewRegistration` | 工具视图装配扫描器：attribute 扫描生成元数据并注册 View 类型 |
| `PanelResize` / `PanelResizer` | 分隔条拖拽的一次增量（命令参数）/ 发出增量的分隔条控件 |
| `LayoutPersistence` | 布局配置的读取与格式校验；写入/删除委托统一文件模块 |
| `ConfigurationPersistence` | 配置写入生命周期的所有者：统一刷新与退出收尾 |
| `DebouncedJsonFile<T>` | 内部单文件实现：快照防抖、串行提交/删除、失败保留 |
| `ShellLayoutDto` 族 | layout.json 的落盘格式：带版本字段、独立于运行时状态机的 DTO record |
| `ToolViewButton` / `ToolViewBar` | 工具视图的拖拽源按钮（tab 头/导航项）/ Bar 投放目标（算落点、显示占位线、发出 `ToolViewMove`） |
| `ToolViewMove` / `ToolViewDragSession` | 一次拖拽落点的命令参数 / 拖拽进行中的全局信号 |

| **贡献批次（ContributionBatch）** | 一次模块加载产生的 Shell 贡献登记边界，准备成功后发布，失败后不可见；不等于 DI 事务 | Contributions/ShellContributionCatalog.cs |
| **有效设置目录（SettingCatalog）** | 按 Id 首个生效的设置项与分组解释，设置服务和页面共享 | Settings/SettingCatalog.cs |
| **布局配置投影** | ShellLayoutConfiguration 在独立 DTO 与 State 之间转换，不读取 UI 集合 | Layout/ShellLayoutConfiguration.cs |
| **共享布局尺寸** | ShellLayoutMetrics 为卡片模板和列宽提供同一外边距来源 | Layout/ShellLayoutMetrics.cs |
