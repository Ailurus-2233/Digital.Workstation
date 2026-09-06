# Abstractions — 对外接口与调用方式

命名空间两组：`DigitalWorkstation.Core.Abstractions.Shell`（Shell/ 目录）与 `DigitalWorkstation.Core.Abstractions.WindowManager`（WindowManager/ 目录）。全部为 `public`；项目无 internal 类型。

## Shell 贡献契约（Shell/）

### `ShellRegions`（static class，Shell/ShellRegions.cs）

Prism Region 名称常量，值均经 `nameof` 生成：

| 常量 | 值 | 语义 |
|---|---|---|
| `ActivityBar` | `"ActivityBar"` | 工作区最左侧竖向导航栏 |
| `SideBar` | `"SideBar"` | ActivityBar 右侧容器，显示当前选中导航项内容 |
| `MainContent` | `"MainContent"` | 工作区中央主 Region，单视图切换 |
| `AuxiliaryPanel` | `"AuxiliaryPanel"` | 工作区右侧 tab + 容器区域 |
| `BottomPanel` | `"BottomPanel"` | 工作区底部 tab + 容器区域 |

### `INavigationItemContribution`（Shell/INavigationItemContribution.cs）

模块向 ActivityBar 贡献导航项。

- `string Id { get; }` — 稳定标识，**同一模块内唯一**（注意：非全局唯一，与其他贡献接口不同）
- `string Title { get; }` — 显示标题（ToolTip 与 SideBar 标题）
- `string IconPath { get; }` — 图标 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色
- `int Order { get; }` — 同一 `Placement` 内排序权重，小者靠前
- `NavigationItemPlacement Placement { get; }` — 放在 ActivityBar 顶部（`Top`，功能域导航）还是底部（`Bottom`，设置类入口）
- `Type ContentViewType { get; }` — 选中时 SideBar 显示的内容视图类型，经容器解析以支持 DI

配套枚举 `NavigationItemPlacement`（同文件）：`Top` / `Bottom`。

### `IMainViewContribution`（Shell/IMainViewContribution.cs）

模块向 MainContent 贡献主视图。

- `string Id { get; }` — 稳定标识，**跨模块全局唯一**；shell 按 Id 索引全部贡献；注释建议以模块名做前缀（如 `dashboard.overview`）
- `Type ViewType { get; }` — 打开时 MainContent 显示的视图类型，经容器解析

调用链：SideBar 内交互发出 `OpenMainViewEvent`（负载为 `Id`）→ shell 找到对应贡献 → 容器解析 `ViewType` → 替换 MainContent 当前视图。

### `IPanelTabContribution`（Shell/IPanelTabContribution.cs）

模块向 AuxiliaryPanel / BottomPanel 贡献面板 tab。

- `string Id { get; }` — tab 稳定标识，**跨两个面板全局唯一**
- `string Title { get; }` — 显示标题
- `string IconPath { get; }` — 同上图标约定
- `int Order { get; }` — **同一面板内**排序权重，小者靠前
- `PanelPlacement Panel { get; }` — `Auxiliary`（右侧 AuxiliaryPanel）或 `Bottom`（底部 BottomPanel）
- `Type ContentViewType { get; }` — 激活该 tab 时面板内容区显示的视图类型，经容器解析

配套枚举 `PanelPlacement`（同文件）：`Auxiliary` / `Bottom`。注释声明不变量：面板收起期间其 tab 的激活操作会被 `ShellLayoutState` 拒绝。

### `IMenuItemContribution`（Shell/IMenuItemContribution.cs）

模块向菜单栏追加菜单项。文件 `using System.Windows.Input;`。

- `string Id { get; }` — 稳定标识，全局唯一
- `string Title { get; }` — 显示标题
- `string IconPath { get; }` — 同上图标约定
- `int Order { get; }` — 同一菜单内排序权重，小者靠前
- `MenuPlacement Menu { get; }` — 追加到哪个顶层菜单
- `ICommand Command { get; }` — 点击菜单项执行的命令（`System.Windows.Input.ICommand`）

配套枚举 `MenuPlacement`（同文件）：`File` / `View` / `Help`。shell 预置项（退出、面板显隐切换、关于）与模块贡献项经同一机制渲染。

### `IStatusBarItemContribution`（Shell/IStatusBarItemContribution.cs）

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
| `ContentViewType` | `INavigationItemContribution`（Shell/INavigationItemContribution.cs） | 选中导航项时 SideBar 显示解析出的视图 |
| `ContentViewType` | `IPanelTabContribution`（Shell/IPanelTabContribution.cs） | 激活 tab 时面板内容区显示解析出的视图 |
| `ViewType` | `IMainViewContribution`（Shell/IMainViewContribution.cs） | 打开时**替换** MainContent 当前视图 |

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

- **贡献接口**：无主动调用方 API。模块实现接口并在 `Prism.Ioc.IContainerRegistry` 注册（生命周期由模块注册方式决定），shell 收集消费。本模块内无调用点——本程序集是纯定义层，典型调用序列发生在 shell 与其他模块（不在本模块范围）。
- **窗口管理**：调用方注入 `IWindowManager`/`IMainWindowManager`，调 `ShowWindow<MyDialog>(vm)` 这类泛型扩展或直接 `ShowWindow(typeof(MyDialog), vm)`。窗口实例来源是 DI 容器（`GetWindow` 注释：「从容器中解析得到的窗口实例」）。
- **数据结构**：本模块不定义任何 DTO/记录类；对外数据完全由上述接口属性承载，字段语义见上。
