# Abstractions — 异常与排查

## 本模块自身抛出的异常

**无。** 本模块（Core/Abstractions/）是纯契约程序集：三个贡献接口（`IMainViewContribution`/`IMenuItemContribution`/`IStatusBarItemContribution`）只有属性签名，`ToolViewAttribute`/`ToolViewContribution` 只有属性声明，`IWindowManager`/`IMainWindowManager` 只有方法签名，`ShellRegions` 只有常量。唯一含方法体的是 `WindowManagerExtenstion`（WindowManager/IWindowManagerExtenstion.cs），其 7 个泛型方法全部是一行转发（如 `return manager.GetWindow(typeof(TWindow));`），无任何参数校验、无 `throw` 语句、无 try/catch。

因此运行期异常只能来自**实现侧**或**调用侧违反契约**，本文件按「使用本契约时可能遇到的错误」组织。

## 使用契约时的潜在错误与触发条件

1. **`NullReferenceException`** — 触发位置：实现侧 `IWindowManager.GetWindow(Type)`（WindowManager/IWindowManager.cs 声明）返回 null 时，或扩展方法收到 null `manager`。
   - 注意可空性不一致：`IWindowManager.GetWindow(Type)` 返回类型声明为非空 `Window`，而 `WindowManagerExtenstion.GetWindow<TWindow>`（IWindowManagerExtenstion.cs 第 22 行）声明返回 `Window?`——泛型层已预判实现可能返回 null，调用 `GetWindow<TWindow>` 的代码必须判空，调非泛型 `GetWindow(Type)` 则编译器不警告。
2. **DI 解析失败异常**（类型由容器决定，如 DryIoc/Prism 的解析异常）— 触发条件：`ViewType` 指向的视图类型或窗口类型未在容器注册。来源约定见各契约注释「经容器解析以支持依赖注入」（如 Contributions/IMainViewContribution.cs 第 19 行 `ViewType`、Contributions/ToolViewContribution.cs 第 44 行 `ViewType`）。排查：工具视图的 View 类型由 `RegisterToolViews(Assembly)` 扫描时自动注册，未注册说明该 View 类未标 `[ToolView]` 或模块未调 `RegisterToolViews`；主视图/窗口则检查模块 `RegisterTypes` 中的注册。
3. **`Id` 冲突导致的行为错乱**（非异常）— 触发条件：两个贡献使用相同 `Id`。唯一性约束写在注释里而非代码（如 `IMainViewContribution.Id`「跨模块全局唯一」），本模块无法强制；工具视图例外——同一程序集内 Id 重复时 `RegisterToolViews` 记 `Logger.Warning` 并跳过（ADR-0002）。排查：全局搜索重复 Id 字符串；`IMainViewContribution` 注释建议以模块名做前缀（如 `dashboard.overview`），`ToolViewAttribute.Id` 注释同样约定模块名前缀。
4. **泛型约束缺失导致的运行期失败** — `WindowManagerExtenstion.ShowWindow<TWindow>(this IWindowManager, object)` 与 `ShowDialog<TWindow>(this IWindowManager, object)`（IWindowManagerExtenstion.cs 第 35、43 行）**没有** `where TWindow : Window` 约束（同文件其余泛型方法都有）。以非 Window 类型调用这两个重载能编译通过，失败推迟到实现侧。
5. **面板激活被拒绝**（非异常，行为约束）— `ToolViewContribution` 注释（Contributions/ToolViewContribution.cs 第 7 行）：「面板收起期间其 tab 的激活操作会被 ShellLayoutState 拒绝」。表现：激活 tab 无效。排查：看 shell 侧 `ShellLayoutState` 的面板展开状态。

## 错误处理路径

本模块无错误处理代码。异常传播路径为：实现侧（shell 的 WindowManager 实现、DI 容器）抛出 → 沿调用栈到调用方（各功能模块）。契约层不做任何转换或包装。

## 排查方式汇总

| 症状 | 看哪里 | 常见原因 |
|---|---|---|
| 贡献项不出现在 UI | 接口类贡献：模块 `RegisterTypes` 是否按接口注册实现；工具视图（ADR-0002）：View 类是否标 `[ToolView]` 且模块 `RegisterTypes` 调了 `RegisterToolViews(Assembly)`（非可实例化 Control、程序集内 Id 重复者扫描时记 `Logger.Warning` 跳过）；菜单：是否调了 `RegisterMenus(Assembly)` 且菜单类/方法标注正确；`Order`/`Placement` 值；菜单的 `Path`/`Group` 是否含空段、方法签名是否无参 `void`/`Task`（非法者扫描时记日志跳过，见 ADR-0001） | 未注册/未扫描；定位字段写错区域；菜单路径含空段或方法签名非法被跳过 |
| 点击导航/面板 tab 无反应 | `ToolViewContribution.ViewType` 是否经 `RegisterToolViews` 扫描注册进容器；菜单则看 `IMenuItemContribution.Command` | 视图类型未注册到容器；Command 为 null 或 CanExecute 恒 false |
| 面板 tab 激活无效 | shell 侧 `ShellLayoutState` | 面板处于收起状态（设计行为，见上 5） |
| `GetWindow<TWindow>` 返回 null | 实现侧窗口注册 | 窗口类型未注册；注意接口非空标注与扩展可空标注不一致 |
| 主视图打不开 | `OpenMainViewEvent` 负载 Id 与 `IMainViewContribution.Id` 是否匹配 | Id 不匹配或前缀约定未遵守 |
