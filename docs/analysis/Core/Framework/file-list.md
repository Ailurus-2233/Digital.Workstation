# Framework — 文件结构与功能

目录树（相对 `Core/Framework/`，省略 `obj/` 与 `Output/` 构建产物）：

```
Core/Framework/
├── Framework.csproj                       项目文件：net10.0；引用 Abstractions/Common/Models/UIPackage/Resource 五项目与 Avalonia/Prism/Ursa 相关包
├── FrameworkApplication.cs                应用入口基类与启动序列
├── Layout/                                  命名空间 DigitalWorkstation.Core.Framework.Layout
│   ├── ShellLayoutState.cs                布局状态根 record + 全部转换方法
│   ├── SideBarState.cs                    SideBar 区域状态 record
│   ├── AuxiliaryPanelState.cs             AuxiliaryPanel 区域状态 record
│   ├── BottomPanelState.cs                BottomPanel 区域状态 record
│   ├── MainContentState.cs                MainContent 区域状态 record
│   ├── PanelResizeTarget.cs               可调尺寸区域枚举
│   ├── PanelAlignment.cs                  面板对齐枚举（FrameworkWindow 基础布局四档）
│   ├── PanelResize.cs                     分隔条命令参数 record struct
│   ├── SetPanelAlignmentEvent.cs          面板对齐切换事件契约（PubSubEvent<PanelAlignment>）
│   └── PanelResizer.cs                    面板分隔条（GridSplitter 改造，自 Modules/Workstation 迁入）
├── Menus/                                   命名空间 DigitalWorkstation.Core.Framework.Menus
│   ├── MenuItemViewModel.cs               菜单项呈现模型（Title/Icon?/Command?/Children + FromSubmenu 递归转换，自 Modules/Workstation 迁入）
│   ├── MenuTreeEntry.cs                   菜单树条目 record 族（叶子/子菜单/分隔线单例）
│   ├── MenuTreeBuilder.cs                 菜单建树器（纯函数：路径切分、分组排序、插分隔线，ADR-0001）
│   └── MenuRegistration.cs                attribute 菜单注册扩展 + internal 反射贡献实现
├── Contributions/                           命名空间 DigitalWorkstation.Core.Framework.Contributions
│   ├── ShellContributionCollector.cs      shell 贡献收集器
│   └── ToolViewRegistration.cs            attribute 工具视图注册扩展（ADR-0002）
├── Windows/                                 命名空间 DigitalWorkstation.Core.Framework.Windows（窗口基类与主题）
│   ├── FrameworkWindow.cs                 带基础布局的窗口基类（内置 VS Code 式五区 shell + 标题栏菜单栏）
│   ├── FrameworkWindowTheme.axaml         基础布局主题资源（四份布局模板 + 共享部件模板 + shell 样式 + 菜单样式；PanelResizer 经 xmlns:layout 引用）
│   └── FrameworkWindowTheme.cs            主题加载器（StyleInclude 强制加载，BaseUri 指向 Windows/ 目录）
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

### `Layout/ShellLayoutState.cs`
`public sealed record ShellLayoutState`（第 7 行）：五个区域状态属性 + `static Initial`（第 22 行）。转换方法：`SelectActivity`（:28）、`ToggleSideBar`（:45）、`ToggleAuxiliaryPanel`（:53）、`ToggleBottomPanel`（:61）、`ActivateAuxTab`（:69）、`ActivateBottomTab`（:82）、`OpenMainView`（:95）、`Resize`（:103）、私有 `Clamp`（:134）。注释自述："原型验证过的 reducer 的正式实现"。

### `Layout/SideBarState.cs`
`public sealed record SideBarState`（第 6 行）：`const MinWidth=120 / MaxWidth=480`；`Visible`（默认 false）、`Width=240`、`ContentFor`（string?，收起时保留导航项 Id）。

### `Layout/AuxiliaryPanelState.cs`
`public sealed record AuxiliaryPanelState`（第 6 行）：`const MinWidth=120 / MaxWidth=480`；`Visible=true`、`Width=280`、`Tabs=[]`、`ActiveTab`（string?）。

### `Layout/BottomPanelState.cs`
`public sealed record BottomPanelState`（第 6 行）：`const MinHeight=80 / MaxHeight=480`；`Visible=true`、`Height=160`、`Tabs=[]`、`ActiveTab`（string?）。

### `Layout/MainContentState.cs`
`public sealed record MainContentState`（第 6 行）：仅 `ActiveView`（string?），单视图切换。

### `Layout/PanelResizeTarget.cs`
`public enum PanelResizeTarget`（第 6 行）：`SideBar` / `AuxiliaryPanel` / `BottomPanel`，`ShellLayoutState.Resize` 的目标参数。

### `Layout/PanelAlignment.cs`
`public enum PanelAlignment`（第 7 行）：`Left`/`Right`/`Center`/`Justify` 四档，FrameworkWindow 基础布局的档位，决定 BottomPanel 在窗口底部的水平跨度；注释自述"面板不占的列由侧栏通高到底吸收，不留空挡"。

### `Layout/PanelResize.cs`
`public readonly record struct PanelResize(PanelResizeTarget Target, double Delta)`（第 6 行）：分隔条命令参数，Delta 为已换算方向的尺寸增量。

### `Layout/PanelResizer.cs`
`public class PanelResizer : GridSplitter`（第 13 行，命名空间 `DigitalWorkstation.Core.Framework.Layout`），自 Modules/Workstation 迁入并改造。成员：`ResizeCommandProperty` StyledProperty（:15）、CLR 属性 `Target`（:31，决定增量取轴与取反）与 `ResizeCommand`（:36）、`StyleKeyOverride => typeof(GridSplitter)`（:26，继承 GridSplitter 主题）、`GetParentGrid() => null`（:46，禁用原生重排）、私有 `OnDragDelta`（:54，方向换算 SideBar=+X、AuxiliaryPanel=-X、BottomPanel=-Y 后 `ResizeCommand?.Execute(new PanelResize(Target, delta))`）。

### `Windows/FrameworkWindow.cs`
`public abstract class FrameworkWindow : UrsaWindow`（第 19 行）——带基础布局的窗口基类，内置 VS Code 式五区 shell + 标题栏左侧菜单栏（ADR-0001）。成员：`PanelAlignmentProperty`（:21，StyledProperty，默认 `Center`）与 CLR 包装 `PanelAlignment`（:67）、`StyleKeyOverride => typeof(UrsaWindow)`（:62，继承窗口 chrome 主题）、构造函数（:27，`Styles.Add(_theme)` + ContentControl 布局宿主 + **内置菜单栏**：`LeftContent = new Menu { Classes=chrome-menu, ItemsSource 宽松绑定 "MenuBarItems" }`（:35-40）与 `DataTemplates.Add(FuncDataTemplate<MenuItemViewModel>)` 注册项模板（:41，子菜单任意深度经模板查找递归复用）+ `UpdateLayoutTemplate`）、私有 `BuildMenuItemHeader`（:48，图标 null 不创建 PathIcon 不留占位间隙）、`OnPropertyChanged`（:73，监听 PanelAlignmentProperty）、私有 `UpdateLayoutTemplate`（:82，枚举→资源键映射 + `TryGetResource` 查找，缺失抛异常）。

### `Layout/SetPanelAlignmentEvent.cs`
`public class SetPanelAlignmentEvent : PubSubEvent<PanelAlignment>`（第 8 行）：请求切换布局档位的事件契约。注释自述放本模块而非 Core/Models 的原因（负载类型定义于此，Models 引用 Framework 会成环）。

### `Windows/FrameworkWindowTheme.axaml`
无 x:Class 的 Styles 根（570 行）。`Styles.Resources`：`NavigationItemTemplate`（:8）、六个共享部件 DataTemplate（`ShellActivityBar` :23 / `ShellSideBar` :39 / `ShellMainContent` :55 / `ShellAuxiliaryPanel` :67 / `ShellBottomPanel` :116 / `ShellStatusBar` :166）、四份布局 DataTemplate（`WindowLayoutLeft` :195 / `WindowLayoutRight` :261 / `WindowLayoutCenter` :327 / `WindowLayoutJustify` :392，差异为 BottomPanel 及分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`，ActivityBar 恒 `RowSpan=2` 通高）。样式（:458 起）自 Modules/Workstation/MainWindow.axaml 迁入；末尾 4 个标题栏菜单样式（:551-569）：顶层 MenuItem 紧凑行高、Popup#PART_Popup VerticalOffset=-8 贴合标题栏下缘、`Menu.chrome-menu MenuItem` 的 ItemsSource/Command/AutomationProperties.Name 样式绑定、PathIcon 前景色。

