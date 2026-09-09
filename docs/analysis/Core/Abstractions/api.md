# Abstractions — 对外接口与调用方式

命名空间四组：`DigitalWorkstation.Core.Abstractions.Contributions`（Contributions/ 目录，主视图/状态栏两接口 + 工具视图枚举/attribute/元数据三类型）、`DigitalWorkstation.Core.Abstractions.Menus`（Menus/ 目录，菜单路径/分组模型三类型）、`DigitalWorkstation.Core.Abstractions.Regions`（Regions/ 目录，仅 `ShellRegions` 常量）与 `DigitalWorkstation.Core.Abstractions.WindowManager`（WindowManager/ 目录）。全部为 `public`；项目无 internal 类型。

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

### `ToolViewPlacement`（enum，Contributions/ToolViewAttribute.cs 第 6-22 行）

工具视图默认栖身的 Bar：`ActivityBar`（内容显示在 SideBar）/ `AuxiliaryPanel`（右侧面板）/ `BottomPanel`（底部面板）。

### `ToolViewAttribute`（Contributions/ToolViewAttribute.cs）

`[AttributeUsage(AttributeTargets.Class)]`（第 29 行），声明一个 View 类是**工具视图（Tool View）**（ADR-0002 `docs/adr/0002-toolview-drag-persistence.md`）：带图标与标题的可停靠界面单元。模块在 `RegisterTypes` 调 `RegisterToolViews(Assembly)`（Framework 侧 `ToolViewRegistration`）扫描注册；`Default` 只是默认归属——用户拖拽后的实际归属以持久化布局为准。主构造参 `string id, string titleKey`（第 30 行）。

| 成员 | 类型 | 语义 |
|---|---|---|
| `Id`（构造参，get-only） | `string` | 稳定标识，全局唯一，约定模块名前缀（如 `"shell.outline"`） |
| `TitleKey`（构造参，get-only） | `string` | 显示标题的 Language 资源键，注册时解析，缺键回退键名本身 |
| `Icon`（命名属性） | `string?` | 图标的 StreamGeometry path 字符串（取 `Icons` 常量）；`null` = 无图标 |
| `Default`（命名属性） | `ToolViewPlacement` | 默认栖身的 Bar；**缺省 `AuxiliaryPanel`**（第 50 行） |
| `Order`（命名属性） | `int` | 同一 Bar 内的默认排序权重，小者靠前 |
| `AllowMove`（命名属性） | `bool` | 是否允许用户拖拽迁移；**缺省 `true`**（第 60 行）；`false` 且 `Default` 为 `ActivityBar` 时钉在 ActivityBar 底部段（钉住项，Pinned Item） |

### `IMainViewContribution`（Contributions/IMainViewContribution.cs）

模块向 MainContent 贡献主视图。

- `string Id { get; }` — 稳定标识，**跨模块全局唯一**；shell 按 Id 索引全部贡献；注释建议以模块名做前缀（如 `dashboard.overview`）
- `Type ViewType { get; }` — 打开时 MainContent 显示的视图类型，经容器解析

调用链：SideBar 内交互发出 `OpenMainViewEvent`（负载为 `Id`）→ shell 找到对应贡献 → 容器解析 `ViewType` → 替换 MainContent 当前视图。

### `ToolViewContribution`（sealed class，Contributions/ToolViewContribution.cs）

工具视图的贡献元数据（ADR-0002）：**不由模块手写**，由 Framework 侧 `RegisterToolViews` 扫描 `ToolViewAttribute` 生成并以单例注册进容器；shell 收集后渲染到 `Placement` 对应的 Bar；激活时经容器解析 `ViewType` 显示内容。全部属性为 `required init`（第 14-44 行）。

| 属性 | 类型 | 语义 |
|---|---|---|
| `Id` | `string` | 稳定标识，全局唯一 |
| `Title` | `string` | 显示标题，**已按当前 UI 区域性解析**（非资源键） |
| `IconPath` | `string?` | 图标 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色；`null` = 无图标 |
| `Order` | `int` | 同一 Bar 内的默认排序权重，小者靠前 |
| `Placement` | `ToolViewPlacement` | 默认栖身的 Bar；用户拖拽后的实际归属以持久化布局为准 |
| `AllowMove` | `bool` | 是否允许用户拖拽迁移 |
| `ViewType` | `Type` | 内容视图类型，经容器解析以支持依赖注入 |

注释声明不变量：面板收起期间其 tab 的激活操作会被 `ShellLayoutState` 拒绝（ToolViewContribution.cs 第 7 行）。

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

## 视图类型属性（ViewType）

「激活时要显示哪个视图」这一能力由 `System.Type` 属性承载（注释均为「经容器解析以支持依赖注入」）。原 `ContentViewType`（已删的导航项/面板 tab 接口）已不复存在，命名统一为 `ViewType`：

| 属性名 | 所在类型（文件） | 激活效果 |
|---|---|---|
| `ViewType` | `ToolViewContribution`（Contributions/ToolViewContribution.cs 第 44 行） | 点击导航项时 SideBar 显示、激活面板 tab 时面板内容区显示解析出的视图 |
| `ViewType` | `IMainViewContribution`（Contributions/IMainViewContribution.cs 第 19 行） | 打开时**替换** MainContent 当前视图 |

其余贡献类型（`IMenuItemContribution` 用 `ICommand Command`、`IStatusBarItemContribution` 无行为字段）不携带视图类型。

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

- **贡献契约**：无主动调用方 API。接口类贡献（`IMainViewContribution`/`IStatusBarItemContribution`）由模块实现接口并在 `Prism.Ioc.IContainerRegistry` 以接口注册（生命周期由模块注册方式决定），shell 收集消费；工具视图与菜单例外——模块在 `RegisterTypes` 分别调 `RegisterToolViews(Assembly)`（View 类标 `ToolViewAttribute`）、`RegisterMenus(Assembly)`（菜单类标 `MenuGroupAttribute`/`MenuItemAttribute`），由 Framework 侧扫描生成 `ToolViewContribution` 元数据/`IMenuItemContribution` 实现并注册。本模块内无调用点——本程序集是纯定义层，典型调用序列发生在 shell 与其他模块（不在本模块范围）。
- **窗口管理**：调用方注入 `IWindowManager`/`IMainWindowManager`，调 `ShowWindow<MyDialog>(vm)` 这类泛型扩展或直接 `ShowWindow(typeof(MyDialog), vm)`。窗口实例来源是 DI 容器（`GetWindow` 注释：「从容器中解析得到的窗口实例」）。
- **数据结构**：本模块不定义任何 DTO/记录类；对外数据完全由上述接口属性承载，字段语义见上。
