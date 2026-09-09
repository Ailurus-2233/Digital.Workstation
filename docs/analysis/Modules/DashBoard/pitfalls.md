# DashBoard — 不变量与陷阱

## 隐含不变量

1. **启动台必须先于模块加载可见**：`DashBoardWindow` 由启动序列 `Container.Resolve<DashBoardWindow>()`（WorkstationApplication.cs:40）在**模块加载开始前**显示。因此 `DashBoardWindow` 与 `DashBoardWindowViewModel` 的可用性不能依赖 `DashBoardModule.RegisterTypes`/`OnInitialized` 已执行——它们经由 Prism 容器对具体类型的直接解析与 ViewModelLocator 约定装配获得，不走模块注册清单。
2. **ViewModelLocator 命名约定**：`DashBoardWindow` ↔ `DashBoardWindowViewModel` 靠 `Views.Windows`/`ViewModels.Windows` 目录与同名前缀配对（DashBoardWindow.axaml:3 `AutoWireViewModel="True"`；README 第 46 行）。改类名、移动目录而不成对修改，绑定静默失效（无编译错误）。
3. **`[RelayCommand]` 命令名约定**：XAML 绑定 `ContinueCommand`/`ExitCommand`（DashBoardWindow.axaml:27-28），命令属性名 = 方法名 `Continue`/`Exit` + "Command"（DashBoardWindowViewModel.cs:70、79）。Avalonia 绑定运行期解析，改方法名后 XAML 不失效于编译期。
4. **UI 线程亲和**：两个事件订阅均为 `ThreadOption.UIThread`（DashBoardWindowViewModel.cs:19-20），回调里直接写 `[ObservableProperty]` 属性依赖这一点；改成 `PublisherThread` 会引入跨线程访问异常。
5. **事件负载 Id 一致性**：`OpenMainViewEvent` 的负载必须等于目标贡献的 `Id`。当前经 `ViewId` 常量（DashBoardOverviewMainView.cs:11、DashBoardRecentMainView.cs:11）单点维护；`DashBoardNavigationView.axaml.cs:35、40` 引用常量而非字面量——新增主视图时不要手写 Id 字符串。
6. **窗口单实例**：`FrameworkWindowManager` 保证同类型窗口同时只有一个实例，关闭（`Closing`）后从映射移除、可再 `ShowWindow`（见 docs/analysis/Core/Framework/）。因此"打开启动台"菜单项重复点击不会叠窗，但不要假设实例是同一个。

## 易错改法

1. **在 `DashBoardModule.OnInitialized` 里开窗**：方法体为空是**有意的**（DashBoardModule.cs:25 注释，ADR-0004）。在模块初始化时显示启动台已经太晚（启动进度窗的用途是显示模块加载进度本身），且会再造一个窗口实例。
2. **把 `FormatModuleText` 的全角括号改成半角**：`$"{moduleName}（{index}/{count}）"`（DashBoardWindowViewModel.cs:63）用全角"（）"是刻意的显示格式；README 第 24 行与文档均以"模块名 + i/N"描述。改格式属 UI 行为变更，不是"修正"。
3. **删除 `OnProgress` 中的 `IsFailed = false`（第 40 行）**：该复位让"失败后继续"的启动序列恢复进度显示（错误区隐藏、进度条恢复滚动）；删掉后失败 UI 会粘住。同理，`ModuleText` 在非 `LoadingModules` 阶段赋 `string.Empty`（第 50 行）是刻意的清空，删掉会残留上一个模块名。
4. **改 `OnProgress` switch 的默认分支**：`_ => PhaseText`（第 46 行）对未知阶段保持原文案；改成抛异常或清空会让未来新增的 `StartupPhase` 值闪空。
5. **删 `DashBoardNavigationView` 的无参构造**：看似死代码（shell 走容器注入构造），实际供 XAML runtime loader 使用（第 20-22 行注释）；删除后设计器/预览器实例化即崩。反向同理：不要把注入构造删掉只留无参——那会让容器解析也走 `IoC.Provider` 服务定位器。
6. **给 `DashBoardStatusBarItem.Title` 换"更贴切"的资源键**：它刻意复用 `Language.DashBoardNavigationTitle`（DashBoardStatusBarItem.cs:15）；在 Core/Resource 改 `DashBoardNavigationTitle` 的值会同时改导航项与状态栏两处显示。
7. **调整 `Order`/组序值"取整"**：`DashBoardTasksView` 的 `[ToolView(Order=15)]`（DashBoardTasksView.axaml.cs:10-11）、`DashBoardStatusBarItem.Order=20` 与 `DashBoardMenus` 的 `[MenuGroup(GroupOrder=100)]`/`[MenuItem(Order=100)]` 不是随意值，是插进 shell 预置项之间（或预置组之前）的排序验证点（各类注释明示）；改成 0/1000 这类整齐数字会让排序验证失效甚至改变相对位置。
8. **为工具视图手写贡献类或手写 `Register<View>()`**：`INavigationItemContribution`/`IPanelTabContribution` 已删除（ADR-0002），工具视图只需在 View 类上标 `[ToolView]`——`RegisterToolViews` 扫描时一并完成 `ToolViewContribution` 元数据生成与 View 容器注册；再在 `RegisterTypes` 手写注册属重复，违背单一渲染管道。
9. **把 `DashBoardWindow.axaml` 的硬编码字符串搬进 Language**：`Title="启动台"`、"Digital.Workstation"、"继续/退出"按钮文本、SideBar 按钮"概览/最近项目"目前是硬编码中文，与 ViewModel 走 `Language` 的文案并存——这是现状不是规范；统一本地化是有意改动，需明确决策而非顺手。

## 历史踩坑线索

- `DashBoardModule.cs:25` 注释「启动台窗口由 shell 启动序列在模块加载前显示（ADR-0004），模块自身不再开窗」——"不再"二字暗示模块历史上（或 Prism 默认模式下）曾自己开窗，迁到启动序列后留下防线注释。ADR-0004 文档本体不在仓库中（悬空引用，见 docs/analysis/Core/Models/pitfalls.md），决策细节只能从注释还原。
- `DashBoardNavigationView.axaml.cs:20-22` 注释「XAML runtime loader 需要无参构造；实际实例由容器经依赖注入构造创建」——双构造是对 XAML loader 限制的妥协记录；保留两个构造是契约。
- 贡献声明的注释普遍自带"验证"字样（`DashBoardNavigationView` 的 tracer bullet、`DashBoardTasksView`/状态栏的"验证贡献通路"），说明本模块第一优先级是**架构通路的探针**；删除这些"演示"贡献等于拆除 shell 贡献机制的活体验证。
- 上游文档备注：Core/Framework 深读期间曾以 `SetProgress` 指称启动台 ViewModel 的进度回调；该名在本模块全部 git 历史中不存在（`git log -S SetProgress` 无结果），真实方法名为 `OnProgress`/`OnModuleFailed`。引用旧名属过时记录。
