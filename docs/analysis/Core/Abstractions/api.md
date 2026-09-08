# Abstractions — 对外接口与调用方式

命名空间四组：`DigitalWorkstation.Core.Abstractions.Contributions`（Contributions/ 目录，导航/主视图/面板/状态栏四接口）、`DigitalWorkstation.Core.Abstractions.Menus`（Menus/ 目录，菜单路径/分组模型三类型）、`DigitalWorkstation.Core.Abstractions.Regions`（Regions/ 目录，仅 `ShellRegions` 常量）与 `DigitalWorkstation.Core.Abstractions.WindowManager`（WindowManager/ 目录）。全部为 `public`；项目无 internal 类型。

## Shell 贡献契约（Contributions/ 与 Menus/，Region 常量在 Regions/）

### `ShellRegions`（static class，Regions/ShellRegions.cs）

Prism Region 名称常量，值均经 `nameof` 生成：

| 常量 | 值 | 语义 |
|---|---|---|
| `ActivityBar` | `"ActivityBar"` | 工作区最左侧竖向导航栏 |
| `SideBar` | `"SideBar"` | ActivityBar 右侧容器，显示当前选中导航项内容 |
| `MainContent` | `"MainContent"` | 工作区中央主 Region，单视图切换 |
| `AuxiliaryPanel` | `"AuxiliaryPanel"` | 工作区右侧 tab + 容器区域 |
| `BottomPanel` | `"BottomPanel"` | 工作区底部 tab + 容器区域 |

注意：`ShellRegions` 目前**全仓零消费方**，属存量公共契约；本次目录拆分只挪位置，类型名（`ShellRegions`）与常量值不变。

### `INavigationItemContribution`（Contributions/INavigationItemContribution.cs）

模块向 ActivityBar 贡献导航项。

- `string Id { get; }` — 稳定标识，**同一模块内唯一**（注意：非全局唯一，与其他贡献接口不同）
- `string Title { get; }` — 显示标题（ToolTip 与 SideBar 标题）
- `string IconPath { get; }` — 图标 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色
- `int Order { get; }` — 同一 `Placement` 内排序权重，小者靠前
- `NavigationItemPlacement Placement { get; }` — 放在 ActivityBar 顶部（`Top`，功能域导航）还是底部（`Bottom`，设置类入口）
- `Type ContentViewType { get; }` — 选中时 SideBar 显示的内容视图类型，经容器解析以支持 DI

配套枚举 `NavigationItemPlacement`（同文件）：`Top` / `Bottom`。

### `IMainViewContribution`（Contributions/IMainViewContribution.cs）

模块向 MainContent 贡献主视图。

- `string Id { get; }` — 稳定标识，**跨模块全局唯一**；shell 按 Id 索引全部贡献；注释建议以模块名做前缀（如 `dashboard.overview`）
- `Type ViewType { get; }` — 打开时 MainContent 显示的视图类型，经容器解析

调用链：SideBar 内交互发出 `OpenMainViewEvent`（负载为 `Id`）→ shell 找到对应贡献 → 容器解析 `ViewType` → 替换 MainContent 当前视图。

### `IPanelTabContribution`（Contributions/IPanelTabContribution.cs）

模块向 AuxiliaryPanel / BottomPanel 贡献面板 tab。

- `string Id { get; }` — tab 稳定标识，**跨两个面板全局唯一**
- `string Title { get; }` — 显示标题
- `string IconPath { get; }` — 同上图标约定
- `int Order { get; }` — **同一面板内**排序权重，小者靠前
- `PanelPlacement Panel { get; }` — `Auxiliary`（右侧 AuxiliaryPanel）或 `Bottom`（底部 BottomPanel）
- `Type ContentViewType { get; }` — 激活该 tab 时面板内容区显示的视图类型，经容器解析

配套枚举 `PanelPlacement`（同文件）：`Auxiliary` / `Bottom`。注释声明不变量：面板收起期间其 tab 的激活操作会被 `ShellLayoutState` 拒绝。

### `IMenuItemContribution`（Menus/IMenuItemContribution.cs）

模块向菜单栏贡献菜单项的契约（路径/分组模型，见 ADR-0001 `docs/adr/0001-attribute-menu-registration.md`）。文件 `using System.Windows.Input;`。**通常不直接实现本接口**：模块用 `MenuGroupAttribute`/`MenuItemAttribute` 标注普通类（见下两节），经 Framework 侧 `MenuRegistration.RegisterMenus` 扫描后生成本契约的实现注册进容器；shell 收集全部实现后由 `MenuTreeBuilder` 建树（分组排序、组间分隔线）并渲染。

