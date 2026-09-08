# Framework — 对外接口与调用方式

命名空间六组：`DigitalWorkstation.Core.Framework`（根）、`.Framework.Layout`（布局状态机与面板分隔条）、`.Framework.Menus`（菜单建树/注册/呈现模型）、`.Framework.Contributions`（贡献收集器）、`.Framework.Windows`（窗口基类与主题）、`.Framework.WindowManager`。类型均 public，例外：`Menus/MenuRegistration.cs` 内由 attribute 扫描生成的 `ReflectedMenuItemContribution` 为 internal（见第 11 节）。

## 1. `FrameworkApplication<TWindow>`（FrameworkApplication.cs:16）

```csharp
public abstract class FrameworkApplication<TWindow> : PrismApplication where TWindow : Window
```

应用入口基类。子类（真实代码：`Modules/Workstation/WorkstationApplication.cs:12` 的 `WorkstationApplication : FrameworkApplication<MainWindow>`）必须做的事：泛型参数指定主窗口类型、重写 `CreateSplashWindow()`、可选重写 `RegisterCustomService()`、按 Prism 惯例重写 `ConfigureModuleCatalog`。

### 公开/保护成员

| 成员 | 签名 | 说明 |
|---|---|---|
| `Initialize` | `public override void Initialize()` | 固定 `RequestedThemeVariant = ThemeVariant.Dark`（第 24 行），`Styles.AddRange(new WorkstationTheme())`（第 25 行），`VSCodePalette.ApplyTo(Resources)`（第 27 行），最后调 `base.Initialize()` |
| `OnFrameworkInitializationCompleted` | `public override void OnFrameworkInitializationCompleted()` | 不调用 base；fire-and-forget 启动 `RunStartupSequenceAsync()`（第 37 行） |
| `OnInitialized` | `protected override void OnInitialized()` | 空方法（第 44-46 行），故意阻止 base 提前显示 MainWindow |
| `InitializeModules` | `protected override void InitializeModules()` | 空方法（第 51-53 行），抑制 Prism 同步一次性模块加载 |
| `CreateSplashWindow` | `protected abstract Window CreateSplashWindow()` | 子类提供启动台窗口；模块加载进度与失败决策均经启动台呈现 |
| `RegisterTypes` | `protected override void RegisterTypes(IContainerRegistry)` | **密封式编排**（注释明确"子类不需要重写"）：先 `RegisterFrameworkServices` 后 `RegisterCustomService`（第 171-175 行） |
| `RegisterCustomService` | `protected virtual void RegisterCustomService(IContainerRegistry)` | 子类注册自定义服务的钩子，默认空实现（第 183 行） |
| `CreateShell` | `protected override AvaloniaObject CreateShell()` | `Container.Resolve<TWindow>()`（第 194 行）——主窗口经容器解析，支持构造注入 |
| `ConfigureViewModelLocator` | `protected override void ConfigureViewModelLocator()` | 约定式 ViewModel 定位（第 205-237 行），见下 |

### 私有启动序列成员（改行为时直接面对）

- `RunStartupSequenceAsync()`（第 64 行）：三阶段启动，见 common.md 状态流转。阶段 2 取 `moduleCatalog.Modules.ToList()` 快照后以 `for (var i = 0; i < total; i++)` 按下标推进（`total = modules.Count`）：每模块先 `Publish(new StartupProgress(StartupPhase.LoadingModules, module.ModuleName, i + 1, total))`（序号从 1 起），再 `await Task.Run(() => moduleManager.LoadModule(module.ModuleName))`（第 88 行）。
- `WaitForFailureActionAsync(IEventAggregator)`（第 117 行）：一次性订阅 `StartupFailureActionEvent`，返回 `true` = Continue。
- `ShowMainWindow()`（第 127 行）：先做两个模式匹配守卫——`MainWindow is not Window window` 或 `ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime` 时**静默 return**（不抛异常、不动作）；通过后依次 `lifetime.MainWindow = window` → `_windowManager?.ShowMainWindow()` → `_windowManager?.CloseWindowsExceptMain()`。
- `RegisterFrameworkServices`（第 142 行）/ `ResolveFrameworkServices`（第 157 行）：前者末尾调后者；后者用 `Container.Resolve<IEventAggregator>()` 与 `Container.Resolve<IMainWindowManager>()` 把两个服务存入 `_eventAggregator`/`_windowManager` 私有字段。RegisterTypes 阶段 `Container` 已可用（`RegisterFrameworkServices` 首行 `IoC.Initialize(containerRegistry, Container)` 即以它为参），且 `IMainWindowManager` 单例刚在本方法前段注册，故可立即解析。

