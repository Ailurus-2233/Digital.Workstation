# Abstractions — 模块简述

## 模块做什么

`DigitalWorkstation.Core.Abstractions`（`Core/Abstractions/Abstractions.csproj`）是 Digital.Workstation 桌面应用的**契约层**：它不含任何实现，只定义两类公开契约，供各功能模块（modules）与 shell 宿主之间解耦通信：

1. **Shell 贡献契约**（`Contributions/`、`Menus/`、`Regions/` 三个目录）：模块向 shell 界面区域（ActivityBar、SideBar、MainContent、AuxiliaryPanel、BottomPanel、菜单栏、状态栏）注入条目的契约，配合 `ShellRegions` 常量类（Regions/，目前全仓零消费方，属存量公共契约）声明 Prism Region 名称。接口类贡献共 3 个：`IMainViewContribution`、`IStatusBarItemContribution`（均在 `Contributions/`）、`IMenuItemContribution`（`Menus/`）；原导航项/面板 tab 两接口已合并删除，统一为**工具视图（Tool View）**声明式契约（ADR-0002，docs/adr/0002-toolview-drag-persistence.md）：`ToolViewAttribute`（标 View 类）+ 配套枚举 `ToolViewPlacement` + 元数据类 `ToolViewContribution`（均在 `Contributions/`），经 Framework 侧 `RegisterToolViews(Assembly)` 扫描注册。`Menus/` 目录另有两个菜单注册 attribute `MenuGroupAttribute`/`MenuItemAttribute`（菜单契约的声明式注册入口，见 ADR-0001）。命名空间相应分 `DigitalWorkstation.Core.Abstractions.Contributions`、`DigitalWorkstation.Core.Abstractions.Menus`、`DigitalWorkstation.Core.Abstractions.Regions` 三组。
2. **窗口管理契约**（`WindowManager/` 目录）：`IWindowManager`（以类型或实例为中心显示/隐藏/关闭窗口与对话框）、`IMainWindowManager`（主窗口显隐与批量关闭）、以及泛型扩展类 `WindowManagerExtenstion`（注意源码中拼写为 Extenstion）。

目标框架 `net10.0`，开启 `ImplicitUsings` 与 `Nullable`，唯一依赖是 NuGet 包 `Avalonia 11.3.20`（仅为 `Avalonia.Controls.Window` 类型）。

## 核心设计逻辑

- **纯契约、零实现**：本项目只有接口、枚举、常量、三个 attribute 声明类（`MenuGroupAttribute`/`MenuItemAttribute`/`ToolViewAttribute`）、一个元数据类（`ToolViewContribution`，仅 required init 属性）和一个静态扩展类。实现落在 shell 宿主与各模块中。这是典型的「抽象在 Core、实现在别处」的分层方式，使模块只依赖本程序集即可参与 shell 的 UI 组合，不依赖 shell 实现。
- **贡献点模式（contribution pattern）**：接口类贡献 `I*Contribution` 结构同构——`Id`（稳定标识）+ `Title` + `IconPath` + `Order` + 定位/行为字段；模块在 `Prism.Ioc.IContainerRegistry` 中以接口注册实现，shell 收集全部实现后按 `Order`（小者靠前）渲染。选择该模式而非直接 new 视图，是为了让模块**声明式**地扩充 UI，shell 无需知道有哪些模块。**工具视图与菜单契约是例外**：工具视图（ADR-0002）不手写贡献类，View 类标 `ToolViewAttribute` 后经 Framework 侧 `RegisterToolViews(Assembly)` 扫描生成 `ToolViewContribution` 元数据（以单例注册）并把 View 类型注册进容器；菜单（ADR-0001）由标注 `MenuGroupAttribute`（标类）/`MenuItemAttribute`（标方法）的模块类经 `RegisterMenus(Assembly)` 扫描生成实现。`IMenuItemContribution` 无 `Id`、无定位枚举，改用路径/分组模型（`Path` + `Group`/`GroupOrder` + `NodeOrder`/`Order`）表达任意深度子菜单、命名分组与组间分隔线。
- **图标走 StreamGeometry path 字符串**：`IconPath`/`Icon` 属性（`IStatusBarItemContribution.IconPath`、`IMenuItemContribution.IconPath`、`ToolViewContribution.IconPath`、`ToolViewAttribute.Icon`）是 `StreamGeometry` 的 path 字符串而非图像资源，由 `PathIcon` 消费并随主题变色——统一了图标来源且支持主题化。
- **视图用 `Type` 经容器解析**：`ToolViewContribution.ViewType` 与 `IMainViewContribution.ViewType` 是 `System.Type` 而非视图实例，注释明确说明「经容器解析以支持依赖注入」——视图实例化延迟到 shell 侧，模块不自己 new 视图。
- **窗口管理接口双形态**：`IWindowManager` 的 `ShowWindow`/`ShowDialog` 各有 `Type` 版和 `Window` 实例版两组重载；`WindowManagerExtenstion` 再加泛型糖（`GetWindow<TWindow>` 等），内部全部转发到 `Type` 版非泛型方法（如 `manager.GetWindow(typeof(TWindow))`）。泛型扩展里 `ShowWindow<TWindow>(this IWindowManager, object)` 与 `ShowDialog<TWindow>(this IWindowManager, object)` 两个带 dataContext 的重载**没有** `where TWindow : Window` 约束（其余四个泛型方法有约束）。
- **Prism Region 名称用 `nameof`**：`ShellRegions` 的五个常量值就是各自标识符名（`ActivityBar`/`SideBar`/`MainContent`/`AuxiliaryPanel`/`BottomPanel`），通过 `nameof` 保证重构安全。

