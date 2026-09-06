# Workstation — 异常与排查

## 模块可能抛出的异常/错误

本模块自身几乎不写防御代码，异常主要来自三处下游：

| 异常 | 触发条件 | 抛出位置 |
|---|---|---|
| 图标解析异常（`StreamGeometry.Parse` 对非法路径字符串抛 `FormatException` 一类） | 某贡献类的 `IconPath` 不是合法 StreamGeometry 路径标记 | 四个呈现模型的构造函数：`NavigationItemViewModel`（NavigationItemViewModel.cs:15）、`MenuItemViewModel`（MenuItemViewModel.cs:15）、`PanelTabViewModel`（PanelTabViewModel.cs:15）、`StatusBarItemViewModel`（StatusBarItemViewModel.cs:14）；以及 `MainWindowViewModel` 属性初始值（MainWindowViewModel.cs:91、96，解析 `Icons.ChevronDown/ChevronRight`） |
| 容器解析失败（DryIoc `ContainerException`） | 贡献声明的 `ContentViewType`/`ViewType` 未在容器注册，或 `EmptyStateView` 未注册 | `MainWindowViewModel.SelectActivity`（:149 解析 SideBar 内容）、`OpenMainView`（:170 解析主视图）、`SyncActiveTab`（:323 解析 tab 内容）；构造函数 :34 解析 `EmptyStateView` |
| `InvalidOperationException` | `AboutMenuItem` 命令执行时主窗口未设置或非活动（`FrameworkWindowManager.ShowDialog` 的契约） | 实际抛出在 Core/Framework `FrameworkWindowManager.ShowDialog`；触发点是 `Shell/AboutMenuItem.cs:17` 的 `windowManager.ShowDialog<AboutWindow>` |

## 静默路径（不抛异常但行为可能出乎意料）

- **未知主视图 Id**：`OpenMainView(string viewId)`（MainWindowViewModel.cs:159-164）在 `_mainViewsById` 查不到时直接 `return`——`OpenMainViewEvent` 负载与 `IMainViewContribution.Id` 是字符串级契约，拼写不匹配表现为"点了没反应"，无任何日志。
- **面板收起时点 tab**：`ActivateAuxTab`/`ActivateBottomTab`（:180-208）检测到 `ShellLayoutState` 拒绝（`ReferenceEquals(next, State)`）后直接返回，点击静默无效（正常时 tab 栏随面板一起不可见，只在绑定/状态异常时遇到）。
- **重复贡献 Id**：`_mainViewsById[id] = contribution`（:114）、`_itemsById[item.Id] = item`（:336）、`index[tab.Id] = contribution`（:283）均为索引器赋值，重复 Id **静默覆盖**；但对应的 `ObservableCollection`（`TopNavigationItems` 等）两个条目都会加入——表现为列表里出现两个相同项、内容解析总是用后注册者。
- **退出命令空操作**：`ExitMenuItem`（Shell/ExitMenuItem.cs:26-27）用 `?.` 链，`ApplicationLifetime` 不是 `IClassicDesktopStyleApplicationLifetime` 时静默不退出。
- **TogglePanel 默认分支**：`MainWindowViewModel.TogglePanel`（:251-256）的 `_` 默认分支调 `State.ToggleBottomPanel()`——传入任何未显式处理的 `TogglePanelTarget` 值都会切换 BottomPanel。
- **CloseWindow 式语义**：本模块不涉及；窗口管理静默路径见 Framework 文档。

## 错误处理路径

本模块**没有 try/catch**（全模块 grep 无一处）。异常沿调用栈向上传播：ViewModel 命令执行中的异常由 CommunityToolkit.Mvvm 的 `RelayCommand` 直接抛出到 Avalonia UI 线程未处理异常；启动期异常（如 `EmptyStateView` 解析失败）在 `MainWindowViewModel` 构造即 `CreateShell` 阶段炸出，由 Framework 启动序列的全局异常处理（`FrameworkApplication` 的 `Logger.Error/Fatal`）兜底。设计取向：贡献/视图注册错误属编程错误，fail-fast 而非吞掉。

## 排查方式

| 症状 | 看哪里 | 常见原因 |
|---|---|---|
| 点了导航项 SideBar 没内容 | `SelectActivity`（:142-154）的分支条件：`State.SideBar.Visible` 与 `State.SideBar.ContentFor`；再看该贡献 `ContentViewType` 是否已 `Register` | 视图类型忘在模块/本模块 `RegisterCustomService` 注册 → DryIoc 异常；或 `ShellLayoutState.SelectActivity` 语义是"再点收起" |
| 发布 `OpenMainViewEvent` 没反应 | `OpenMainView`（:161）的静默 return；核对事件负载字符串与 `IMainViewContribution.Id` 是否逐字符一致 | Id 不匹配（字符串级契约）；或 `EnsureContributionsLoaded` 尚未执行（窗口未 Opened） |
| 菜单/面板 tab 顺序不对 | `ShellContributionCollector.Get*` 按 `Order` 升序；核对各贡献类 `Order` 值（矩阵见 api.md 第 5 节） | Order 撞值（如两个 tab 都 10，容器解析顺序决定先后） |
| 拖拽分隔条面板不动/乱跳 | `PanelResizer.GetParentGrid`（PanelResizer.cs:20）是否仍返回 null；code-behind 方向换算（MainWindow.axaml.cs:28、36 的负号） | `GetParentGrid` 被"修复"成返回 base → GridSplitter 原生重排与 `ShellLayoutState` 打架；Auxiliary/Bottom 忘了取反导致方向反 |
| 应用启动即崩溃 | `MainWindowViewModel` 构造函数 :34 解析 `EmptyStateView`；`WorkstationApplication.RegisterCustomService` 是否先执行（Framework `RegisterTypes` 保证先 `RegisterFrameworkServices` 后 `RegisterCustomService`） | 注册顺序/遗漏 |
| 日志位置 | 本模块不写日志；启动期错误看 Framework `Logger`（Serilog 静态封装）输出 | — |