### ViewModel 定位约定（ConfigureViewModelLocator，第 209-233 行）

```
viewName.Replace("Views", "ViewModels")
后缀 Window 或 Page → 追加 "ViewModel"
后缀 View          → 追加 "Model"（即 *View → *ViewModel）
```

即 `**/Views/*View.axaml → **/ViewModels/*ViewModel.cs`；`**/Views/Windows/*Window.axaml → **/ViewModels/Windows/*WindowViewModel.cs`；`**/Views/Pages/*Page.axaml → **/ViewModels/Pages/*PageViewModel.cs`。映射后的全名再拼上程序集全名组成程序集限定名 `$"{viewModelName}, {viewAssemblyName}"`（第 230 行），其中 `viewAssemblyName` 来自 `viewType.GetTypeInfo().Assembly.FullName`（第 212 行，即 **View 所在程序集**的全名——ViewModel 必须与 View 同程序集才能解析到）；最后调 `Type.GetType(fullViewModelName)`（第 232 行）解析出 ViewModel 的 `Type` 并返回。解析不到（`viewName`/`viewAssemblyName` 为空或类型不存在）时返回 null（不抛异常）。View 中需 `mvvm:ViewModelLocator.AutoWireViewModel="True"` 启用。

## 2. `FrameworkWindowManager`（WindowManager/FrameworkWindowManager.cs:12）

```csharp
public class FrameworkWindowManager : IWindowManager, IMainWindowManager
```

实现 Abstractions 的两个接口（方法契约见 `docs/analysis/Core/Abstractions/api.md`），本模块提供行为语义：

| 方法 | 行为要点 |
|---|---|
| `GetWindow(Type)` | `IoC.Provider.Resolve(type) as Window`，解析结果非 Window 抛 `ArgumentException("TargetWindow type is illegal")`（第 37 行） |
| `ShowWindow(Type)` / `ShowWindow(Type, object)` | `GetWindow` 后转实例版 |
| `ShowWindow(Window)` / `ShowWindow(Window, object)` | 顺序：`InitializeWindow` 注册 →（dataContext 版）`window.DataContext = dataContext` 赋值 → 检查主窗口：已设且 `IsActive` 时 `window.Show(_mainWindow)`（以主窗口为 owner），否则 `window.Show()`；主窗口未设抛 `InvalidOperationException`（第 78-111 行）。注意抛异常时窗口已注册进 `_windowMap` 且 DataContext 已赋值，无回滚 |
| `ShowDialog(...)` 四个重载 | 与 ShowWindow 类似，但**要求主窗口已设且 IsActive**，否则抛 `InvalidOperationException`（第 124-141 行） |
| `CloseWindow(Type)` | `_windowMap` 命中则 `Close()`；未命中静默不操作（第 144-148 行） |
| `HandleMainWindow()` | 从 `Application.Current as PrismApplication` 取 `MainWindow` 登记为 `_mainWindow` 并加入 `_windowMap`；取不到或已在映射中抛 `InvalidOperationException`（第 158-167 行）。**唯一调用点**：启动序列阶段 1（`FrameworkApplication.cs:72`），先于启动台显示 |
| `HideMainWindow()` / `ShowMainWindow()` | `_mainWindow?.Hide()/Show()`，空调用安全（第 169-170 行） |
| `CloseWindowsExceptMain()` | 先 `_windowMap.Values.Where(w => w != _mainWindow).ToList()` 物化快照（排除主窗口），再逐个 `Close()`（第 172-177 行）。必须 ToList：每个窗口 `Close()` 触发 `InitializeWindow` 挂的 `Closing` 处理器把类型从 `_windowMap` 移除，直接枚举 `Values` 会在迭代中修改集合 |