## 状态流转

本模块自身**无状态、无运行时行为**——没有字段、没有方法体（扩展类除外，且只做转发）。数据流发生在其消费者之间：

- Shell 贡献链：接口类贡献（主视图/状态栏项）由模块在 DI 容器以接口注册实现；工具视图由模块在 `RegisterTypes` 调 `RegisterToolViews(Assembly)`，Framework 侧扫描 `[ToolView]` 的 View 类，生成 `ToolViewContribution` 元数据并注册 View 类型；菜单类标注 `MenuGroupAttribute`/`MenuItemAttribute` 后经 `RegisterMenus(Assembly)` 扫描生成实现并注册 → shell 启动时收集全部贡献 → 工具视图按 `Placement`（默认归属，用户拖拽后以持久化布局为准）分派到 ActivityBar/AuxiliaryPanel/BottomPanel、按 `Order` 排序渲染；状态栏项渲染到状态栏；菜单经 Framework 侧 `MenuTreeBuilder` 按 `Path` 建树、分组排序、组间插分隔线后渲染到菜单栏。用户交互（点击导航项/面板 tab/菜单项、打开主视图事件 `OpenMainViewEvent` 负载为 `IMainViewContribution.Id`）触发 shell 解析 `ViewType` 或执行 `IMenuItemContribution.Command`。
- 窗口管理链：调用方持 `IWindowManager` → 调 `ShowWindow(Type)` 等 → 实现侧（不在本模块）从容器解析窗口实例并控制显隐。`IMainWindowManager.HandleMainWindow()` 处理主窗口显隐（托盘类场景），`CloseWindowsExceptMain()` 关闭除主窗口外所有窗口。
- 副作用约束：面板收起期间其 tab 的激活操作会被 `ShellLayoutState` 拒绝（见 `ToolViewContribution` 注释，`ShellLayoutState` 在 shell 侧实现，不在本模块）。

## 常见修改场景

1. **新增一种 shell 贡献类型**：若是可停靠的界面单元（导航项/面板 tab 一类），优先走工具视图体系——在 View 类上标 `[ToolView(id, titleKey)]`（`Contributions/ToolViewAttribute.cs`），模块 `RegisterTypes` 已调 `RegisterToolViews(Assembly)` 即可，无需新建契约类型。若是全新类别（如「工具栏按钮贡献」）：在 `Contributions/` 下新建 `IToolBarItemContribution.cs`，仿照 `IStatusBarItemContribution.cs`/`IMainViewContribution.cs` 的同构字段结构（`Id`/`Title`/`IconPath`/`Order` + 定位枚举 + 行为字段；菜单契约已演进为路径/分组模型，不再适合作为同构模板），放入命名空间 `DigitalWorkstation.Core.Abstractions.Contributions`；若需要新 Region，在 `Regions/ShellRegions.cs` 加 `public const string X = nameof(X);`。
2. **给现有贡献加元数据**（如菜单项加快捷键提示）：改对应接口文件（如 `Menus/IMenuItemContribution.cs`）增加属性；菜单元数据还需在 `MenuItemAttribute`/`MenuGroupAttribute` 补同名命名属性并在 Framework 侧 `MenuRegistration` 的扫描映射中透传；工具视图元数据同理——改 `ToolViewAttribute` 加命名属性、`ToolViewContribution` 加对应 init 属性，并在 Framework 侧 `ToolViewRegistration` 的扫描映射中透传。**注意这会同时破坏所有实现方与 shell 收集方**，属于跨程序集破坏性变更；本模块是解决方案最底层契约，任何签名改动都会级联。
3. **加窗口管理操作**（如「激活已存在窗口」）：在 `WindowManager/IWindowManager.cs` 加 `Type` 版方法声明，并在 `WindowManager/IWindowManagerExtenstion.cs` 补一条对应的泛型扩展（泛型版统一只是转发 `typeof(TWindow)`）。实现方（shell 侧）需同步实现。
4. **改 Region 名称**：只改 `Regions/ShellRegions.cs` 中的 `nameof` 目标；由于值来自 `nameof`，重命名常量本身时引用方编译安全，但**序列化/配置中已存的 Region 名字符串**会失配。
5. **改图标机制**：`IconPath`/`Icon` 出现在 4 个文件中（`Contributions/IStatusBarItemContribution.cs`、`Contributions/ToolViewContribution.cs`、`Contributions/ToolViewAttribute.cs`、`Menus/IMenuItemContribution.cs`），要换图标来源需一次性改这 4 个文件。
