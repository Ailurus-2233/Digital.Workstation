# Framework — 文件结构与功能

目录树（相对 `Core/Framework/`，省略 `obj/` 与 `Output/` 构建产物）：

```
Core/Framework/
├── Framework.csproj                       项目文件：net10.0；引用 Abstractions/Common/Models/UIPackage 四项目与 Avalonia/Prism 相关包
├── FrameworkApplication.cs                应用入口基类与启动序列
├── Shell/
│   ├── ShellLayoutState.cs                布局状态根 record + 全部转换方法
│   ├── SideBarState.cs                    SideBar 区域状态 record
│   ├── AuxiliaryPanelState.cs             AuxiliaryPanel 区域状态 record
│   ├── BottomPanelState.cs                BottomPanel 区域状态 record
│   ├── MainContentState.cs                MainContent 区域状态 record
│   ├── PanelResizeTarget.cs               可调尺寸区域枚举
│   └── ShellContributionCollector.cs      shell 贡献收集器
└── WindowManager/
    └── FrameworkWindowManager.cs          IWindowManager + IMainWindowManager 实现
```

## 各文件功能

### `Framework.csproj`
`net10.0`、`ImplicitUsings`+`Nullable` 开启。ProjectReference：`..\Abstractions`、`..\Common`、`..\Models`、`..\UIPackage`（第 10-13 行）。PackageReference：`Avalonia.Desktop`/`Avalonia.Fonts.Inter`/`Avalonia.Themes.Fluent` 11.3.20、`AvaloniaUI.DiagnosticsSupport` 2.1.1、`CommunityToolkit.Mvvm` 8.4.0（第 17-21 行）。

### `FrameworkApplication.cs`
定义 `public abstract class FrameworkApplication<TWindow> : PrismApplication where TWindow : Window`（第 16 行，命名空间 `DigitalWorkstation.Core.Framework`）。关键入口：
- `Initialize()`（第 21 行）：主题装载（Dark + `WorkstationTheme` + `VSCodePalette.ApplyTo`）。
- `OnFrameworkInitializationCompleted()`（第 35 行）→ `RunStartupSequenceAsync()`（第 64 行）：三阶段启动序列（ADR-0004）。
- `OnInitialized()`（第 44 行）、`InitializeModules()`（第 51 行）：两个故意的空覆盖。
- `CreateSplashWindow()`（第 58 行，abstract）/`RegisterCustomService()`（第 183 行，virtual）：子类扩展点。
- `RegisterTypes()`（第 171 行）→ `RegisterFrameworkServices`（第 142 行）：`IoC.Initialize`、窗口管理器双接口单例、`ShellContributionCollector` 单例注册。
- `ConfigureViewModelLocator()`（第 205 行）：约定式 ViewModel 定位解析器。

### `Shell/ShellLayoutState.cs`
`public sealed record ShellLayoutState`（第 7 行）：五个区域状态属性 + `static Initial`（第 22 行）。转换方法：`SelectActivity`（:28）、`ToggleSideBar`（:45）、`ToggleAuxiliaryPanel`（:53）、`ToggleBottomPanel`（:61）、`ActivateAuxTab`（:69）、`ActivateBottomTab`（:82）、`OpenMainView`（:95）、`Resize`（:103）、私有 `Clamp`（:134）。注释自述："原型验证过的 reducer 的正式实现"。

### `Shell/SideBarState.cs`
`public sealed record SideBarState`（第 6 行）：`const MinWidth=120 / MaxWidth=480`；`Visible`（默认 false）、`Width=240`、`ContentFor`（string?，收起时保留导航项 Id）。

### `Shell/AuxiliaryPanelState.cs`
`public sealed record AuxiliaryPanelState`（第 6 行）：`const MinWidth=120 / MaxWidth=480`；`Visible=true`、`Width=280`、`Tabs=[]`、`ActiveTab`（string?）。

### `Shell/BottomPanelState.cs`
`public sealed record BottomPanelState`（第 6 行）：`const MinHeight=80 / MaxHeight=480`；`Visible=true`、`Height=160`、`Tabs=[]`、`ActiveTab`（string?）。

### `Shell/MainContentState.cs`
`public sealed record MainContentState`（第 6 行）：仅 `ActiveView`（string?），单视图切换。

### `Shell/PanelResizeTarget.cs`
`public enum PanelResizeTarget`（第 6 行）：`SideBar` / `AuxiliaryPanel` / `BottomPanel`，`ShellLayoutState.Resize` 的目标参数。

### `Shell/ShellContributionCollector.cs`
`public class ShellContributionCollector(IContainerProvider containerProvider)`（第 8 行，主构造）。五个收集方法：`GetNavigationItems(NavigationItemPlacement)`（:13）、`GetMainViews()`（:23）、`GetPanelTabs(PanelPlacement)`（:30）、`GetMenuItems(MenuPlacement)`（:40）、`GetStatusBarItems()`（:50）。统一模式：容器解析 `IEnumerable<T>` → 定位枚举过滤 → `Order` 升序 → `ToArray()`。

### `WindowManager/FrameworkWindowManager.cs`
`public class FrameworkWindowManager : IWindowManager, IMainWindowManager`（第 12 行，命名空间 `DigitalWorkstation.Core.Framework.WindowManager`）。内部状态 `_windowMap: Dictionary<Type, Window>`（:17）与 `_mainWindow`（:22）。入口：`GetWindow`（:35）、`InitializeWindow`（:45，私有）、`ShowWindow` 四重载（:57-111）、`ShowDialog` 四重载（:113-141）、`CloseWindow`（:144）、`HideWindow`（:150）、`HandleMainWindow`（:158）、`HideMainWindow`/`ShowMainWindow`（:169-170）、`CloseWindowsExceptMain`（:172）。

## 对应测试文件（UnitTest/Framework/）

```
UnitTest/Framework/
├── Framework.csproj                  xUnit 测试项目：Microsoft.NET.Test.Sdk 17.6.0 + xunit 2.4.2 + xunit.runner.visualstudio 2.4.5
└── ShellLayoutStateResizeTests.cs    11 个 [Fact]：Resize 增减/clamp/区域隔离/收起恢复保留尺寸
```