内部状态：`private readonly Dictionary<Type, Window> _windowMap`（第 17 行）、`private Window? _mainWindow`（第 22 行）。`InitializeWindow`（第 45 行）注册时挂 `Closing` 事件把类型从映射移除——窗口关闭后同类型可再次 ShowWindow。

**调用方式**：消费方构造注入 `IWindowManager` 或 `IMainWindowManager`（同一单例）。真实调用点：`Modules/DashBoard/DashBoardMenus.cs:20` `windowManager.ShowWindow<DashBoardWindow>`（经 Abstractions 的泛型扩展转发到 `ShowWindow(Type)`）、`Modules/Workstation/Menus/HelpMenus.cs:20` `windowManager.ShowDialog<AboutWindow>`；框架内部 `FrameworkApplication.cs:73` `Container.Resolve<IWindowManager>().ShowWindow(CreateSplashWindow())`。

## 3. `ShellLayoutState` 及区域 record（Layout/）

```csharp
public sealed record ShellLayoutState
{
    public string? SelectedActivity { get; init; }
    public SideBarState SideBar { get; init; } = new();
    public AuxiliaryPanelState AuxiliaryPanel { get; init; } = new();
    public BottomPanelState BottomPanel { get; init; } = new();
    public MainContentState MainContent { get; init; } = new();
    public static ShellLayoutState Initial => new();
}
```

### 转换方法（全部纯函数，返回新实例）

| 方法 | 语义 |
|---|---|
| `SelectActivity(string id)`（ShellLayoutState.cs:28） | 已选中该 id 且 SideBar 可见 → 收起 SideBar（`SelectedActivity` 保留）；否则选中该 id、展开 SideBar 并置 `ContentFor = id`。不改 MainContent |
| `ToggleSideBar()`（:45） | 独立翻转 `SideBar.Visible`，不影响选中项与内容 |
| `ToggleAuxiliaryPanel()`（:53） | 独立翻转 `AuxiliaryPanel.Visible`，不影响活动 tab |
| `ToggleBottomPanel()`（:61） | 独立翻转 `BottomPanel.Visible`，不影响活动 tab |
| `ActivateAuxTab(string tab)`（:69） | AuxiliaryPanel 收起时**拒绝，返回 `this`**；面板可见时以 with 表达式更新 `AuxiliaryPanel.ActiveTab` 返回新实例 |
| `ActivateBottomTab(string tab)`（:82） | BottomPanel 收起时拒绝返回 `this`；面板可见时以 with 表达式更新 `BottomPanel.ActiveTab` 返回新实例 |
| `OpenMainView(string view)`（:95） | 仅改 `MainContent.ActiveView` |
| `Resize(PanelResizeTarget target, double delta)`（:103） | SideBar/AuxiliaryPanel 调 `Width`、BottomPanel 调 `Height`，经私有静态 `Clamp(double value, double min, double max)`（:134，实现为 `Math.Max(min, Math.Min(max, value))`）钳到各 record 的 Min/Max 常量；switch 兜底分支 `_ => this`，未知 target 返回等值状态 |

注意：**面板对齐（PanelAlignment）不在本状态机内**——它是 `FrameworkWindow` 的 StyledProperty（见第 4 节），由窗口依赖属性持有并双向绑定到 ViewModel，布局状态机只管五区域的可见性/尺寸/tab。

### 区域 record 字段与默认值

| 类型（文件） | 字段（默认） |
|---|---|
| `SideBarState`（Layout/SideBarState.cs:6） | `MinWidth=120`、`MaxWidth=480`（const）；`Visible=false`（bool 默认）、`Width=240`、`ContentFor=null`（收起时保留导航项 Id） |
| `AuxiliaryPanelState`（Layout/AuxiliaryPanelState.cs:6） | `MinWidth=120`、`MaxWidth=480`（const）；`Visible=true`、`Width=280`、`Tabs=[]`、`ActiveTab=null` |
| `BottomPanelState`（Layout/BottomPanelState.cs:6） | `MinHeight=80`、`MaxHeight=480`（const）；`Visible=true`、`Height=160`、`Tabs=[]`、`ActiveTab=null` |
| `MainContentState`（Layout/MainContentState.cs:6） | `ActiveView=null` |
| `PanelResizeTarget`（Layout/PanelResizeTarget.cs:6） | 枚举：`SideBar` / `AuxiliaryPanel` / `BottomPanel` |

