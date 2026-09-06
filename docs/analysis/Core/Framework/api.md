# Framework — 对外接口与调用方式

命名空间三组：`DigitalWorkstation.Core.Framework`（根）、`DigitalWorkstation.Core.Framework.Shell`、`DigitalWorkstation.Core.Framework.WindowManager`。全部类型 public。

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

**调用方式**：消费方构造注入 `IWindowManager` 或 `IMainWindowManager`（同一单例）。真实调用点：`Modules/DashBoard/OpenDashBoardMenuItem.cs:18` `windowManager.ShowWindow<DashBoardWindow>`（经 Abstractions 的泛型扩展转发到 `ShowWindow(Type)`）、`Modules/Workstation/Shell/AboutMenuItem.cs:17` `windowManager.ShowDialog<AboutWindow>`；框架内部 `FrameworkApplication.cs:73` `Container.Resolve<IWindowManager>().ShowWindow(CreateSplashWindow())`。

## 3. `ShellLayoutState` 及区域 record（Shell/）

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

### 区域 record 字段与默认值

| 类型（文件） | 字段（默认） |
|---|---|
| `SideBarState`（Shell/SideBarState.cs:6） | `MinWidth=120`、`MaxWidth=480`（const）；`Visible=false`（bool 默认）、`Width=240`、`ContentFor=null`（收起时保留导航项 Id） |
| `AuxiliaryPanelState`（Shell/AuxiliaryPanelState.cs:6） | `MinWidth=120`、`MaxWidth=480`（const）；`Visible=true`、`Width=280`、`Tabs=[]`、`ActiveTab=null` |
| `BottomPanelState`（Shell/BottomPanelState.cs:6） | `MinHeight=80`、`MaxHeight=480`（const）；`Visible=true`、`Height=160`、`Tabs=[]`、`ActiveTab=null` |
| `MainContentState`（Shell/MainContentState.cs:6） | `ActiveView=null` |
| `PanelResizeTarget`（Shell/PanelResizeTarget.cs:6） | 枚举：`SideBar` / `AuxiliaryPanel` / `BottomPanel` |

**典型消费**（真实代码）：`Modules/Workstation/MainWindowViewModel.cs:38` `[ObservableProperty] private ShellLayoutState _state = ShellLayoutState.Initial;`，各 RelayCommand 调转换方法后整体替换 `_state`；`Modules/Workstation/PanelResizer.cs` 把 GridSplitter 拖拽增量交给 `Resize`。

## 4. `ShellContributionCollector`（Shell/ShellContributionCollector.cs:8）

```csharp
public class ShellContributionCollector(IContainerProvider containerProvider)
```

主构造注入 Prism `IContainerProvider`。注册为单例（`FrameworkApplication.cs:152`）。五个收集方法，统一模式：`Resolve<IEnumerable<TContribution>>()` → 按定位枚举 `Where` 过滤 → `OrderBy(Order)` → `ToArray()`：

| 方法 | 过滤 | 排序 |
|---|---|---|
| `GetNavigationItems(NavigationItemPlacement)` | `item.Placement == placement` | `Order` 升序 |
| `GetMainViews()` | 无（全部） | 无（保持容器解析顺序） |
| `GetPanelTabs(PanelPlacement)` | `tab.Panel == panel` | `Order` 升序 |
| `GetMenuItems(MenuPlacement)` | `item.Menu == menu` | `Order` 升序 |
| `GetStatusBarItems()` | 无 | `Order` 升序 |

返回类型均为 `IReadOnlyList<T>`（快照数组）。贡献接口（`INavigationItemContribution`、`IMainViewContribution`、`IPanelTabContribution`、`IMenuItemContribution`、`IStatusBarItemContribution`）与定位枚举（`NavigationItemPlacement`/`PanelPlacement`/`MenuPlacement`）定义在 Core/Abstractions 的 `Shell/` 目录。

**典型消费**：`Modules/Workstation/MainWindowViewModel.cs:27` 构造注入 `ShellContributionCollector`，初始化时调各 `Get*` 方法构建导航/面板/菜单/状态栏 ViewModel。

## 容器注册清单（对外可解析的服务）

`RegisterFrameworkServices`（FrameworkApplication.cs:142-155）注册：

| 服务 | 注册方式 | 实现 |
|---|---|---|
| `IMainWindowManager` | Singleton | 同一 `FrameworkWindowManager` 实例 |
| `IWindowManager` | Singleton | 同一 `FrameworkWindowManager` 实例 |
| `ShellContributionCollector` | Singleton | 自身 |
| `IoC.Registry` / `IoC.Provider` | 静态初始化 | `IoC.Initialize(containerRegistry, Container)`（Common 模块） |
