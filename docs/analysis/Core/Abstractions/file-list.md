# Abstractions — 文件结构与功能

相对 `Core/Abstractions/` 的目录树（共 10 个文件，含 csproj；无子目录嵌套超过一层，无测试、无资源文件）：

```
Abstractions.csproj
Shell/
├── ShellRegions.cs
├── INavigationItemContribution.cs
├── IMainViewContribution.cs
├── IPanelTabContribution.cs
├── IMenuItemContribution.cs
└── IStatusBarItemContribution.cs
WindowManager/
├── IWindowManager.cs
├── IMainWindowManager.cs
└── IWindowManagerExtenstion.cs
```

## 逐文件说明

### Abstractions.csproj

项目文件。`Microsoft.NET.Sdk`，`net10.0`，`ImplicitUsings` + `Nullable` 开启；唯一依赖 `Avalonia 11.3.20`。无 `ProjectReference`。

### Shell/ShellRegions.cs

Prism Region 名称常量。定义 `public static class ShellRegions`，含 5 个 `public const string`：`ActivityBar`、`SideBar`、`MainContent`、`AuxiliaryPanel`、`BottomPanel`（值均 `nameof(自身)`）。命名空间 `DigitalWorkstation.Core.Abstractions.Shell`。

### Shell/INavigationItemContribution.cs

定义枚举 `NavigationItemPlacement`（`Top`/`Bottom`）与接口 `INavigationItemContribution`（`Id`/`Title`/`IconPath`/`Order`/`Placement`/`ContentViewType`）——模块向 ActivityBar 贡献导航项、SideBar 显示对应内容的契约。

### Shell/IMainViewContribution.cs

定义接口 `IMainViewContribution`（`Id`/`ViewType`）——模块向 MainContent 贡献主视图的契约；配合 shell 侧 `OpenMainViewEvent`（负载 Id）使用。

### Shell/IPanelTabContribution.cs

定义枚举 `PanelPlacement`（`Auxiliary`/`Bottom`）与接口 `IPanelTabContribution`（`Id`/`Title`/`IconPath`/`Order`/`Panel`/`ContentViewType`）——模块向右侧 AuxiliaryPanel 或底部 BottomPanel 贡献面板 tab 的契约。

### Shell/IMenuItemContribution.cs

`using System.Windows.Input;`。定义枚举 `MenuPlacement`（`File`/`View`/`Help`）与接口 `IMenuItemContribution`（`Id`/`Title`/`IconPath`/`Order`/`Menu`/`Command`）——模块向菜单栏顶层菜单追加菜单项的契约。

### Shell/IStatusBarItemContribution.cs

定义接口 `IStatusBarItemContribution`（`Id`/`Title`/`IconPath`/`Order`）——模块向状态栏追加「图标 + 文本」状态指示的契约；无定位枚举、无行为字段。

### WindowManager/IWindowManager.cs

`using Avalonia.Controls;`。定义接口 `IWindowManager`（11 个方法）：`GetWindow(Type)`；`ShowWindow` 与 `ShowDialog` 各 4 个重载（`Type`/`Type+dataContext`/`Window`/`Window+dataContext`）；`CloseWindow(Type)`；`HideWindow(Type)`。窗口显隐管理契约。

### WindowManager/IMainWindowManager.cs

定义接口 `IMainWindowManager`（4 个方法）：`HandleMainWindow()`、`HideMainWindow()`、`ShowMainWindow()`、`CloseWindowsExceptMain()`。主窗口显隐与批量关闭契约。

### WindowManager/IWindowManagerExtenstion.cs

`using Avalonia.Controls;`。定义 `public static class WindowManagerExtenstion`（"Extenstion" 为源码原始拼写），7 个泛型扩展方法：`GetWindow<TWindow>`、`ShowWindow<TWindow>`（两个重载）、`ShowDialog<TWindow>`（两个重载）、`HideWindow<TWindow>`、`CloseWindow<TWindow>`，全部转发 `IWindowManager` 的 `Type` 版方法。带 dataContext 的两个重载无 `where TWindow : Window` 约束。