**典型消费**（真实代码）：`Modules/Workstation/MainWindowViewModel.cs:41` `[ObservableProperty] private ShellLayoutState _state = ShellLayoutState.Initial;`，各 RelayCommand 调转换方法后整体替换 `_state`；本模块 `Layout/PanelResizer.cs` 经 `ResizeCommand` 以 `PanelResize` 参数把 GridSplitter 拖拽增量交给 `Resize`（见第 6 节）。

## 4. `FrameworkWindow`（Windows/FrameworkWindow.cs:19）

```csharp
public abstract class FrameworkWindow : UrsaWindow
```

带基础布局的窗口基类：内置 VS Code 式五区 shell（ActivityBar/SideBar/MainContent/AuxiliaryPanel/BottomPanel + 状态栏），**以及标题栏左侧的菜单栏**（全部菜单贡献建树生成，ADR-0001；宽松绑定 ViewModel 的 `MenuBarItems`）。真实子类：`Modules/Workstation/MainWindow`（`MainWindow.axaml.cs:5`，菜单栏已由基类内置，axaml 侧只保留应用级 chrome——标题、快捷键）。成员：

| 成员 | 签名 | 说明 |
|---|---|---|
| `PanelAlignmentProperty` | `public static readonly StyledProperty<PanelAlignment>`（:21） | 布局档位的依赖属性，**默认 `PanelAlignment.Center`**（= 历史布局）；是布局定义的唯一入口，与 ViewModel 双向绑定 |
| `PanelAlignment` | `public PanelAlignment PanelAlignment`（:67） | CLR 包装；写值触发 `OnPropertyChanged` → `UpdateLayoutTemplate`，**整体替换** `ContentTemplate`，不做动态调整 |
| `StyleKeyOverride` | `protected override Type StyleKeyOverride => typeof(UrsaWindow)`（:62） | 继承 UrsaWindow 的窗口主题（标题栏 chrome、模板与焦点行为） |
| 构造函数 | `protected FrameworkWindow()`（:27） | 依次：`Styles.Add(_theme)`（`_theme` 为 `FrameworkWindowTheme` 实例字段，:24）→ `_layoutHost`（ContentControl 布局宿主，:25）的 `Content` 经 `this[!DataContextProperty]` 绑定窗口 DataContext（:31，布局模板以 ViewModel 为绑定源，全部宽松绑定）→ `Content = _layoutHost`（:32）→ **内置菜单栏**：`LeftContent = new Menu { Classes = { "chrome-menu" }, VerticalAlignment.Center, ItemsSource 宽松绑定 "MenuBarItems" }`（:35-40）→ `DataTemplates.Add(new FuncDataTemplate<MenuItemViewModel>(...))`（:41，项模板入窗口 DataTemplates 而非主题资源，子菜单任意深度经模板查找递归复用）→ `UpdateLayoutTemplate()`（:42） |
| `BuildMenuItemHeader` | `private static Control`（:48） | 菜单项头部：水平 StackPanel（Spacing=6）+ 图标（`Icon` 非 null 才创建 14×14 `PathIcon`，**不留占位间隙**）+ 标题 `TextBlock`；图标前景色由 chrome-menu 样式接管 |
| `OnPropertyChanged` | `protected override void`（:73） | `e.Property == PanelAlignmentProperty` 时调 `UpdateLayoutTemplate()` |
| `UpdateLayoutTemplate` | `private void`（:82） | 枚举→资源键映射后 `_theme.TryGetResource(key, null, out var template)` 查找并强转 `IDataTemplate` 赋给 `_layoutHost.ContentTemplate`；**缺失抛 `InvalidOperationException($"布局模板资源缺失：{key}")`**（:94），窗口构造期即失败 |

枚举→资源键映射（:84-90）：

