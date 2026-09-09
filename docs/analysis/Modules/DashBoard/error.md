# DashBoard — 异常与排查

## 模块自身抛出的异常

**本模块源码中没有显式 `throw` 语句，也没有 try/catch。** 可能的运行期异常全部来自它调用的外部设施：

1. **`IoC.Provider` 未初始化时构造 `DashBoardNavigationView` 失败** — 触发位置：无参构造 `DashBoardNavigationView()` → `IoC.Provider.Resolve<IEventAggregator>()`（Views/DashBoardNavigationView.axaml.cs:23）。触发条件：XAML runtime loader（设计器、预览器）在容器初始化前实例化视图。`IoC.Provider` 的未初始化行为定义在 Core/Common（见 docs/analysis/Core/Common/ 文档）。排查：确认该视图的实例化路径——shell 正常渲染走容器 `Resolve<DashBoardNavigationView>`（调用注入构造，不经无参构造）；只有 XAML loader 直建实例才走无参构造。
2. **`IWindowManager` 相关异常（来自 Core/Framework 实现侧，被菜单命令捕获记日志）** — 触发位置：`DashBoardMenus.OpenDashBoard` → `windowManager.ShowWindow<DashBoardWindow>`（DashBoardMenus.cs:20）。点击菜单时 `ReflectedMenuItemContribution` 的 `Command` 反射调用该方法，**方法内抛出的异常被捕获并记日志、不向 UI 传播**（见 docs/analysis/Core/Framework/api.md）。可能异常：
   - `InvalidOperationException`（消息为 `NullMainWindowError` 常量）：主窗口未登记（`FrameworkWindowManager.ShowWindow(Window)` 要求 `_mainWindow != null`，见 docs/analysis/Core/Framework/api.md）。本模块场景下主窗口已就绪（菜单项只在工作区可用），正常不触发；若在主窗口登记前触发该命令会命中。
   - `ArgumentException("TargetWindow type is illegal")`：`GetWindow` 解析结果非 `Window`。`DashBoardWindow` 是 `Window` 子类，不会触发；仅当容器被改写返回非 Window 时才可能。
3. **启动序列的模块加载异常不在此抛出**：本模块的 `RegisterTypes` 若抛异常（如容器注册冲突），由 Core/Framework 启动序列捕获并转为 `ModuleLoadFailedEvent` 发布——**本模块的 ViewModel 恰是该事件的订阅方**，因此 DashBoard 自身加载失败会显示在启动台上，用户可"继续（跳过）/退出"。

## 行为性故障（非异常）与排查

| 症状 | 可能原因 | 看哪里 |
|---|---|---|
| 启动台窗口打开但无任何进度文字/ViewModel 不工作 | `prism:ViewModelLocator.AutoWireViewModel="True"`（DashBoardWindow.axaml:3）的约定装配失败——ViewModel 命名/目录不符合 `Views.Windows.*` ↔ `ViewModels.Windows.*` 约定，或 `DashBoardWindowViewModel` 改名 | 窗口 DataContext 是否为 `DashBoardWindowViewModel` 实例；README 第 46 行"按 Views ↔ ViewModels 的命名/目录约定自动绑定" |
| 启动台进度不动、失败不显示 | 事件未到达：订阅在构造函数（DashBoardWindowViewModel.cs:19-20），若 ViewModel 未创建则无人订阅；或发布方（FrameworkApplication）未到对应阶段 | Serilog 控制台启动日志（docs/agents/verification.md 约定）；确认 `StartupProgressEvent` 发布 |
| 点击"概览/最近项目"主视图不切换 | `OpenMainViewEvent` 负载 Id 与 `IMainViewContribution.Id` 不匹配——负载来自 `DashBoardOverviewMainView.ViewId`/`DashBoardRecentMainView.ViewId` 常量（DashBoardNavigationView.axaml.cs:35、40），改 Id 字符串必须同步改常量引用处 | 比对 `"dashboard.overview"`/`"dashboard.recent"` 在贡献类与订阅方（shell）两侧是否一致 |
| 菜单"打开启动台"点击无反应或报错 | `IWindowManager` 未解析到（`DashBoardMenus` 主构造注入失败会在模块加载期暴露）；或 `OpenDashBoard` 方法内异常被 `ReflectedMenuItemContribution` 吞掉记日志；或窗口已注册在 `_windowMap` 的实例状态异常 | Serilog 日志中的菜单命令异常记录；Core/Framework `FrameworkWindowManager.ShowWindow` 路径；窗口是否已被关闭并从映射移除（关闭后可再次 Show） |
| 菜单"打开启动台"项根本不出现 | `OpenDashBoard` 方法签名被改非法（带参/返回值非 void/Task）——`RegisterMenus` 判定非法记 `Logger.Warning` 跳过；或 `[MenuGroup]` 路径含空段 | Serilog 日志的 Warning；Core/Framework `MenuRegistration`/`MenuTreeBuilder` 的跳过规则 |
| 工具视图或接口贡献项不显示或顺序不对（菜单项见上行） | 工具视图不出现：`[ToolView]` 标注的类非可实例化 `Control`、或程序集内 Id 重复——`RegisterToolViews` 记 `Logger.Warning` 跳过（ADR-0002）；Id 跨模块冲突（唯一性约束，见 docs/analysis/Core/Abstractions/error.md）；`Order` 值与其他贡献相同导致相对顺序不稳。菜单项无 Id/全局 Order，位次由 `[MenuGroup]`/`[MenuItem]` 的 Path/Group/GroupOrder/Order 经 `MenuTreeBuilder` 建树决定 | Serilog 日志的 Warning；`ToolViewRegistration` 的跳过规则与 `ShellContributionCollector.GetToolViews()` 的排序（Core/Framework）；全仓搜索重复 Id 字符串；菜单看 `MenuTreeBuilder` 的分组/排序规则 |
| 按钮点击后绑定命令不执行 | XAML 绑定名 `ContinueCommand`/`ExitCommand` 与源生成命令名不匹配——命令名 = `[RelayCommand]` 方法名 + "Command"，改方法名后 XAML 运行期绑定静默失败 | DashBoardWindow.axaml:27-28 的 `Command="{Binding ...}"` 与 DashBoardWindowViewModel.cs:70、79 的方法名 |

## 错误处理路径

模块内无错误转换/传播层：

- **进入模块的异常**：`RegisterTypes` 与贡献类属性求值中的异常 → 冒泡到 Prism 模块初始化 → 启动序列捕获 → `ModuleLoadFailedEvent`（含 `ErrorMessage`）→ 本模块 `DashBoardWindowViewModel.OnModuleFailed` 显示。例外：`RegisterToolViews` 对非法 `[ToolView]` 类（非可实例化 Control、程序集内 Id 重复）与 `RegisterMenus` 对非法 `[MenuItem]` 签名、空段路径只记 `Logger.Warning` 跳过，不抛异常。
- **模块发出的决策**：`StartupFailureActionEvent` 是唯一的"错误后续"通道，`Continue` 让启动序列跳过失败模块，`Exit` 终止应用。
- **UI 线程亲和**：两个事件订阅都用 `ThreadOption.UIThread`，事件回调直接修改可观察属性是安全的；若改为 `PublisherThread`，跨线程改属性会抛 Avalonia 线程访问异常。

## 日志

本模块不直接写日志（无 `Logger` 调用）。启动台相关的日志在启动序列侧（Core/Framework `FrameworkApplication` 的 `Logger.Error/Fatal`）；排查启动问题时先看 Serilog 控制台输出，再对照启动台显示。
