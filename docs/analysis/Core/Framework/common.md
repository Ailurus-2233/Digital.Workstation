# Framework — 模块简述

## 模块做什么

Core/Framework 是 Digital.Workstation 桌面应用的**应用框架层**，位于抽象层（Abstractions/Common/Models/UIPackage）之上、业务模块（Modules/*）之下，提供五块能力：

1. **应用引导与启动序列**：抽象基类 `FrameworkApplication<TWindow>`（`FrameworkApplication.cs:16`）继承 Prism.DryIoc 的 `PrismApplication`，装载主题、注册框架服务、执行"启动台 → 逐模块异步加载 → 显示主窗口"的三阶段启动序列（ADR-0004）。
2. **窗口管理实现**：`FrameworkWindowManager`（`WindowManager/FrameworkWindowManager.cs:12`）实现 Abstractions 定义的 `IWindowManager` 与 `IMainWindowManager`，维护"窗口类型 → 窗口实例"映射，负责窗口的显示/对话/隐藏/关闭与主窗口登记。
3. **Shell 布局状态机**：不可变 record `ShellLayoutState`（`Shell/ShellLayoutState.cs:7`）+ 四个区域状态 record（`SideBarState`、`AuxiliaryPanelState`、`BottomPanelState`、`MainContentState`），以 reducer 风格的纯转换方法描述工作区五区域（ActivityBar/SideBar/MainContent/AuxiliaryPanel/BottomPanel）的布局状态流转。
4. **Shell 贡献收集**：`ShellContributionCollector`（`Shell/ShellContributionCollector.cs:8`）从 DI 容器收集各模块注册的 shell 贡献（导航项、主视图、面板 tab、菜单项、状态栏项），过滤定位枚举并按 `Order` 排序后交给 shell 渲染。
5. **窗口基类与基础布局**：抽象基类 `FrameworkWindow`（`Shell/FrameworkWindow.cs:14`，继承 UrsaWindow）+ `FrameworkWindowTheme`（`Shell/FrameworkWindowTheme.cs:12`）内置 VS Code 式五区 shell 的布局模板（四档面板对齐）、共享部件模板与 shell 样式；`PanelResizer`（`Shell/PanelResizer.cs:13`）提供声明式分隔条。Modules/Workstation 的 `MainWindow` 继承之，axaml 只保留应用级 chrome（菜单、标题、快捷键）。

## 核心设计逻辑

- **ADR-0004 启动序列**：Prism 默认在框架初始化阶段同步 `InitializeModules()` 并把 MainWindow 直接设为桌面生命周期主窗口。本框架用三个空覆盖切断默认行为——`OnInitialized()`（`FrameworkApplication.cs:44`）与 `InitializeModules()`（`FrameworkApplication.cs:51`）置空阻止提前显示主窗口与同步加载模块，`OnFrameworkInitializationCompleted()`（`FrameworkApplication.cs:35`）不调用 base（base 会把尚未完成模块加载的 MainWindow 直接设为桌面生命周期主窗口），改为 fire-and-forget 调用私有 `RunStartupSequenceAsync()`（`FrameworkApplication.cs:64`）。理由：模块加载可能慢/失败，需要启动台呈现进度并让用户对失败模块做"继续/退出"决策；权衡是放弃了 Prism 内建的同步模块加载路径，自行用 `IModuleCatalog`/`IModuleManager` 逐模块 `await Task.Run(() => moduleManager.LoadModule(name))`（第 88 行）把加载移出 UI 线程。
- **事件驱动的进度/失败协议**：启动序列与启动台之间不直接引用，全部走 Models 模块的 `IEventAggregator` 事件：`StartupProgressEvent`（进度）、`ModuleLoadFailedEvent`（失败）、`StartupFailureActionEvent`（用户决策回传）。`WaitForFailureActionAsync`（`FrameworkApplication.cs:117`）把 PubSub 事件当一次性 RPC 用（订阅 → `TaskCompletionSource` await → 退订）。理由：Framework 不依赖任何具体启动台窗口类型，子类经抽象方法 `CreateSplashWindow()`（第 58 行）注入启动台，解耦框架与具体模块。
- **不可变布局状态（reducer 模式）**：`ShellLayoutState` 是 `sealed record`，所有转换方法（`SelectActivity`、`ToggleSideBar`、`Resize` 等）返回新实例，非法操作（面板收起时激活 tab）返回等值状态（`return this`，见 `ShellLayoutState.cs:73、86`）。理由：布局状态单一来源、可单测（这正是 UnitTest/Framework 唯一测试对象的由来）、UI 只绑定状态不做决策；代价是每次转换产生新对象，但状态极小（五个嵌套 record），开销可忽略。
- **一个窗口管理器实例注册两个接口**：`RegisterFrameworkServices`（`FrameworkApplication.cs:142`）中 `new FrameworkWindowManager()` 一次，`RegisterSingleton<IMainWindowManager>(() => windowManager)` 与 `RegisterSingleton<IWindowManager>(() => windowManager)` 共享同一实例（第 147-149 行）。理由：主窗口操作与普通窗口操作共享 `_windowMap` 与 `_mainWindow` 状态，拆成两个实例会出现状态分裂。
- **固定 Dark 主题**：`Initialize()`（第 21-29 行）硬编码 `RequestedThemeVariant = ThemeVariant.Dark`，注释说明"当前设计目标为 VS Code Dark+ 单一色调，未做亮色适配"。
- **ViewModel 约定式定位**：`ConfigureViewModelLocator()`（第 205 行）把 `Views` 命名空间替换为 `ViewModels`，`Window`/`Page` 后缀补 `ViewModel`、`View` 后缀补 `Model`，免注册。理由：统一约定消灭样板注册代码；代价是命名/目录偏离约定时定位静默失败（返回 null）。
- **布局模板整体切换（四档面板对齐）**：`FrameworkWindow.PanelAlignment` 的每个枚举值对应 `FrameworkWindowTheme` 里一份**静态**布局模板（`WindowLayoutLeft/Right/Center/Justify`），切换即整体替换 `ContentTemplate`（`FrameworkWindow.cs:54-67`），不在一份模板上做动态调整。理由：四档差异只在 BottomPanel 及其分隔条的 `Grid.Column`/`ColumnSpan` 和侧栏的 `Grid.RowSpan`（非两端对齐时侧栏通高到底，不留空挡；ActivityBar 恒通高），静态模板直观且零运行时布局逻辑；代价是四份模板结构大量重复，修改五区结构要同步四份。配套的两个解耦决策：① Framework 不引用具体 ViewModel 类型，布局模板全部走宽松反射绑定（`{Binding ResizePanelCommand}` 等），ViewModel 契约靠约定；② 主题不走 x:Class code-behind，改走 `StyleInclude` + 构造时强制 `Loaded`（`FrameworkWindowTheme.cs:22`）——Avalonia.Generators 源生成器在本项目不产出 InitializeComponent（最小探针复现失败，Workstation 项目正常），StyleInclude 是 Semi/Ursa 主题同款机制，可绕过该问题。

## 状态流转

```
Avalonia 启动
  → FrameworkApplication.Initialize()            [主题：Dark + WorkstationTheme + VSCodePalette.ApplyTo(Resources)]
  → Prism 容器构建
  → RegisterTypes()                              [FrameworkApplication.cs:171]
      → RegisterFrameworkServices(): IoC.Initialize(registry, Container)；
        注册 FrameworkWindowManager 双接口单例、ShellContributionCollector 单例
      → ResolveFrameworkServices(): 解析 IEventAggregator、IMainWindowManager 存入字段
      → RegisterCustomService()（子类钩子）
  → CreateShell(): Container.Resolve<TWindow>()   [主窗口实例由容器创建]
  → OnFrameworkInitializationCompleted()
      → RunStartupSequenceAsync()                 [异步，fire-and-forget]
          阶段 1 CoreServices: _windowManager.HandleMainWindow()（登记主窗口）
                              → ShowWindow(CreateSplashWindow())（显示启动台）
                              → Publish StartupProgress(CoreServices, null, 0, 0)
                              → IModuleCatalog.Initialize()
          阶段 2 LoadingModules: 逐模块 Publish StartupProgress(LoadingModules, name, i+1, total)
                              → await Task.Run(LoadModule)
                              → 失败：Logger.Error + Publish ModuleLoadFailedEvent
                                     → WaitForFailureActionAsync()
                                       Continue → 跳过该模块继续；Exit → Shutdown()
          阶段 3 Ready: Publish StartupProgress(Ready, null, total, total)
                     → ShowMainWindow(): lifetime.MainWindow = window
                                       → _windowManager.ShowMainWindow()
                                       → _windowManager.CloseWindowsExceptMain()（关掉启动台）
          序列自身异常：Logger.Fatal + Shutdown()
```

副作用集中点：`IoC.Initialize`（一次性全局容器引用）、`FrameworkWindowManager._windowMap`（窗口注册表）、`ApplicationLifetime.MainWindow` 赋值、事件发布。`ShellLayoutState` 一侧无任何副作用——纯数据转换，由消费方（Modules/Workstation 的 `MainWindowViewModel._state`）持有当前实例并整体替换。

## 常见修改场景

1. **要调整某区域的默认尺寸或 clamp 区间**：改对应区域 record 的常量/默认值——`Shell/SideBarState.cs`（`MinWidth=120`、`MaxWidth=480`、`Width=240`）、`Shell/AuxiliaryPanelState.cs`（`Width=280`）、`Shell/BottomPanelState.cs`（`MinHeight=80`、`MaxHeight=480`、`Height=160`）；clamp 逻辑在 `ShellLayoutState.Resize`（`Shell/ShellLayoutState.cs:103`）。同步更新 `UnitTest/Framework/ShellLayoutStateResizeTests.cs` 中断言边界的用例。
2. **要加启动阶段或改失败处理策略**：核心逻辑在 `FrameworkApplication.RunStartupSequenceAsync`（`FrameworkApplication.cs:64`）；新增阶段需同步扩展 Models 模块的 `StartupPhase` 枚举并在启动台 ViewModel 中处理；改失败策略看 `WaitForFailureActionAsync`（第 117 行）与 catch 块（第 90-100 行）。
3. **要新增一种 shell 贡献类型**（如工具栏项）：在 Abstractions 定义新贡献接口 → 在 `ShellContributionCollector`（`Shell/ShellContributionCollector.cs`）加一个 `Get*` 收集方法（仿照 `GetNavigationItems` 的 Resolve→Where→OrderBy→ToArray 模式）→ 消费方在 Modules/Workstation。
4. **要改窗口显示语义**（如允许同类型多实例窗口）：改 `FrameworkWindowManager.InitializeWindow`（`WindowManager/FrameworkWindowManager.cs:45`）的 `_windowMap.TryAdd` 拒绝重复注册逻辑，注意 `CloseWindow`/`HideWindow` 按类型索引的前提会随之失效。
5. **要支持亮色主题**：改 `FrameworkApplication.Initialize`（第 23-27 行）的硬编码 Dark 与 `VSCodePalette.ApplyTo` 的写死色值（色值在 UIPackage 模块）。
6. **要改 View/ViewModel 命名约定**：改 `ConfigureViewModelLocator`（`FrameworkApplication.cs:209`）的 `SetDefaultViewTypeToViewModelTypeResolver` 委托。
7. **要新增/修改布局档位**（面板对齐）：三处同步改——`Shell/PanelAlignment.cs` 枚举加/改档；`Shell/FrameworkWindowTheme.axaml` 加一份 `WindowLayout*` 静态布局模板（仿现有四份，差异点在 BottomPanel 及其分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`）并同步四份模板的公共结构；`FrameworkWindow.UpdateLayoutTemplate`（`Shell/FrameworkWindow.cs:56-62`）的枚举→资源键映射。注意 Center 是 switch 兜底分支（`_ => "WindowLayoutCenter"`），新档必须显式加分支；消费侧（Modules/Workstation 的 `PanelAlignmentContribution`）还要为新档加 Title/IconPath/Order 的 case 与注册。