| PanelAlignment | 资源键 | BottomPanel 跨度（模板内 `Grid.Column`/`ColumnSpan`） |
|---|---|---|
| `Left` | `WindowLayoutLeft` | 列 1 跨 2（SideBar + MainContent 列下方）；AuxiliaryPanel 及其分隔条 `RowSpan=2` 通高到底 |
| `Right` | `WindowLayoutRight` | 列 2 跨 2（MainContent + AuxiliaryPanel 列下方）；SideBar 及其分隔条 `RowSpan=2` 通高到底 |
| `Center`（默认，switch 兜底） | `WindowLayoutCenter` | 列 2 跨 1（仅 MainContent 列下方）；SideBar 与 AuxiliaryPanel 及各自分隔条 `RowSpan=2` 通高到底 |
| `Justify` | `WindowLayoutJustify` | 列 1 跨 3（三列全宽）；侧栏只占第 0 行 |

## 5. `FrameworkWindowTheme`（Windows/FrameworkWindowTheme.cs:12）

```csharp
public class FrameworkWindowTheme : Styles
```

FrameworkWindow 的基础布局主题。加载机制：构造函数（:16-24）创建 `StyleInclude`（BaseUri `avares://DigitalWorkstation.Core.Framework/Windows/`，:14；Source 相对 `FrameworkWindowTheme.axaml`，:20），先 `_ = include.Loaded` **强制加载**（:22，保证窗口构造期即可查到布局模板资源）再 `Add(include)`。与 Semi/Ursa 主题同款机制；不用 x:Class code-behind 的原因见 common.md「核心设计逻辑」。

资源清单（`Windows/FrameworkWindowTheme.axaml`，全部在 `Styles.Resources` 内）：

| 资源键 | 位置 | 内容 |
|---|---|---|
| `NavigationItemTemplate` | :8 | ActivityBar 导航项按钮 |
| `ShellActivityBar` / `ShellSideBar` / `ShellMainContent` / `ShellAuxiliaryPanel` / `ShellBottomPanel` / `ShellStatusBar` | :23 / :39 / :55 / :67 / :116 / :166 | 六个共享部件 DataTemplate |
| `WindowLayoutLeft` / `WindowLayoutRight` / `WindowLayoutCenter` / `WindowLayoutJustify` | :195 / :261 / :327 / :392 | 四份布局 DataTemplate：布局 Grid 列 `Auto,{Binding SideBarColumnWidth},*,{Binding AuxiliaryColumnWidth}`、行 `*,Auto`；差异为 BottomPanel 及分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`（见上表）；ActivityBar 恒 `RowSpan=2` 通高 |

样式（:458 起）：nav-item/panel-tab/GridSplitter/panel-collapse/region-title/placeholder/status-item 自 Modules/Workstation/MainWindow.axaml 迁入；末尾新增 4 个标题栏菜单样式（:551-569，ADR-0001 菜单栏内置配套）：`Menu.chrome-menu > MenuItem` 紧凑行高（MinHeight=30、Padding=10,0，:552-555）、`Popup#PART_Popup` VerticalOffset=-8 让弹出层贴合标题栏下缘（:557-559）、`Menu.chrome-menu MenuItem` 的 `ItemsSource={Binding Children}`/`Command={Binding Command}`/`AutomationProperties.Name={Binding Title}` 样式绑定（:562-566，叶子 Children 为空、节点 Command 为 null，均无副作用）、`PathIcon` 前景色 SemiColorText1（:567-569）。**菜单项的内容模板不在本主题内**——无 x:Key 的 DataTemplate 不能放 `Styles.Resources`（AVLN3000），故由 `FrameworkWindow` 构造时在窗口 `DataTemplates` 代码注册（见第 4 节）。分隔条改用 `layout:PanelResizer` 的 `Target`+`ResizeCommand` 声明式绑定（如 :208-219；axaml 以 `xmlns:layout="clr-namespace:DigitalWorkstation.Core.Framework.Layout"` 引入，:3）。**所有绑定为宽松反射绑定**——Framework 不引用具体 ViewModel 类型。面板对齐的切换入口不在本主题内（在视图菜单，见 Modules/Workstation 的 `Menus/ViewAlignmentMenus.cs`）。

## 6. `PanelAlignment` / `PanelResize` / `SetPanelAlignmentEvent` / `PanelResizer`（均位于 Layout/）

```csharp
public enum PanelAlignment { Left, Right, Center, Justify }                       // PanelAlignment.cs:7
public readonly record struct PanelResize(PanelResizeTarget Target, double Delta); // PanelResize.cs:6
public class PanelResizer : GridSplitter                                          // PanelResizer.cs:13
public class SetPanelAlignmentEvent : PubSubEvent<PanelAlignment>                 // SetPanelAlignmentEvent.cs:8
```