| 属性 | 类型 | 语义与排序规则 |
|---|---|---|
| `Title` | `string` | 显示标题，**已按当前 UI 区域性解析**（非资源键） |
| `IconPath` | `string?` | 图标 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色；`null` = 无图标 |
| `Path` | `string` | 完整菜单路径：`"/"` 分隔，段为 Language 资源键，首段为顶层菜单；支持任意深度子菜单 |
| `Group` | `string?` | 单段路径：本条目在该菜单内的组；多段路径：末端子菜单节点在其父菜单内的组。`null` = 默认组（排在命名组之前） |
| `GroupOrder` | `int` | 组的排序权重，小者靠前；同名组多处声明冲突时取最小值 |
| `NodeOrder` | `int` | 顶层菜单（单段路径）或末端子菜单节点（多段路径）在父级中的排序权重；多处声明取最小值 |
| `Order` | `int` | 条目在组内的排序权重，小者靠前；同 `Order` 按解析后的 `Title` 字典序（Ordinal） |
| `Command` | `ICommand` | 点击菜单项执行的命令（`System.Windows.Input.ICommand`） |

与旧模型的差异（ADR-0001）：删除 `Id`（菜单链路无任何消费方）与「追加到哪个顶层菜单」的封闭枚举定位；定位完全由 `Path` + `Group`/`GroupOrder` + `NodeOrder`/`Order` 表达，可表达多级子菜单、命名分组与组间自动分隔线。建树与排序语义（顶层不分组、子菜单组间插分隔线、位次冲突取最小）在 Framework 侧 `MenuTreeBuilder`，不在本程序集。

### `MenuGroupAttribute`（Menus/MenuGroupAttribute.cs）

`[AttributeUsage(AttributeTargets.Class)]`（第 12 行），声明一个菜单类：类中标注 `MenuItemAttribute` 的公共实例方法成为菜单项，经 `MenuRegistration.RegisterMenus` 扫描注册。主构造参 `string path`（第 13 行）。

| 成员 | 类型 | 语义 |
|---|---|---|
| `Path`（构造参，get-only） | `string` | 菜单路径：`"/"` 分隔的多级 Language 资源键，首段为顶层菜单；各段 Trim 后按序精确匹配（Ordinal 大小写敏感） |
| `Group`（命名属性） | `string?` | 分组名；`null` = 默认组（GroupOrder 视为 0，排在命名组之前） |
| `GroupOrder`（命名属性） | `int` | 组的排序权重，小者靠前；同名组多处声明冲突时取最小值 |
| `Order`（命名属性） | `int` | 顶层菜单或末端子菜单节点在父级中的排序权重；多处声明取最小值。**缺省 `int.MaxValue`**（MenuGroupAttribute.cs 第 34 行）：未声明视为「无位次意见」，排最后，且不参与多处声明取最小——防止忘写 `Order` 的类以缺省 0 把所在菜单钉到最前 |

路径段数决定三个命名属性的语义（ADR-0001 第 10 条）：**单段路径**（如 `"MenuFileTitle"`）时 `Group`/`GroupOrder` 描述方法项在该菜单内的分组，`Order` 描述顶层菜单在菜单栏的位次；**多段路径**（如 `"MenuFileTitle/Export"`）时三者描述末端子菜单节点在其父菜单内的分组与位次，方法项进入末端菜单的默认组。一个 attribute 只有一套分组参数，深层子菜单内部分组需拆类声明。含空段（`"A//B"`）的路径整体非法，扫描时记日志跳过。

### `MenuItemAttribute`（Menus/MenuItemAttribute.cs）

`[AttributeUsage(AttributeTargets.Method)]`（第 7 行），声明一个菜单项，标注在菜单类（`MenuGroupAttribute`）的公共实例方法上。主构造参 `string title`（第 8 行）。

| 成员 | 类型 | 语义 |
|---|---|---|
| `Title`（构造参，get-only） | `string` | 显示标题的 Language 资源键，运行时解析，缺键回退键名本身 |
| `Order`（命名属性） | `int` | 同组内的排序权重，小者靠前；同 `Order` 按解析后的标题字典序 |
| `Icon`（命名属性） | `string?` | 图标的 StreamGeometry path 字符串（取 `Icons` 常量）；`null` = 无图标 |

方法签名仅支持无参 `void M()` 与 `Task M()`；非法签名（带参、返回值非 `void`/`Task`）在扫描时记 `Logger.Warning` 跳过（ADR-0001 第 7 条）。

### `IStatusBarItemContribution`（Contributions/IStatusBarItemContribution.cs）

模块向状态栏追加条目，shell 渲染为「图标 + 文本」的状态指示。

- `string Id { get; }` — 稳定标识，全局唯一
- `string Title { get; }` — 显示文本
- `string IconPath { get; }` — 同上图标约定
- `int Order { get; }` — 排序权重，小者靠前

（此接口**没有**定位枚举和行为字段——状态栏只有一个区域，且条目是纯展示无点击行为。）

