# Abstractions — 文件结构与功能

相对 `Core/Abstractions/` 的目录树（共 12 个文件，含 csproj；无子目录嵌套超过一层，无测试、无资源文件）：

```
Abstractions.csproj
Contributions/
├── INavigationItemContribution.cs
├── IMainViewContribution.cs
├── IPanelTabContribution.cs
└── IStatusBarItemContribution.cs
Menus/
├── IMenuItemContribution.cs
├── MenuGroupAttribute.cs
└── MenuItemAttribute.cs
Regions/
└── ShellRegions.cs
WindowManager/
├── IWindowManager.cs
├── IMainWindowManager.cs
└── IWindowManagerExtenstion.cs
```

## 逐文件说明

### Abstractions.csproj

项目文件。`Microsoft.NET.Sdk`，`net10.0`，`ImplicitUsings` + `Nullable` 开启；唯一依赖 `Avalonia 11.3.20`。无 `ProjectReference`。

### Regions/ShellRegions.cs

Prism Region 名称常量。定义 `public static class ShellRegions`，含 5 个 `public const string`：`ActivityBar`、`SideBar`、`MainContent`、`AuxiliaryPanel`、`BottomPanel`（值均 `nameof(自身)`）。命名空间 `DigitalWorkstation.Core.Abstractions.Regions`。目前全仓零消费方，属存量公共契约，目录拆分时只挪位置、类型名不变。

### Contributions/INavigationItemContribution.cs

定义枚举 `NavigationItemPlacement`（`Top`/`Bottom`）与接口 `INavigationItemContribution`（`Id`/`Title`/`IconPath`/`Order`/`Placement`/`ContentViewType`）——模块向 ActivityBar 贡献导航项、SideBar 显示对应内容的契约。

### Contributions/IMainViewContribution.cs

定义接口 `IMainViewContribution`（`Id`/`ViewType`）——模块向 MainContent 贡献主视图的契约；配合 shell 侧 `OpenMainViewEvent`（负载 Id）使用。

### Contributions/IPanelTabContribution.cs

定义枚举 `PanelPlacement`（`Auxiliary`/`Bottom`）与接口 `IPanelTabContribution`（`Id`/`Title`/`IconPath`/`Order`/`Panel`/`ContentViewType`）——模块向右侧 AuxiliaryPanel 或底部 BottomPanel 贡献面板 tab 的契约。

### Contributions/IStatusBarItemContribution.cs

定义接口 `IStatusBarItemContribution`（`Id`/`Title`/`IconPath`/`Order`）——模块向状态栏追加「图标 + 文本」状态指示的契约；无定位枚举、无行为字段。

### Menus/IMenuItemContribution.cs

`using System.Windows.Input;`。定义接口 `IMenuItemContribution`（`Title`/`IconPath?`/`Path`/`Group?`/`GroupOrder`/`NodeOrder`/`Order`/`Command`）——模块向菜单栏贡献菜单项的契约，路径/分组模型（ADR-0001），无 `Id`、无定位枚举。命名空间 `DigitalWorkstation.Core.Abstractions.Menus`。

### Menus/MenuGroupAttribute.cs

定义 `MenuGroupAttribute`（`[AttributeUsage(AttributeTargets.Class)]`，构造参 `path`，命名属性 `Group?`/`GroupOrder`/`Order`）——声明菜单类：类中标注 `MenuItemAttribute` 的公共实例方法成为菜单项，经 Framework 侧 `RegisterMenus` 扫描注册。单段/多段路径语义见 api.md。

### Menus/MenuItemAttribute.cs

定义 `MenuItemAttribute`（`[AttributeUsage(AttributeTargets.Method)]`，构造参 `title` 为 Language 资源键，命名属性 `Order`/`Icon?`）——声明菜单项；方法签名仅支持无参 `void M()`/`Task M()`，非法签名扫描时记日志跳过。

### WindowManager/IWindowManager.cs

`using Avalonia.Controls;`。定义接口 `IWindowManager`（11 个方法）：`GetWindow(Type)`；`ShowWindow` 与 `ShowDialog` 各 4 个重载（`Type`/`Type+dataContext`/`Window`/`Window+dataContext`）；`CloseWindow(Type)`；`HideWindow(Type)`。窗口显隐管理契约。

### WindowManager/IMainWindowManager.cs

定义接口 `IMainWindowManager`（4 个方法）：`HandleMainWindow()`、`HideMainWindow()`、`ShowMainWindow()`、`CloseWindowsExceptMain()`。主窗口显隐与批量关闭契约。

### WindowManager/IWindowManagerExtenstion.cs

`using Avalonia.Controls;`。定义 `public static class WindowManagerExtenstion`（"Extenstion" 为源码原始拼写），7 个泛型扩展方法：`GetWindow<TWindow>`、`ShowWindow<TWindow>`（两个重载）、`ShowDialog<TWindow>`（两个重载）、`HideWindow<TWindow>`、`CloseWindow<TWindow>`，全部转发 `IWindowManager` 的 `Type` 版方法。带 dataContext 的两个重载无 `where TWindow : Window` 约束。