- `PanelAlignment`：FrameworkWindow 基础布局的档位，决定 BottomPanel 在窗口底部的水平跨度（领域定义见根目录 CONTEXT.md「面板对齐」）。
- `SetPanelAlignmentEvent`：请求切换布局档位的事件契约（负载 `PanelAlignment`）。发布方：视图菜单对齐项（Workstation 的 `Menus/ViewAlignmentMenus.cs`）；订阅方：主窗口 ViewModel，写入 `PanelAlignment` 依赖属性。契约放本模块而非 Core/Models——负载类型定义于此，Models 引用 Framework 会成环（文件注释自述）。
- `PanelResize`：分隔条命令参数，`Delta` 为**已换算方向**的尺寸增量；消费方直接转交 `ShellLayoutState.Resize(Target, Delta)`。
- `PanelResizer`（自 Modules/Workstation 迁入并改造）：复用 GridSplitter 的拖拽手势与方向光标，但禁用其原生列重排。成员契约：

| 成员 | 说明 |
|---|---|
| `Target`（:31，CLR 属性，`PanelResizeTarget`） | 拖拽调整的目标区域：决定尺寸增量取哪个轴、是否取反 |
| `ResizeCommandProperty`（:15）/ `ResizeCommand`（:36，`StyledProperty<ICommand?>`） | 拖拽增量的出口：ViewModel 的 ResizePanelCommand |
| `StyleKeyOverride => typeof(GridSplitter)`（:26） | ControlTheme 按 StyleKey 精确查找：继承 GridSplitter 的主题（模板/尺寸/焦点行为） |
| `GetParentGrid() => null`（:46） | 使原生 resize 初始化短路：ResizeData 为空，GridSplitter 的所有原生重排路径自动跳过，只剩 Thumb 的 DragDelta 事件 |
| 构造函数挂 `DragDelta += OnDragDelta`（:20）；`OnDragDelta`（:54） | 方向换算：`SideBar => +e.Vector.X`、`AuxiliaryPanel => -e.Vector.X`、其他（BottomPanel）`=> -e.Vector.Y`，随后 `ResizeCommand?.Execute(new PanelResize(Target, delta))`（:62） |

**调用方式**：布局模板内声明式使用——`<layout:PanelResizer Target="SideBar" ResizeCommand="{Binding ResizePanelCommand}" .../>`（真实用例 `Windows/FrameworkWindowTheme.axaml:208-219` 等，每份布局模板三枚：SideBar/AuxiliaryPanel/BottomPanel）。ViewModel 侧契约：提供接受 `PanelResize` 参数的 `ResizePanelCommand`（真实实现 `Modules/Workstation/MainWindowViewModel.cs` 转调 `ShellLayoutState.Resize`）。

## 7. `ShellContributionCollector`（Contributions/ShellContributionCollector.cs:9）

```csharp
public class ShellContributionCollector(IContainerProvider containerProvider)
```

主构造注入 Prism `IContainerProvider`。注册为单例（`FrameworkApplication.cs:152`）。五个收集方法；除菜单外统一模式：`Resolve<IEnumerable<TContribution>>()` → 按定位枚举 `Where` 过滤 → `OrderBy(Order)` → `ToArray()`；菜单方法自 ADR-0001 起不过滤不排序，建树器负责分组排序：

| 方法 | 过滤 | 排序 |
|---|---|---|
| `GetNavigationItems(NavigationItemPlacement)` | `item.Placement == placement` | `Order` 升序 |
| `GetMainViews()` | 无（全部） | 无（保持容器解析顺序） |
| `GetPanelTabs(PanelPlacement)` | `tab.Panel == panel` | `Order` 升序 |
| `GetMenuItems()`（:40，**无参数**） | 无（路径/分组模型下不再按定位枚举过滤） | 无（分组排序建树由 `MenuTreeBuilder` 负责，见第 10 节） |
| `GetStatusBarItems()` | 无 | `Order` 升序 |

