# Workstation — 异常与排查

## 模块可能抛出的异常/错误

本模块自身几乎不写防御代码，异常主要来自三处下游：

| 异常 | 触发条件 | 抛出位置 |
|---|---|---|
| 图标解析异常（`StreamGeometry.Parse` 对非法路径字符串抛 `FormatException` 一类） | `[ToolView(Icon = …)]`/`IStatusBarItemContribution.IconPath` 或菜单类 `[MenuItem(Icon = …)]` 不是合法 StreamGeometry 路径标记（`[ToolView]` 的 `Icon` 为 null 时不解析、不抛） | 呈现模型：`NavigationItemViewModel`（NavigationItemViewModel.cs:15）、`PanelTabViewModel`（PanelTabViewModel.cs:15）、`StatusBarItemViewModel`（StatusBarItemViewModel.cs:14）的构造函数与 Framework 的 `MenuItemViewModel.FromSubmenu`（`Core/Framework/Menus/MenuItemViewModel.cs:43`，建树转换时解析）；以及 `MainWindowViewModel` 属性初始值（MainWindowViewModel.cs:136、141、146，解析 `Icons.ChevronDown/ChevronRight/Settings`） |
| 容器解析失败（DryIoc `ContainerException`） | 工具视图元数据的 `ViewType` 未在容器注册（正常由 `RegisterToolViews` 扫描时自动 `Register`，缺席说明扫描跳过：非可实例化 `Control`/程序集内重复 Id，有 `Logger.Warning`），或 `EmptyStateView` 未注册 | 统一出口 `MainWindowViewModel.ContentFor`（:331-340 解析工具视图内容，SideBar/面板/拖拽迁移共用）、`OpenMainView`（:344 解析主视图）；构造函数 :54 解析 `EmptyStateView` |
| `InvalidOperationException`（被吞） | `HelpMenus.About` 执行时主窗口未设置或非活动（`FrameworkWindowManager.ShowDialog` 的契约） | 抛出在 Core/Framework `FrameworkWindowManager.ShowDialog`（触发点 `Menus/HelpMenus.cs:20`），但菜单命令经 Framework `ReflectedMenuItemContribution` 反射调用、**异常记日志不抛出**——表现为点"关于"无反应 + 一条错误日志 |

## 静默路径（不抛异常但行为可能出乎意料）

- **未知主视图 Id**：`OpenMainView(string viewId)`（MainWindowViewModel.cs:344-360）在 `_mainViewsById` 查不到时直接 `return`——`OpenMainViewEvent` 负载与 `IMainViewContribution.Id` 是字符串级契约，拼写不匹配表现为"点了没反应"，无任何日志。
- **面板收起时点 tab**：`ActivateAuxTab`/`ActivateBottomTab`（:365-393）检测到 `ShellLayoutState` 拒绝（`ReferenceEquals(next, State)`）后直接返回，点击静默无效（正常时 tab 栏随面板一起不可见，只在绑定/状态异常时遇到）。**拖拽落放不受此限**：`MoveTab`（:440-483）对目标面板强制展开（语义在 `ShellLayoutState.MoveTab`）。
- **重复贡献 Id**：`_mainViewsById[id] = contribution`（:182）、`_itemsById[item.Id] = item`（:663）、`_tabsById[tab.Id] = tab`（:601）均为索引器赋值，重复 Id **静默覆盖**；但对应的 `ObservableCollection`（`TopNavigationItems` 等）两个条目都会加入——表现为列表里出现两个相同项、内容解析总是用后注册者。工具视图的注册侧守卫只管程序集内（`ToolViewRegistration` 对同程序集重复 Id 记 `Logger.Warning` 跳过，`ToolViewRegistration.cs:38-44`）；跨程序集 Id 冲突无守卫，靠 Id 带模块前缀的约定自律（ADR-0002）。
- **退出命令空操作**：`FileMenus.Exit`（Menus/FileMenus.cs:21）用 `?.` 链，`ApplicationLifetime` 不是 `IClassicDesktopStyleApplicationLifetime` 时静默不退出。
- **TogglePanel 默认分支**：`MainWindowViewModel.TogglePanel`（:488-497）的 `_` 默认分支调 `State.ToggleBottomPanel()`——传入任何未显式处理的 `TogglePanelTarget` 值都会切换 BottomPanel。
- **CloseWindow 式语义**：本模块不涉及；窗口管理静默路径见 Framework 文档。

## 错误处理路径

