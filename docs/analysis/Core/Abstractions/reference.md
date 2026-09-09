# Abstractions — 模块关系链

## 依赖关系（本模块 → 外部）

来自 `Core/Abstractions/Abstractions.csproj`：

| 依赖 | 版本 | 用途 |
|---|---|---|
| `Avalonia`（NuGet） | 11.3.20 | 提供 `Avalonia.Controls.Window`，被 `IWindowManager`（WindowManager/IWindowManager.cs）与 `WindowManagerExtenstion`（WindowManager/IWindowManagerExtenstion.cs）引用 |

无项目引用（`ProjectReference`），本模块是解决方案依赖图的最底层之一。

编译设置：`<TargetFramework>net10.0</TargetFramework>`、`<ImplicitUsings>enable</ImplicitUsings>`、`<Nullable>enable</Nullable>`。

间接提及（XML 注释中出现但未直接引用的外部概念，实际引用由 shell 实现侧承担）：

- `Prism.Ioc.IContainerRegistry` — 主视图/状态栏两接口注释声明「模块在 `Prism.Ioc.IContainerRegistry` 中以本接口注册实现」；本程序集本身**未**引用 Prism 程序集，注释中的 `<see cref="Prism.Ioc.IContainerRegistry" />` 无法解析。
- Prism Region — `ShellRegions`（Regions/ShellRegions.cs）注释「Shell 布局的 Prism Region 名称常量」。
- `PathIcon`、`StreamGeometry`（Avalonia）— `IconPath` 的注释约定「由 PathIcon 消费并随主题变色」。
- `ShellLayoutState`（shell 侧类型）— `ToolViewContribution` 注释提及「面板收起期间其 tab 的激活操作会被 ShellLayoutState 拒绝」。
- `OpenMainViewEvent`（shell 侧事件）— `IMainViewContribution` 注释提及「SideBar 内交互请求打开主视图时（OpenMainViewEvent，负载为 Id）」。
- `MenuRegistration.RegisterMenus`（Framework 侧）— `IMenuItemContribution`/`MenuGroupAttribute` 注释提及的菜单 attribute 扫描注册入口（ADR-0001）。
- `ToolViewRegistration.RegisterToolViews`（Framework 侧）— `ToolViewAttribute`/`ToolViewContribution` 注释提及的工具视图 attribute 扫描注册入口（ADR-0002，`docs/adr/0002-toolview-drag-persistence.md`）。

## 被依赖关系（外部 → 本模块）

本模块为纯契约层，按设计意图被以下角色依赖（依据接口注释推断；具体实现项目不在本模块目录内，属其他模块文档范围）：

- **Shell 宿主**：实现/消费全部贡献契约（收集 `ToolViewContribution` 元数据与接口实现并渲染到 `ShellRegions` 各 Region；菜单经 Framework 侧 `MenuTreeBuilder` 建树渲染），实现 `IWindowManager` 与 `IMainWindowManager`（窗口实例「从容器中解析」）。
- **各功能模块**：实现 `I*Contribution` 接口向 shell 贡献主视图、状态栏项；View 类标 `ToolViewAttribute` 后经 `RegisterToolViews(Assembly)` 扫描贡献工具视图（导航项/面板 tab，ADR-0002）；菜单项例外——标注 `MenuGroupAttribute`/`MenuItemAttribute` 的菜单类经 `RegisterMenus(Assembly)` 扫描生成实现（ADR-0001）；注入 `IWindowManager`/`IMainWindowManager` 弹窗。

> 待进一步调查：解决方案中具体的 shell 项目与各模块项目名，需在对应模块的深读中确认。

## 核心内部数据结构

本模块无内部（非 public）类型。全部类型清单及定义位置：

| 类型 | 种类 | 文件 |
|---|---|---|
| `ToolViewPlacement` | enum（`ActivityBar`/`AuxiliaryPanel`/`BottomPanel`） | Core/Abstractions/Contributions/ToolViewAttribute.cs |
| `ToolViewAttribute` | sealed class（`AttributeTargets.Class`，工具视图声明，ADR-0002） | 同上 |
| `ToolViewContribution` | sealed class（7 个 `required init` 属性，工具视图元数据） | Core/Abstractions/Contributions/ToolViewContribution.cs |
| `IMainViewContribution` | interface | Core/Abstractions/Contributions/IMainViewContribution.cs |
| `IMenuItemContribution` | interface（路径/分组模型，无 `Id`/定位枚举，ADR-0001） | Core/Abstractions/Menus/IMenuItemContribution.cs |
| `MenuGroupAttribute` | sealed class（`AttributeTargets.Class`，菜单类声明） | Core/Abstractions/Menus/MenuGroupAttribute.cs |
| `MenuItemAttribute` | sealed class（`AttributeTargets.Method`，菜单项声明） | Core/Abstractions/Menus/MenuItemAttribute.cs |
| `IStatusBarItemContribution` | interface | Core/Abstractions/Contributions/IStatusBarItemContribution.cs |
| `IWindowManager` | interface（11 方法） | Core/Abstractions/WindowManager/IWindowManager.cs |
| `IMainWindowManager` | interface（4 方法） | Core/Abstractions/WindowManager/IMainWindowManager.cs |
| `WindowManagerExtenstion` | static class（7 个泛型扩展） | Core/Abstractions/WindowManager/IWindowManagerExtenstion.cs |

### 贡献契约的结构

接口类贡献（主视图/状态栏）与工具视图元数据共享字段骨架：

```
Id (string) + Title (string) + IconPath (string) + Order (int)
  + 定位字段: ToolViewContribution.Placement（IStatusBarItemContribution/IMainViewContribution 无）
  + 行为字段: ViewType / Command（IStatusBarItemContribution 无）
```

`ToolViewContribution` 不是接口而是 sealed 元数据类（required init 属性），由 Framework 侧 `RegisterToolViews` 扫描 `ToolViewAttribute` 生成，模块不手写实现（ADR-0002）。菜单契约 `IMenuItemContribution` 不适用此骨架（ADR-0001）：无 `Id`、无定位枚举，字段为 `Title`/`IconPath?`/`Path`/`Group?`/`GroupOrder`/`NodeOrder`/`Order`/`Command`，定位由路径/分组模型表达。

差异点（易混，见 glossary.md）：

- `IMainViewContribution` 没有 `Title`/`IconPath`/`Order`，只有 `Id` + `ViewType`。
- `Id` 唯一性约束一致为全局唯一、约定模块名前缀：`IMainViewContribution.Id` 注释建议模块名前缀；`ToolViewAttribute.Id` 约定模块名前缀且程序集内重复时扫描记 `Logger.Warning` 跳过；`IStatusBarItemContribution.Id` 全局唯一。`IMenuItemContribution` 无 `Id`（菜单链路无消费方，ADR-0001 第 9 条）。

### 窗口管理类型的关系

`WindowManagerExtenstion` →（转发）→ `IWindowManager`（Type 版方法）→（实现侧）→ DI 容器解析 `Window`。`IMainWindowManager` 与 `IWindowManager` 无继承关系，是独立接口，专管主窗口。