返回类型均为 `IReadOnlyList<T>`（快照数组）。贡献接口中 `INavigationItemContribution`、`IMainViewContribution`、`IPanelTabContribution`、`IStatusBarItemContribution` 与定位枚举（`NavigationItemPlacement`/`PanelPlacement`，随前两者同文件定义）在 Core/Abstractions 的 `Contributions/` 目录；`IMenuItemContribution` 在 `Menus/` 目录，形状已按 ADR-0001 改为路径/分组模型（见 Abstractions 文档）。

**典型消费**：`Modules/Workstation/MainWindowViewModel.cs:28` 构造注入 `ShellContributionCollector`，初始化时调各 `Get*` 方法构建导航/面板/状态栏 ViewModel；菜单走 `MenuTreeBuilder.Build(_collector.GetMenuItems())`（:135）建树后转为菜单 ViewModel。

## 8. `MenuItemViewModel`（Menus/MenuItemViewModel.cs:13）

```csharp
public class MenuItemViewModel
```

菜单项的呈现模型（自 Modules/Workstation 迁入，命名空间 `DigitalWorkstation.Core.Framework.Menus`，API 不变）：叶子（`Command` 非空）或子菜单节点（`Children` 非空），由菜单树（`MenuTreeSubmenu`）递归转换而来；分隔线直接是 Avalonia `Separator` 控件。成员：

| 成员 | 签名 | 说明 |
|---|---|---|
| `Title` | `public required string Title { get; init; }`（:19） | 已解析的显示标题 |
| `Icon` | `public Geometry? Icon { get; init; }`（:24） | 图标几何（随主题变色）；null = 无图标，模板不渲染 `PathIcon` |
| `Command` | `public ICommand? Command { get; init; }`（:26） | 叶子命令；节点为 null |
| `Children` | `public ObservableCollection<object> Children { get; }`（:28） | 子项（`MenuItemViewModel` 或 `Separator`） |
| `FromSubmenu` | `public static MenuItemViewModel FromSubmenu(MenuTreeSubmenu)`（:33） | 递归转换：`MenuTreeItem` → 叶子（`IconPath` 经 `StreamGeometry.Parse` 转几何，:43）、`MenuTreeSubmenu` → 递归、分隔线 → `new Separator()`（:38-48） |

**消费约定**：shell 宿主窗口的 ViewModel 暴露 `MenuBarItems` 顶层集合（宽松绑定约定），`FrameworkWindow` 内置的 `Menu` 直接绑定它（见第 4 节）；真实实现 `Modules/Workstation/MainWindowViewModel.cs:71`。

## 9. 菜单树数据结构 `MenuTreeEntry` 族（Menus/MenuTreeEntry.cs:10）

```csharp
public abstract record MenuTreeEntry;                                                  // :10
public sealed record MenuTreeItem(string Title, string? IconPath, ICommand Command)    // :15
    : MenuTreeEntry;
public sealed record MenuTreeSubmenu(string Title, IReadOnlyList<MenuTreeEntry> Children) // :21
    : MenuTreeEntry;
public sealed record MenuTreeSeparator : MenuTreeEntry                                 // :26，单例
{
    public static readonly MenuTreeSeparator Instance = new();                         // :28
}
```

建树产物：叶子菜单项（`MenuTreeItem`，标题已按当前 UI 区域性解析）、子菜单节点（`MenuTreeSubmenu`，含顶层菜单；`Children` 中分隔线已按分组规则插好，不存在开头/结尾/连续分隔线）、组间分隔线标记（`MenuTreeSeparator`，私有构造的单例）。由 `MenuTreeBuilder` 生成，本模块的 `MenuItemViewModel.FromSubmenu`（见第 8 节）再转换为 Avalonia 控件（分隔线转 `Separator`）。

## 10. `MenuTreeBuilder`（Menus/MenuTreeBuilder.cs:13）

```csharp
public static class MenuTreeBuilder
{
    public static IReadOnlyList<MenuTreeSubmenu> Build(IEnumerable<IMenuItemContribution> contributions); // :18
}
```

纯函数、无状态建树器：把扁平贡献列表构建为分组排序好的顶层菜单列表，标题（路径段与条目）在建树时经 `Language.Get` 一次性解析（:59、:74、:98）。规则（ADR-0001）：