本模块**没有 try/catch**（全模块 grep 无一处）。异常沿调用栈向上传播：ViewModel 命令执行中的异常由 CommunityToolkit.Mvvm 的 `RelayCommand` 直接抛出到 Avalonia UI 线程未处理异常；启动期异常（如 `EmptyStateView` 解析失败）在 `MainWindowViewModel` 构造即 `CreateShell` 阶段炸出，由 Framework 启动序列的全局异常处理（`FrameworkApplication` 的 `Logger.Error/Fatal`）兜底。设计取向：贡献/视图注册错误属编程错误，fail-fast 而非吞掉。**例外在菜单链路**：`[MenuItem]` 方法经 Framework `ReflectedMenuItemContribution` 反射调用，异常被记日志吞掉（见上表 `InvalidOperationException` 行）；非法方法签名（带参/返回值非 void/Task）与空段路径在注册时记 `Logger.Warning` 跳过。**工具视图链路同理**：`[ToolView]` 标在非可实例化 `Control` 或程序集内 Id 重复时记 `Logger.Warning` 跳过（`ToolViewRegistration.cs:31-44`），编译不报错，表现为条目静默缺席。

## 排查方式

| 症状 | 看哪里 | 常见原因 |
|---|---|---|
| 点了导航项 SideBar 没内容 | `SelectActivity`（:291-296）→ `SyncSideBarSelection`（:311-326）的分支条件：`State.SideBar.Visible` 与 `State.SideBar.ContentFor`；再看该工具视图的 `ViewType` 是否经 `RegisterToolViews` 注册（扫描跳过会记日志） | `[ToolView]` 标在抽象类/非 Control 上被跳过；或 `ShellLayoutState.SelectActivity` 语义是"再点收起" |
| 发布 `OpenMainViewEvent` 没反应 | `OpenMainView`（:344）的静默 return；核对事件负载字符串与 `IMainViewContribution.Id` 是否逐字符一致 | Id 不匹配（字符串级契约）；或 `EnsureContributionsLoaded` 尚未执行（窗口未 Opened） |
| 菜单/面板 tab 顺序不对 | 工具视图（含面板 tab）：持久化 placements 优先（`LoadToolViews.MovableIn`，MainWindowViewModel.cs:209-219 按配置 Index 排序），无配置条目才按 `ShellContributionCollector.GetToolViews` 的 `Order` 升序兜底（`ShellContributionCollector.cs:15-20`）；菜单：`MenuTreeBuilder` 规则——顶层按 `(NodeOrder, 标题)`、子菜单按 `(GroupOrder, 组名)` 分组 + 组内 `(Order, Title)`（核对各 attribute 值，矩阵见 api.md 第 5 节）；条目缺席看注册日志（工具视图：非 Control/重复 Id 被 `ToolViewRegistration` 跳过；菜单：非法签名/空段路径被跳过）；持久化里有但贡献已删除的孤儿条目随贡献迭代自然丢弃 | tab Order 撞值（容器解析顺序决定先后）；菜单 attribute 位次撞值（取最小声明）；layout.json 存的是旧配置 |
| 拖拽分隔条面板不动/乱跳 | `PanelResizer.GetParentGrid`（`Core/Framework/Layout/PanelResizer.cs:46`）是否仍返回 null；方向换算取反（同文件 :54-63 的负号） | `GetParentGrid` 被"修复"成返回 base → GridSplitter 原生重排与 `ShellLayoutState` 打架；Auxiliary/Bottom 忘了取反导致方向反 |
| 应用启动即崩溃 | `MainWindowViewModel` 构造函数 :41-55 解析 `EmptyStateView`；`WorkstationApplication.RegisterCustomService` 是否先执行（Framework `RegisterTypes` 保证先 `RegisterFrameworkServices` 后 `RegisterCustomService`） | 注册顺序/遗漏 |
| 布局改动重启后丢了 | 该变更路径末尾是否调了 `ScheduleSave()`（七个现有变更点见 pitfalls.md）；落盘文件是 %AppData%/Digital.Workstation/layout.json（Framework `LayoutPersistence`，500ms 防抖） | 新增变更路径忘挂 `ScheduleSave`；或 500ms 防抖窗口内进程被杀 |
| 工具视图拖不动/落放无反应 | 钉住项（`AllowMove=false`）本就不可拖（机制保留，当前无内置钉住项实例）；其余看：Framework `ToolViewButton.CanDrag` 绑定（主题模板里绑 `Contribution.AllowMove`）、`ToolViewBar` 的 ControlTheme 是否生效（`layout|ToolViewBar` 选择器匹配 StyleKey——控件若被加 `StyleKeyOverride` 会整体失效，见 Framework pitfalls）、`MoveTabCommand` 的拒绝分支（:442-445） | 钉住项不可拖是设计；底部钉住段（普通 ItemsControl）不接受拖放是设计；控件 StyleKeyOverride 破坏主题查找 |
| 日志位置 | 本模块不写日志；启动期错误看 Framework `Logger`（Serilog 静态封装）输出 | — |
