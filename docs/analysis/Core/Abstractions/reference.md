# Abstractions — 模块关系链

## 依赖关系（本模块 → 外部）

来自 `Core/Abstractions/Abstractions.csproj`：

| 依赖 | 版本 | 用途 |
|---|---|---|
| `Avalonia`（NuGet） | 11.3.20 | 提供 `Avalonia.Controls.Window`，被 `IWindowManager`（WindowManager/IWindowManager.cs）与 `WindowManagerExtenstion`（WindowManager/IWindowManagerExtenstion.cs）引用 |

无项目引用（`ProjectReference`），本模块是解决方案依赖图的最底层之一。

编译设置：`<TargetFramework>net10.0</TargetFramework>`、`<ImplicitUsings>enable</ImplicitUsings>`、`<Nullable>enable</Nullable>`。

间接提及（XML 注释中出现但未直接引用的外部概念，实际引用由 shell 实现侧承担）：

- `Prism.Ioc.IContainerRegistry` — 五个贡献接口注释均声明「模块在 `Prism.Ioc.IContainerRegistry` 中以本接口注册实现」；本程序集本身**未**引用 Prism 程序集，注释中的 `<see cref="Prism.Ioc.IContainerRegistry" />` 无法解析。
- Prism Region — `ShellRegions`（Shell/ShellRegions.cs）注释「Shell 布局的 Prism Region 名称常量」。
- `PathIcon`、`StreamGeometry`（Avalonia）— `IconPath` 的注释约定「由 PathIcon 消费并随主题变色」。
- `ShellLayoutState`（shell 侧类型）— `IPanelTabContribution` 注释提及「面板收起期间其 tab 的激活操作会被 ShellLayoutState 拒绝」。
- `OpenMainViewEvent`（shell 侧事件）— `IMainViewContribution` 注释提及「SideBar 内交互请求打开主视图时（OpenMainViewEvent，负载为 Id）」。

## 被依赖关系（外部 → 本模块）

本模块为纯契约层，按设计意图被以下角色依赖（依据接口注释推断；具体实现项目不在本模块目录内，属其他模块文档范围）：

- **Shell 宿主**：实现/消费全部贡献接口（收集注册实现并按 `Order` 渲染到 `ShellRegions` 各 Region），实现 `IWindowManager` 与 `IMainWindowManager`（窗口实例「从容器中解析」）。
- **各功能模块**：实现 `I*Contribution` 接口向 shell 贡献导航项、主视图、面板 tab、菜单项、状态栏项；注入 `IWindowManager`/`IMainWindowManager` 弹窗。

> 待进一步调查：解决方案中具体的 shell 项目与各模块项目名，需在对应模块的深读中确认。

## 核心内部数据结构

本模块无内部（非 public）类型。全部类型清单及定义位置：

| 类型 | 种类 | 文件 |
|---|---|---|
| `ShellRegions` | static class，5 个 `const string` | Core/Abstractions/Shell/ShellRegions.cs |
| `NavigationItemPlacement` | enum（`Top`/`Bottom`） | Core/Abstractions/Shell/INavigationItemContribution.cs |
| `INavigationItemContribution` | interface | 同上 |
| `IMainViewContribution` | interface | Core/Abstractions/Shell/IMainViewContribution.cs |
| `PanelPlacement` | enum（`Auxiliary`/`Bottom`） | Core/Abstractions/Shell/IPanelTabContribution.cs |
| `IPanelTabContribution` | interface | 同上 |
| `MenuPlacement` | enum（`File`/`View`/`Help`） | Core/Abstractions/Shell/IMenuItemContribution.cs |
| `IMenuItemContribution` | interface | 同上 |
| `IStatusBarItemContribution` | interface | Core/Abstractions/Shell/IStatusBarItemContribution.cs |
| `IWindowManager` | interface（11 方法） | Core/Abstractions/WindowManager/IWindowManager.cs |
| `IMainWindowManager` | interface（4 方法） | Core/Abstractions/WindowManager/IMainWindowManager.cs |
| `WindowManagerExtenstion` | static class（7 个泛型扩展） | Core/Abstractions/WindowManager/IWindowManagerExtenstion.cs |

### 贡献接口的同构结构

五个 `I*Contribution` 接口共享字段骨架：

```
Id (string) + Title (string) + IconPath (string) + Order (int)
  + 定位字段: Placement / Menu / Panel（IStatusBarItemContribution 无）
  + 行为字段: ContentViewType / ViewType / Command（IStatusBarItemContribution 无）
```

差异点（易混，见 glossary.md）：

- `IMainViewContribution` 没有 `Title`/`IconPath`/`Order`，行为字段名是 `ViewType`（其他两个视图型接口是 `ContentViewType`）。
- `Id` 唯一性约束各不相同：`INavigationItemContribution.Id` 同一模块内唯一；`IMainViewContribution.Id` 跨模块全局唯一（建议模块名前缀）；`IPanelTabContribution.Id` 跨两个面板全局唯一；`IMenuItemContribution.Id`、`IStatusBarItemContribution.Id` 全局唯一。

### 窗口管理类型的关系

`WindowManagerExtenstion` →（转发）→ `IWindowManager`（Type 版方法）→（实现侧）→ DI 容器解析 `Window`。`IMainWindowManager` 与 `IWindowManager` 无继承关系，是独立接口，专管主窗口。