### `Windows/FrameworkWindowTheme.cs`
`public class FrameworkWindowTheme : Styles`（第 12 行）：`StyleInclude`（BaseUri `avares://DigitalWorkstation.Core.Framework/Windows/`，:14；Source 相对 `FrameworkWindowTheme.axaml`，:20）加载主题，构造时 `_ = include.Loaded` 强制加载（:22）再 `Add(include)`（:23）。

### `Contributions/ShellContributionCollector.cs`
`public class ShellContributionCollector(IContainerProvider containerProvider)`（第 9 行，主构造；命名空间 `DigitalWorkstation.Core.Framework.Contributions`）。四个收集方法：`GetToolViews()`（:15，`Order` 升序，不按定位枚举过滤——三处 Bar 的分派由消费方按 `Placement`/`AllowMove` 决定，ADR-0002）、`GetMainViews()`（:24）、`GetMenuItems()`（:31，无参数——不过滤不排序，建树由 `MenuTreeBuilder` 负责，ADR-0001）、`GetStatusBarItems()`（:38）。统一模式：容器解析 `IEnumerable<T>` → （可选）`Order` 升序 → `ToArray()`。

### `Contributions/ToolViewRegistration.cs`
`public static class ToolViewRegistration`（第 14 行）：`RegisterToolViews(this IContainerRegistry, Assembly)` 扩展（:20，ADR-0002）——扫描传入程序集中标注 `ToolViewAttribute` 的类（不做全局扫描），非可实例化 `Control` 或程序集内 `Id` 重复记 `Logger.Warning` 跳过；合法者 `Register(viewType)` 注册 View 类型本身，并把 attribute 元数据（标题经 `Language.Get` 解析）落成 `ToolViewContribution` 注册 singleton。与 `Menus/MenuRegistration.cs` 同构。

