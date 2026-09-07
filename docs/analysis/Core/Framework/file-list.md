# Framework — 文件结构与功能

目录树（相对 `Core/Framework/`，省略 `obj/` 与 `Output/` 构建产物）：

```
Core/Framework/
├── Framework.csproj                       项目文件：net10.0；引用 Abstractions/Common/Models/UIPackage/Resource 五项目与 Avalonia/Prism/Ursa 相关包
├── FrameworkApplication.cs                应用入口基类与启动序列
├── Shell/
│   ├── ShellLayoutState.cs                布局状态根 record + 全部转换方法
│   ├── SideBarState.cs                    SideBar 区域状态 record
│   ├── AuxiliaryPanelState.cs             AuxiliaryPanel 区域状态 record
│   ├── BottomPanelState.cs                BottomPanel 区域状态 record
│   ├── MainContentState.cs                MainContent 区域状态 record
│   ├── PanelResizeTarget.cs               可调尺寸区域枚举
│   ├── PanelAlignment.cs                  面板对齐枚举（FrameworkWindow 基础布局四档）
│   ├── PanelResize.cs                     分隔条命令参数 record struct
│   ├── PanelResizer.cs                    面板分隔条（GridSplitter 改造，自 Modules/Workstation 迁入）
│   ├── FrameworkWindow.cs                 带基础布局的窗口基类（内置 VS Code 式五区 shell）
│   ├── SetPanelAlignmentEvent.cs          面板对齐切换事件契约（PubSubEvent<PanelAlignment>）
│   ├── FrameworkWindowTheme.axaml         基础布局主题资源（四份布局模板 + 共享部件模板 + shell 样式）
│   ├── FrameworkWindowTheme.cs            主题加载器（StyleInclude 强制加载）
│   └── ShellContributionCollector.cs      shell 贡献收集器
└── WindowManager/
    └── FrameworkWindowManager.cs          IWindowManager + IMainWindowManager 实现
```

## 各文件功能

### `Framework.csproj`
`net10.0`、`ImplicitUsings`+`Nullable` 开启。ProjectReference：`..\Abstractions`、`..\Common`、`..\Models`、`..\UIPackage`、`..\Resource`（第 10-14 行）。PackageReference：`Avalonia.Desktop`/`Avalonia.Fonts.Inter`/`Avalonia.Themes.Fluent` 11.3.20、`AvaloniaUI.DiagnosticsSupport` 2.1.1、`CommunityToolkit.Mvvm` 8.4.0、`Irihi.Ursa` 1.15.1（第 18-23 行，FrameworkWindow 直接继承 UrsaWindow）。

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

### `Shell/PanelAlignment.cs`
`public enum PanelAlignment`（第 7 行）：`Left`/`Right`/`Center`/`Justify` 四档，FrameworkWindow 基础布局的档位，决定 BottomPanel 在窗口底部的水平跨度；注释自述"面板不占的列由侧栏通高到底吸收，不留空挡"。

### `Shell/PanelResize.cs`
`public readonly record struct PanelResize(PanelResizeTarget Target, double Delta)`（第 6 行）：分隔条命令参数，Delta 为已换算方向的尺寸增量。

### `Shell/PanelResizer.cs`
`public class PanelResizer : GridSplitter`（第 13 行），自 Modules/Workstation 迁入并改造。成员：`ResizeCommandProperty` StyledProperty（:15）、CLR 属性 `Target`（:31，决定增量取轴与取反）与 `ResizeCommand`（:36）、`StyleKeyOverride => typeof(GridSplitter)`（:26，继承 GridSplitter 主题）、`GetParentGrid() => null`（:46，禁用原生重排）、私有 `OnDragDelta`（:54，方向换算 SideBar=+X、AuxiliaryPanel=-X、BottomPanel=-Y 后 `ResizeCommand?.Execute(new PanelResize(Target, delta))`）。

### `Shell/FrameworkWindow.cs`
`public abstract class FrameworkWindow : UrsaWindow`（第 14 行）——带基础布局的窗口基类，内置 VS Code 式五区 shell。成员：`PanelAlignmentProperty`（:16，StyledProperty，默认 `Center`）与 CLR 包装 `PanelAlignment`（:39）、`StyleKeyOverride => typeof(UrsaWindow)`（:34，继承窗口 chrome 主题）、构造函数（:22，`Styles.Add(_theme)` + ContentControl 布局宿主 + `UpdateLayoutTemplate`）、`OnPropertyChanged`（:45，监听 PanelAlignmentProperty）、私有 `UpdateLayoutTemplate`（:54，枚举→资源键映射 + `TryGetResource` 查找，缺失抛异常）。

### `Shell/SetPanelAlignmentEvent.cs`
`public class SetPanelAlignmentEvent : PubSubEvent<PanelAlignment>`（第 8 行）：请求切换布局档位的事件契约。注释自述放本模块而非 Core/Models 的原因（负载类型定义于此，Models 引用 Framework 会成环）。

### `Shell/FrameworkWindowTheme.axaml`
无 x:Class 的 Styles 根（550 行）。`Styles.Resources`：`NavigationItemTemplate`（:8）、六个共享部件 DataTemplate（`ShellActivityBar` :23 / `ShellSideBar` :39 / `ShellMainContent` :55 / `ShellAuxiliaryPanel` :67 / `ShellBottomPanel` :116 / `ShellStatusBar` :166）、四份布局 DataTemplate（`WindowLayoutLeft` :195 / `WindowLayoutRight` :261 / `WindowLayoutCenter` :327 / `WindowLayoutJustify` :392，差异为 BottomPanel 及分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`，ActivityBar 恒 `RowSpan=2` 通高）。样式（:458 起）自 Modules/Workstation/MainWindow.axaml 迁入。

### `Shell/FrameworkWindowTheme.cs`
`public class FrameworkWindowTheme : Styles`（第 12 行）：`StyleInclude`（BaseUri `avares://DigitalWorkstation.Core.Framework/Shell/`，:14；Source 相对 `FrameworkWindowTheme.axaml`，:20）加载主题，构造时 `_ = include.Loaded` 强制加载（:22）再 `Add(include)`（:23）。

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
