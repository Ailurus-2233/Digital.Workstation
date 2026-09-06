# Framework — 术语表

| 术语 | 定义 | 首次出现/定义位置 |
|---|---|---|
| **Shell** | 应用主窗口的整体 UI 骨架：ActivityBar + SideBar + MainContent + AuxiliaryPanel + BottomPanel 五区域及菜单栏、状态栏。本模块中 `Shell/` 目录承载其布局状态与贡献收集；渲染层在 Modules/Workstation | `Core/Framework/Shell/`（命名空间 `DigitalWorkstation.Core.Framework.Shell`） |
| **ActivityBar** | 工作区最左侧竖向导航栏，条目来自各模块的 `INavigationItemContribution`；`ShellLayoutState.SelectedActivity` 记录当前选中项 Id | `ShellLayoutState.cs:12`（属性注释）；区域常量定义在 Abstractions `ShellRegions` |
| **SideBar** | ActivityBar 右侧容器，显示当前选中导航项的内容视图；`SideBarState.ContentFor` 记录内容对应的导航项 Id | `Shell/SideBarState.cs:6` |
| **MainContent** | 工作区中央主区域，单视图切换（无 tab）；`MainContentState.ActiveView` 为当前视图 Id（对应 `IMainViewContribution.Id`） | `Shell/MainContentState.cs:6` |
| **AuxiliaryPanel** | 工作区右侧 tab + 容器面板，tab 来自 `IPanelTabContribution`（`PanelPlacement.Auxiliary`） | `Shell/AuxiliaryPanelState.cs:6` |
| **BottomPanel** | 工作区底部 tab + 容器面板（输出、日志等），唯一调高度（而非宽度）的区域 | `Shell/BottomPanelState.cs:6` |
| **启动台 / Splash** | 启动期间显示的进度窗口：呈现模块加载进度（i/N）、单模块失败时提供"继续/退出"决策。由子类经 `CreateSplashWindow()` 提供（真实实现：DashBoard 模块的 `DashBoardWindow`）。与通用 "splash screen" 区别：它是**交互式**的（承载失败决策），且可经菜单重开（`OpenDashBoardMenuItem`） | `FrameworkApplication.cs:58`（`CreateSplashWindow` 抽象方法注释） |
| **贡献（Contribution）** | 模块向 shell 提供的声明式条目（导航项/主视图/面板 tab/菜单项/状态栏项），实现 Abstractions 的 `I*Contribution` 接口并注册到容器；本模块的 `ShellContributionCollector` 负责收集排序。与通用 "插件" 区别：贡献是纯声明 + 视图类型引用，无生命周期钩子 | `Shell/ShellContributionCollector.cs:6-7` |
| **启动序列（Startup Sequence）** | ADR-0004 定义的三阶段引导：CoreServices（登记主窗口、显示启动台、校验模块目录）→ LoadingModules（逐模块异步加载并发布进度）→ Ready（关启动台、显示工作区）。取代 Prism 默认的同步模块加载 | `FrameworkApplication.cs:64` `RunStartupSequenceAsync`；阶段枚举 `StartupPhase` 在 Models 模块 |
| **ADR-0004** | 架构决策记录编号：确立"启动台 + 逐模块异步加载 + 失败可决策"的启动模型，是本模块三个空覆盖与整个 `RunStartupSequenceAsync` 的存在理由 | `FrameworkApplication.cs:32、49、56` 注释 |
| **主窗口（MainWindow）** | 泛型参数 `TWindow` 经 `CreateShell()` 解析出的工作区窗口（真实代码：`Modules/Workstation/MainWindow`）。`FrameworkWindowManager._mainWindow` 在 `HandleMainWindow()` 时捕获；与启动台是**两个不同窗口** | `FrameworkApplication.cs:16、192-195` |
| **窗口注册表（_windowMap）** | `FrameworkWindowManager` 内 `Dictionary<Type, Window>`：同类型窗口单实例的运行时索引，`Closing` 事件自动移除 | `FrameworkWindowManager.cs:17` |
| **reducer / 状态转换** | `ShellLayoutState` 的方法风格：输入当前状态 + 参数，返回新实例，无原地修改、无副作用；非法操作返回等值状态。借自 Redux 术语但无 action 类型层级，方法是直接挂在 record 上的 | `ShellLayoutState.cs:4-6` 注释 |
| **clamp** | `Resize` 把区域尺寸钳制在 `[Min, Max]` 区间的操作，区间常量在各自区域 record 上（如 `SideBarState.MinWidth/MaxWidth`） | `ShellLayoutState.cs:134`（私有 `Clamp` 方法） |
| **PrismApplication / Prism** | 本框架基类来源（`Prism.DryIoc.PrismApplication`）：提供容器（DryIoc）、模块目录（`IModuleCatalog`/`IModuleManager`）、事件聚合器（`IEventAggregator`）、Region 与 ViewModelLocator 基础设施；本模块大量"空覆盖"都是在改 Prism 默认行为 | `FrameworkApplication.cs:12、16` |
| **IoC（类）** | Common 模块的静态容器引用持有者；本模块在 `RegisterFrameworkServices` 里完成其一次性初始化，`FrameworkWindowManager.GetWindow` 经 `IoC.Provider` 解析窗口 | `FrameworkApplication.cs:144`、`FrameworkWindowManager.cs:37` |
| **ViewModel 定位约定** | `ConfigureViewModelLocator` 的命名映射规则：`Views`→`ViewModels` 命名空间替换 + `Window`/`Page`/`View` 后缀补 `ViewModel`/`Model`；View 侧需 `AutoWireViewModel="True"` | `FrameworkApplication.cs:197-233` |

## 类名 ↔ 业务概念对照

| 类/类型名 | 业务概念 |
|---|---|
| `FrameworkApplication<TWindow>` | 桌面应用骨架（主题、容器、模块加载、主窗口生命周期） |
| `FrameworkWindowManager` | 窗口管家（普通窗口 Show/Dialog/Hide/Close + 主窗口登记与显隐） |
| `ShellLayoutState` | 工作区五区域布局的唯一事实来源 |
| `SideBarState`/`AuxiliaryPanelState`/`BottomPanelState`/`MainContentState` | 各区域的可见性、尺寸、内容/活动项快照 |
| `PanelResizeTarget` | 分隔条拖拽的目标区域 |
| `ShellContributionCollector` | shell 装配工：把散落在各模块的贡献按位收齐排序 |