### `Menus/MenuItemViewModel.cs`
`public class MenuItemViewModel`（第 13 行），自 Modules/Workstation 迁入（命名空间 `DigitalWorkstation.Core.Framework.Menus`，API 不变）。菜单项呈现模型：`Title`（required init，:19）、`Icon: Geometry?`（:24，null 不渲染图标）、`Command: ICommand?`（:26）、`Children: ObservableCollection<object>`（:28，子项为 MenuItemViewModel 或 Avalonia `Separator`）；静态 `FromSubmenu(MenuTreeSubmenu)`（:33）把建树器产物递归转换（`IconPath` 经 `StreamGeometry.Parse` 转几何，:43；分隔线转 `Separator`，:47）。

### `Menus/MenuTreeEntry.cs`
菜单树的条目 record 族（ADR-0001）：`abstract record MenuTreeEntry`（第 10 行）；`MenuTreeItem(Title, IconPath?, Command)`（:15，叶子，标题已解析）；`MenuTreeSubmenu(Title, Children)`（:21，子菜单节点含顶层菜单，Children 分隔线已插好）；`MenuTreeSeparator`（:26，组间分隔线标记，私有构造单例 `Instance`）。

### `Menus/MenuTreeBuilder.cs`
`public static class MenuTreeBuilder`（第 13 行）：纯函数建树器。`Build(IEnumerable<IMenuItemContribution>) → IReadOnlyList<MenuTreeSubmenu>`（:18）：路径含空段记日志跳过（:24-29）；单段路径 Order 声明顶层位次、Group/GroupOrder 描述条目分组（:41-47）；多段路径三者声明末端子菜单节点位次、条目进默认组（:48-54）；顶层按 `(NodeOrder, Language.Get(段) Ordinal)` 排序不分组（:57-61）；子菜单内按 `(GroupOrder, null 组优先, Group Ordinal, Order, Title Ordinal)` 排序、组间插分隔线（:77-96）。内部累积结构 `LeafAccum` record（:101）与 `NodeAccum` class（:104，`MergeNodeOrder`/`MergePlacement` 位次冲突取最小）。

### `Menus/MenuRegistration.cs`
`public static class MenuRegistration`（第 14 行）：`RegisterMenus(this IContainerRegistry, Assembly)` 扩展（:20）——扫描传入程序集中标注 `MenuGroupAttribute` 的类（不做全局扫描），类路径含空段或方法签名非法（带参/返回值非 void/Task）记 `Logger.Warning` 跳过，菜单类 `RegisterSingleton(Type)`，每个合法 `MenuItemAttribute` 方法注册一个 `IMenuItemContribution` 工厂。同文件 `internal sealed class ReflectedMenuItemContribution`（:75）：构造期把 attribute 元数据落成契约属性（`Title` 经 `Language.Get` 解析），点击反射调用、`Task` 等待、异常记日志不抛出。

### `WindowManager/FrameworkWindowManager.cs`
`public class FrameworkWindowManager : IWindowManager, IMainWindowManager`（第 12 行，命名空间 `DigitalWorkstation.Core.Framework.WindowManager`）。内部状态 `_windowMap: Dictionary<Type, Window>`（:17）与 `_mainWindow`（:22）。入口：`GetWindow`（:35）、`InitializeWindow`（:45，私有）、`ShowWindow` 四重载（:57-111）、`ShowDialog` 四重载（:113-141）、`CloseWindow`（:144）、`HideWindow`（:150）、`HandleMainWindow`（:158）、`HideMainWindow`/`ShowMainWindow`（:169-170）、`CloseWindowsExceptMain`（:172）。

## 对应测试文件（UnitTest/Framework/）

```
UnitTest/Framework/
├── Framework.csproj                  xUnit 测试项目：Microsoft.NET.Test.Sdk 17.6.0 + xunit 2.4.2 + xunit.runner.visualstudio 2.4.5
└── ShellLayoutStateResizeTests.cs    11 个 [Fact]：Resize 增减/clamp/区域隔离/收起恢复保留尺寸
```