## 视图类型属性对照（ContentViewType vs ViewType）

「激活时要显示哪个视图」这一能力由 `System.Type` 属性承载（注释均为「经容器解析以支持依赖注入」），但**分布在三个接口上且命名不统一**：

| 属性名 | 所在接口（文件） | 激活效果 |
|---|---|---|
| `ContentViewType` | `INavigationItemContribution`（Contributions/INavigationItemContribution.cs） | 选中导航项时 SideBar 显示解析出的视图 |
| `ContentViewType` | `IPanelTabContribution`（Contributions/IPanelTabContribution.cs） | 激活 tab 时面板内容区显示解析出的视图 |
| `ViewType` | `IMainViewContribution`（Contributions/IMainViewContribution.cs） | 打开时**替换** MainContent 当前视图 |

即：`ContentViewType` 属于 `INavigationItemContribution` 与 `IPanelTabContribution` 两个接口；`ViewType` 只属于 `IMainViewContribution`。其余两个贡献接口（`IMenuItemContribution` 用 `ICommand Command`、`IStatusBarItemContribution` 无行为字段）不携带视图类型。

## 窗口管理契约（WindowManager/）

### `IWindowManager`（WindowManager/IWindowManager.cs）

以「窗口显示与隐藏管理」为职责，11 个方法，`using Avalonia.Controls;`：

- `Window GetWindow(Type type)` — 从容器解析得到指定类型窗口实例
- `void ShowWindow(Type type)` / `void ShowWindow(Type type, object dataContext)`
- `void ShowWindow(Window window)` / `void ShowWindow(Window window, object dataContext)` — 实例版重载（XML 注释写的是「对话框窗口」，与 `ShowDialog` 实例版注释雷同，属注释瑕疵）
- `void ShowDialog(Type type)` / `void ShowDialog(Type type, object dataContext)`
- `void ShowDialog(Window window)` / `void ShowDialog(Window window, object dataContext)`
- `void CloseWindow(Type type)` — 关闭指定类型窗口
- `void HideWindow(Type type)` — 隐藏指定类型窗口

返回类型注意：`GetWindow(Type)` 返回非空 `Window`（`WindowManagerExtenstion.GetWindow<TWindow>` 却声明返回 `Window?`——可空性标注在两层不一致，见 pitfalls.md）。

### `IMainWindowManager`（WindowManager/IMainWindowManager.cs）

- `void HandleMainWindow()` — 处理主窗口的显示与隐藏
- `void HideMainWindow()` — 隐藏主窗口
- `void ShowMainWindow()` — 显示主窗口
- `void CloseWindowsExceptMain()` — 关闭除主窗口外的所有窗口

### `WindowManagerExtenstion`（static class，WindowManager/IWindowManagerExtenstion.cs）

文件名与类名中 "Extenstion" 为源码原始拼写（Extension 的拼写错误）。泛型便捷方法，全部转发到 `IWindowManager` 的 `Type` 版方法：

- `Window? GetWindow<TWindow>(this IWindowManager manager) where TWindow : Window` → `manager.GetWindow(typeof(TWindow))`
- `void ShowWindow<TWindow>(this IWindowManager manager) where TWindow : Window`
- `void ShowWindow<TWindow>(this IWindowManager manager, object dataContext)` — **无** `where TWindow : Window` 约束
- `void ShowDialog<TWindow>(this IWindowManager manager) where TWindow : Window`
- `void ShowDialog<TWindow>(this IWindowManager manager, object dataContext)` — **无**约束
- `void HideWindow<TWindow>(this IWindowManager manager) where TWindow : Window`
- `void CloseWindow<TWindow>(this IWindowManager manager) where TWindow : Window`

## 调用方式与生命周期

- **贡献接口**：无主动调用方 API。模块实现接口并在 `Prism.Ioc.IContainerRegistry` 注册（生命周期由模块注册方式决定），shell 收集消费；菜单例外——`IMenuItemContribution` 通常无手写实现，模块类标注 `MenuGroupAttribute`/`MenuItemAttribute` 后在 `RegisterTypes` 调 `RegisterMenus(Assembly)`，由 Framework 侧扫描生成实现并以接口注册。本模块内无调用点——本程序集是纯定义层，典型调用序列发生在 shell 与其他模块（不在本模块范围）。
- **窗口管理**：调用方注入 `IWindowManager`/`IMainWindowManager`，调 `ShowWindow<MyDialog>(vm)` 这类泛型扩展或直接 `ShowWindow(typeof(MyDialog), vm)`。窗口实例来源是 DI 容器（`GetWindow` 注释：「从容器中解析得到的窗口实例」）。
- **数据结构**：本模块不定义任何 DTO/记录类；对外数据完全由上述接口属性承载，字段语义见上。