- **路径切分与跳过**：`Path.Split('/', TrimEntries)`（:23），含空段记 `Logger.Warning` 跳过该条目（:24-29）。
- **单段路径**（顶层菜单直下条目）：`Order` 经 `NodeAccum.MergeNodeOrder` 声明顶层菜单位次（多处声明取最小，:126-135）；`Group`/`GroupOrder` 描述条目分组（:41-47）。
- **多段路径**：`Group`/`GroupOrder`/`NodeOrder` 经 `MergePlacement` 声明末端子菜单节点的位次（多处声明：`NodeOrder`/`GroupOrder` 取最小，`Group` 取 `GroupOrder` 最小声明的组名、同值取组名 Ordinal 小者，:141-159）；条目进末端菜单默认组（:48-54）。
- **顶层排序**：只按 `(NodeOrder, Language.Get(段) Ordinal)` 排序，不分组不插分隔线（:57-61）。
- **子菜单内排序与分组**（`Emit`，:64）：条目与递归子节点统一按 `(GroupOrder, null 组优先, Group Ordinal, Order, Title Ordinal)` 排序（:77-83）；相邻条目组变化处插 `MenuTreeSeparator.Instance`（:87-96），天然不产生开头/结尾/连续分隔线。

内部累积结构（建树期临时对象，不外泄）：`LeafAccum` record（:101，一条叶子的分组/位次/标题/图标/命令）与 `NodeAccum` class（:104，路径段节点：子节点字典 `Children`（Ordinal 键）、条目列表 `Items`、合并位次的 `MergeNodeOrder`/`MergePlacement`）。

## 11. `MenuRegistration.RegisterMenus`（Menus/MenuRegistration.cs:14）

```csharp
public static void RegisterMenus(this IContainerRegistry registry, Assembly assembly); // :20
```

attribute 菜单注册扩展（ADR-0001）。模块在自身 `RegisterTypes` 中调用并传入本模块程序集，**不做全局程序集扫描**；扫描只在注册时发生一次。规则（:21-68）：

1. 遍历 `assembly.DefinedTypes`，取标注 `MenuGroupAttribute` 的类（:22-28）；类路径含空段记 `Logger.Warning` 整类跳过（:30-35）。
2. 取该类 `Public | Instance | DeclaredOnly` 方法中标注 `MenuItemAttribute` 者（:37-40）；带参或返回值非 `void`/`Task` 的记 `Logger.Warning` 跳过（:44-51）——因此**静态方法与泛型方法**（非实例/含参）天然进不了候选或被签名校验挡下。
3. 类内无合法方法则整体跳过（:55-58）；否则菜单类本体 `RegisterSingleton(menuType)`（:61），每个合法方法注册一个 `IMenuItemContribution` 工厂（:62-66），工厂内 `provider.Resolve(menuType)` 取菜单类 singleton 实例（建树时经容器解析一次，之后复用）。

### `ReflectedMenuItemContribution`（internal，:75）

由 `RegisterMenus` 生成的 `IMenuItemContribution` 实现。构造期（:80-93）把 attribute 元数据落成契约属性：`Title = Language.Get(item.Title)`（注册时即按 UI 区域性解析）、`IconPath = item.Icon`、`Path`/`Group`/`GroupOrder` 取自类级 `MenuGroupAttribute`、`NodeOrder = group.Order`、`Order = item.Order`、`Command = new DelegateCommand(Execute)`。点击时 `Execute` fire-and-forget 调 `ExecuteAsync`（:111-114）：反射 `_method.Invoke(_instance, null)`，返回 `Task` 则 `await`（:120-123）；异常解包 `TargetInvocationException` 后 `Logger.Error` 记录，**不抛出**（:125-132）。

## 容器注册清单（对外可解析的服务）

`RegisterFrameworkServices`（FrameworkApplication.cs:142-155）注册：

| 服务 | 注册方式 | 实现 |
|---|---|---|
| `IMainWindowManager` | Singleton | 同一 `FrameworkWindowManager` 实例 |
| `IWindowManager` | Singleton | 同一 `FrameworkWindowManager` 实例 |
| `ShellContributionCollector` | Singleton | 自身 |
| `IoC.Registry` / `IoC.Provider` | 静态初始化 | `IoC.Initialize(containerRegistry, Container)`（Common 模块） |
