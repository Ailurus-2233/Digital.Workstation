# DashBoard — 术语表

| 术语 | 定义 | 首次出现位置 |
|---|---|---|
| **启动台 / Launch Pad / DashBoard** | 本模块的核心窗口 `DashBoardWindow`：应用启动期间的进度窗，显示阶段名与模块 i/N 进度，失败时提供"继续/退出"决策。英文资源名是 "Launch Pad"（Language.en-US.resx `DashBoardNavigationTitle`="Launch Pad"），中文为"启动台"。与通用 "dashboard"（仪表盘）无关——本模块不是数据看板 | `Views/Windows/DashBoardWindow.axaml:5` `Title="启动台"`；README 第 24 行 |
| **贡献（Contribution）** | 模块向 shell 提供的声明式条目：工具视图（导航项/面板 tab 的统一概念，ADR-0002）为 View 类上的 `[ToolView]` attribute，由 Framework `RegisterToolViews` 反射扫描生成 `ToolViewContribution` 元数据；主视图/状态栏为实现 `IMainViewContribution`/`IStatusBarItemContribution` 接口并注册到容器的纯属性类；菜单项为 `[MenuGroup]`/`[MenuItem]` attribute 标记的菜单类，由 Framework `RegisterMenus` 反射注册（ADR-0001）。均由 shell 收集排序/建树渲染。本模块现仅贡献状态栏项一处（"启动台"）；原工具视图两项（"启动台"、"任务"）、主视图两项、菜单项均已删除 | `DashBoardModule.cs:12-13`；`ToolViewAttribute`/接口定义在 Core/Abstractions/Contributions/，菜单 attribute 在 Core/Abstractions/Menus/ |
| **工具视图（Tool View）** | ADR-0002 统一的 shell 可停靠内容概念：ActivityBar 项与 AuxiliaryPanel/BottomPanel tab 的统称，由 View 类上的 `[ToolView(id, titleKey)]` 声明（`Icon`/`Default` 缺省 `AuxiliaryPanel`/`Order`/`AllowMove` 缺省 `true`），扫描生成的元数据类型为 `ToolViewContribution`；实际落位由用户布局决定，`Default` 仅作兜底。本模块原有两个实例（"启动台"、"任务"）已删除，当前无 `[ToolView]` 标注类 | 机制定义在 Core/Abstractions/Contributions/ToolViewAttribute.cs |
| **tracer bullet（曳光弹）** | 架构验证手法：用一个端到端最小实现打穿整条通路。本模块的 shell 贡献项历史上是通路验证探针（原 `DashBoardNavigationView` 类注释自称 tracer bullet）；演示贡献已随占位清理删除，仅状态栏项保留 | 历史：`Views/DashBoardNavigationView.axaml.cs`（已删除） |
| **ADR-0002** | 架构决策记录：「工具视图统一为 ToolView：Attribute 声明、跨 Bar 拖拽、布局持久化」（docs/adr/0002-toolview-drag-persistence.md）。`INavigationItemContribution`/`IPanelTabContribution` 合并删除，改为 View 类上的 `[ToolView]` attribute + `RegisterToolViews(Assembly)` 扫描注册；工具视图可在三处 Bar 间拖拽，布局持久化 | `DashBoardModule.cs:10-11` 注释（两个 `[ToolView]` 标注处已随演示视图删除） |
| **ADR-0005** | 架构决策记录：「命令体系：Attribute 注册、全局命令列表与命令面板」（docs/adr/0005-command-registration-palette.md）。`CommandAttribute`（标方法、免类级 attribute）+ `ICommandContribution` + Framework `RegisterCommands` 扫描注册；命令面板（Ctrl+P）检索执行；命令与菜单两套独立声明 | 本模块已无命令贡献（原 `DashBoardCommands.cs` 已删除）；真实样例见 `Modules/Workstation/Commands/ViewCommands.cs` |
| **ADR-0004** | 架构决策记录编号：确立"启动台 + 逐模块异步加载 + 失败可决策"的启动模型。是 `OnInitialized` 空实现与整个启动台存在方式的理由。**ADR 文档本体不在仓库中**（悬空引用） | `DashBoardModule.cs:18` 注释；README 第 47-51 行 |
| **启动序列（Startup Sequence）** | Core/Framework `FrameworkApplication.RunStartupSequenceAsync` 的三阶段引导：CoreServices → LoadingModules → Ready。启动台是它的可视化前端 | 本模块侧体现为 `DashBoardWindowViewModel.OnProgress` 的 `StartupPhase` switch（DashBoardWindowViewModel.cs:41-47） |
| **i/N 进度** | 逐模块加载计数显示"第 i 个/共 N 个"，格式 `{moduleName}（{index}/{count}）`（全角括号），由 `FormatModuleText` 生成；`ModuleIndex` 从 1 开始 | `DashBoardWindowViewModel.cs:61-64` |
| **MainContent 单视图切换** | 工作区中央区域同时只承载一个主视图，`OpenMainViewEvent` 到达后整体替换（非多文档 tabs）。本模块的"概览/最近项目"两个演示主视图已删除；现存实例为 Settings 模块的设置页（`WellKnownViews.Settings`，ADR-0006） | `MainWindowViewModel.OpenMainView`（Modules/Workstation） |
| **ActivityBar / SideBar / BottomPanel / 状态栏** | shell 工作区的四个可贡献区域：左侧竖条导航、侧边栏内容、底部面板、底部状态栏。本模块仅贡献状态栏一项（原 ActivityBar/BottomPanel 演示项已删除） | `DashBoardStatusBarItem.cs` |
| **AutoWireViewModel** | Prism ViewModelLocator 的约定装配开关：按 `Views.*.XxxWindow` ↔ `ViewModels.*.XxxWindowViewModel` 命名/目录约定自动解析并设置 DataContext | `DashBoardWindow.axaml:3` |

## 与同名通用概念的区别

- **DashBoard ≠ 仪表盘**：名字来自通用 "dashboard"，但本模块实例是**启动进度窗 + 状态栏"启动台"条目**，不含任何图表/指标。
- **Splash vs 启动台**：代码注释与 Models 资源键用 "Splash" 前缀（`Language.SplashStartingText` 等），窗口与模块名用 DashBoard/启动台——两者指同一个窗口；Framework 的抽象钩子叫 `CreateSplashWindow`。
- **SetProgress（历史误称）**：上游 Core/Framework 深读期间曾用 `SetProgress` 指称进度回调；该成员从未存在于本模块（main @ 04cfd02 及全部历史），真实方法名为 `OnProgress`（进度）与 `OnModuleFailed`（失败）。

## 代码命名 ↔ 领域词汇对应

| 代码命名 | 业务/领域概念 |
|---|---|
| `DashBoardModule` | 模块入口（Prism `IModule`） |
| `DashBoardWindow` / `DashBoardWindowViewModel` | 启动台进度窗及其呈现逻辑 |
| `DashBoardStatusBarItem` | 状态栏"启动台"条目（无视图，仅标题+图标） |
