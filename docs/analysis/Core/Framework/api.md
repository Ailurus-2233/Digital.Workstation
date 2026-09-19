# Framework — 对外接口与调用方式

命名空间为 `DigitalWorkstation.Core.Framework` 及其 `.Layout`、`.Menus`、`.Commands`、`.Contributions`、`.Settings`、`.Resources`、`.Windows`、`.WindowManager`。类型通常 public；attribute 扫描生成的 `ReflectedMenuItemContribution` 与 `ReflectedCommandContribution` 为 internal。

## 1. `FrameworkApplication<TWindow>`（FrameworkApplication.cs:20）

```csharp
public abstract class FrameworkApplication<TWindow> : PrismApplication where TWindow : Window
```

应用入口基类。子类（真实代码：`Modules/Workstation/WorkstationApplication.cs:15` 的 `WorkstationApplication : FrameworkApplication<MainWindow>`）必须做的事：泛型参数指定主窗口类型、重写 `CreateSplashWindow()`、可选重写 `RegisterCustomService()`、按 Prism 惯例重写 `ConfigureModuleCatalog`。

### 公开/保护成员

| 成员 | 签名 | 说明 |
|---|---|---|
| `Initialize` | `public override void Initialize()` | 固定 `RequestedThemeVariant = ThemeVariant.Dark`（第 28 行），`Styles.AddRange(new WorkstationTheme())`（第 29 行），`VSCodePalette.ApplyTo(Resources)`（第 31 行），最后调 `base.Initialize()` |
| `OnFrameworkInitializationCompleted` | `public override void OnFrameworkInitializationCompleted()` | 不调用 base；fire-and-forget 启动 `RunStartupSequenceAsync()`（第 41 行） |
| `OnInitialized` | `protected override void OnInitialized()` | 空方法（第 48-50 行），故意阻止 base 提前显示 MainWindow |
| `InitializeModules` | `protected override void InitializeModules()` | 空方法（第 55-57 行），抑制 Prism 同步一次性模块加载 |
| `CreateSplashWindow` | `protected abstract Window CreateSplashWindow()` | 子类提供启动台窗口；模块加载进度与失败决策均经启动台呈现 |
| `RegisterTypes` | `protected override void RegisterTypes(IContainerRegistry)` | **密封式编排**（注释明确"子类不需要重写"）：先 `RegisterFrameworkServices` 后 `RegisterCustomService`（第 189-193 行） |
| `RegisterCustomService` | `protected virtual void RegisterCustomService(IContainerRegistry)` | 子类注册自定义服务的钩子，默认空实现（第 215 行） |
| `PrepareShell` | `protected virtual void PrepareShell()` | Ready 前准备呈现；Workstation 在此收集贡献并接线菜单和手势，失败按序列级异常退出 |
| `CreateShell` | `protected override AvaloniaObject CreateShell()` | `Container.Resolve<TWindow>()`（第 224 行）——主窗口经容器解析，支持构造注入 |
| `ConfigureViewModelLocator` | `protected override void ConfigureViewModelLocator()` | 约定式 ViewModel 定位（第 237-269 行），见下 |

### 私有启动序列成员（改行为时直接面对）

- `RunStartupSequenceAsync()`：按依赖顺序逐模块建立贡献批次，后台 LoadModule 后回 UI 线程准备工厂并提交。依赖不可用、模块未完成初始化、注册或贡献构造失败都进入 Continue/Exit；失败批次不可见。全部完成后准备宿主贡献与 Shell 呈现，再发布 Ready。
- `WaitForFailureActionAsync(IEventAggregator)`（第 121 行）：一次性订阅 `StartupFailureActionEvent`，返回 `true` = Continue。
- `ShowMainWindow()`（第 131 行）：先做两个模式匹配守卫——`MainWindow is not Window window` 或 `ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime` 时**静默 return**（不抛异常、不动作）；通过后依次 `lifetime.MainWindow = window` → `_windowManager?.ShowMainWindow()` → `_windowManager?.CloseWindowsExceptMain()`。
- `RegisterFrameworkServices` / `ResolveFrameworkServices`：前者初始化 IoC、注册窗口管理器双接口单例、贡献收集器与布局持久化服务，显式构造 `SettingsService` 并立即 `Load()`，注册 `ISettingsService` 与 Framework 的常规/语言设置；再 `ApplyLanguageSetting` 按持久化语言设置当前及默认线程区域性，并用 `SharedResources.ProductName` 设置 Application.Name。必须先于模块 `RegisterTypes`：工具视图扫描时解析文本，菜单/命令启动准备时解析。设置元数据保留来源与键，设置页构造时才解析。末尾 `ResolveFrameworkServices` 解析并保存事件聚合器与主窗口管理器。

### ViewModel 定位约定（ConfigureViewModelLocator，第 237-269 行）

```
viewName.Replace("Views", "ViewModels")
后缀 Window 或 Page → 追加 "ViewModel"
后缀 View          → 追加 "Model"（即 *View → *ViewModel）
```

即 `**/Views/*View.axaml → **/ViewModels/*ViewModel.cs`；`**/Views/Windows/*Window.axaml → **/ViewModels/Windows/*WindowViewModel.cs`；`**/Views/Pages/*Page.axaml → **/ViewModels/Pages/*PageViewModel.cs`。映射后的全名再拼上程序集全名组成程序集限定名 `$"{viewModelName}, {viewAssemblyName}"`（第 262 行），其中 `viewAssemblyName` 来自 `viewType.GetTypeInfo().Assembly.FullName`（第 244 行，即 **View 所在程序集**的全名——ViewModel 必须与 View 同程序集才能解析到）；最后调 `Type.GetType(fullViewModelName)`（第 264 行）解析出 ViewModel 的 `Type` 并返回。解析不到（`viewName`/`viewAssemblyName` 为空或类型不存在）时返回 null（不抛异常）。View 中需 `mvvm:ViewModelLocator.AutoWireViewModel="True"` 启用。

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
| `HandleMainWindow()` | 从 `Application.Current as PrismApplication` 取 `MainWindow` 登记为 `_mainWindow` 并加入 `_windowMap`；取不到或已在映射中抛 `InvalidOperationException`（第 158-167 行）。**唯一调用点**：启动序列阶段 1（`FrameworkApplication.cs:76`），先于启动台显示 |
| `HideMainWindow()` / `ShowMainWindow()` | `_mainWindow?.Hide()/Show()`，空调用安全（第 169-170 行） |
| `CloseWindowsExceptMain()` | 先 `_windowMap.Values.Where(w => w != _mainWindow).ToList()` 物化快照（排除主窗口），再逐个 `Close()`（第 172-177 行）。必须 ToList：每个窗口 `Close()` 触发 `InitializeWindow` 挂的 `Closing` 处理器把类型从 `_windowMap` 移除，直接枚举 `Values` 会在迭代中修改集合 |

内部状态：`private readonly Dictionary<Type, Window> _windowMap`（第 17 行）、`private Window? _mainWindow`（第 22 行）。`InitializeWindow`（第 45 行）注册时挂 `Closing` 事件把类型从映射移除——窗口关闭后同类型可再次 ShowWindow。

**调用方式**：消费方构造注入 `IWindowManager` 或 `IMainWindowManager`（同一单例）。真实调用点：`Modules/Workstation/Menus/HelpMenus.cs:20` `windowManager.ShowDialog<AboutWindow>`；框架内部 `FrameworkApplication.cs:77` `Container.Resolve<IWindowManager>().ShowWindow(CreateSplashWindow())`。

## 3. `ShellLayoutState` 及区域 record（Layout/）

```csharp
public sealed record ShellLayoutState
{
    public string? SelectedActivity { get; init; }
    public IReadOnlyList<string> ActivityBarItems { get; init; } = [];  // 顶部段有序 Id（钉住项不入列，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）
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
| `SelectActivity(string id)`（ShellLayoutState.cs:36） | 已选中该 id 且 SideBar 可见 → 收起 SideBar（`SelectedActivity` 保留）；否则选中该 id、展开 SideBar 并置 `ContentFor = id`。不改 MainContent |
| `ToggleSideBar()`（:53） | 独立翻转 `SideBar.Visible`，不影响选中项与内容 |
| `ToggleAuxiliaryPanel()`（:61） | 独立翻转 `AuxiliaryPanel.Visible`，不影响活动 tab |
| `ToggleBottomPanel()`（:69） | 独立翻转 `BottomPanel.Visible`，不影响活动 tab |
| `ActivateAuxTab(string tab)`（:77） | AuxiliaryPanel 收起时**拒绝，返回 `this`**；面板可见时以 with 表达式更新 `AuxiliaryPanel.ActiveTab` 返回新实例 |
| `ActivateBottomTab(string tab)`（:90） | BottomPanel 收起时拒绝返回 `this`；面板可见时以 with 表达式更新 `BottomPanel.ActiveTab` 返回新实例 |
| `MoveTab(string tabId, ToolViewPlacement targetBar, int index)`（:109，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)） | 跨 Bar 迁移/同 Bar 重排：从源 Bar 移除、按 index 插入目标。跨 Bar：目标面板强制 `Visible=true` 且激活该 tab（目标 ActivityBar → 选中该导航项、展开 SideBar 并置 `ContentFor`）；源面板拖空 → `Visible=false`；源面板活动 tab 被拖走 → 回退到其**前一个** tab（原首位取移除后首个）；源为 ActivityBar 且被拖走的是选中项 → 顶部段仍有项则改选中其前一项、SideBar 保持展开，顶部段拖空才取消选中并收起。同 Bar 为纯重排（index 按移除前列表计，`sourceIndex < index` 时内部减一修正），不改激活状态。tabId 不属于任何 Bar（如钉住项）或原地落放 → 拒绝返回 `this` |
| `OpenMainView(string view)`（:236） | 仅改 `MainContent.ActiveView` |
| `Resize(PanelResizeTarget target, double delta)`（:244） | SideBar/AuxiliaryPanel 调 `Width`、BottomPanel 调 `Height`，经私有静态 `Clamp(double value, double min, double max)`（:275，实现为 `Math.Max(min, Math.Min(max, value))`）钳到各 record 的 Min/Max 常量；switch 兜底分支 `_ => this`，未知 target 返回等值状态 |

注意：**面板对齐（PanelAlignment）不在本状态机内**——它是 `FrameworkWindow` 的 StyledProperty（见第 4 节），由窗口依赖属性持有并双向绑定到 ViewModel，布局状态机只管五区域的可见性/尺寸/tab。

### 区域 record 字段与默认值

| 类型（文件） | 字段（默认） |
|---|---|
| `SideBarState`（Layout/SideBarState.cs:6） | `MinWidth=120`、`MaxWidth=480`（const）；`Visible=false`（bool 默认）、`Width=240`、`ContentFor=null`（收起时保留导航项 Id） |
| `AuxiliaryPanelState`（Layout/AuxiliaryPanelState.cs:6） | `MinWidth=120`、`MaxWidth=480`（const）；`Visible=true`、`Width=280`、`Tabs=[]`、`ActiveTab=null` |
| `BottomPanelState`（Layout/BottomPanelState.cs:6） | `MinHeight=80`、`MaxHeight=480`（const）；`Visible=true`、`Height=160`、`Tabs=[]`、`ActiveTab=null` |
| `MainContentState`（Layout/MainContentState.cs:6） | `ActiveView=null` |
**典型消费**（真实代码）：`Modules/Workstation/MainWindowViewModel.cs:60` `[ObservableProperty] private ShellLayoutState _state = ShellLayoutState.Initial;`，各 RelayCommand 调转换方法后整体替换 `_state`；本模块 `Layout/PanelResizer.cs` 经 `ResizeCommand` 以 `PanelResize` 参数把 GridSplitter 拖拽增量交给 `Resize`（见第 6 节），`Layout/ToolViewBar.cs` 经 `MoveCommand` 以 `ToolViewMove` 参数把拖拽落点交给 `MoveTab`（见第 6 节）。

## 4. `FrameworkWindow`（Windows/FrameworkWindow.cs:22）

```csharp
public abstract class FrameworkWindow : UrsaWindow
```

带基础布局的窗口基类：内置 VS Code 式五区 shell（ActivityBar/SideBar/MainContent/AuxiliaryPanel/BottomPanel + 状态栏）、**按平台呈现的菜单栏**（全部菜单贡献建树生成，[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)；macOS 使用屏幕顶部的 `NativeMenu`，其他平台使用标题栏左侧的 Avalonia `Menu`），**以及顶部居中的命令面板**（`CommandPalette`，[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)；宽松绑定 ViewModel 的 `Commands`，Ctrl+P 开关）。真实子类：`Modules/Workstation/MainWindow`。成员：

| 成员 | 签名 | 说明 |
|---|---|---|
| `PanelAlignmentProperty` | `public static readonly StyledProperty<PanelAlignment>` | 布局档位的依赖属性，默认 `PanelAlignment.Center`；与 ViewModel 双向绑定 |
| `PanelAlignment` | `public PanelAlignment PanelAlignment` | CLR 包装；写值触发 `OnPropertyChanged` → `UpdateLayoutTemplate` |
| `StyleKeyOverride` | `protected override Type StyleKeyOverride` | 返回 `typeof(UrsaWindow)`，继承 UrsaWindow 的窗口主题 |
| 构造函数 | `protected FrameworkWindow()` | 创建布局宿主与命令面板；非 macOS 创建 `LeftContent` 标题栏菜单，macOS 留空以避开 traffic-light 按钮；注册菜单项 DataTemplate 后装载当前布局模板 |
| `RegisterNativeMenu` | `protected void RegisterNativeMenu(IEnumerable<MenuItemViewModel>)` | 仅 macOS 生效；在贡献收集完成后把呈现树递归转换为原生菜单并通过 `NativeMenu.SetMenu(this, menu)` 附加到窗口；其他平台 no-op |
| `RegisterCommandGestures` | `public void RegisterCommandGestures(IEnumerable<ICommandContribution>)` | 为带 Gesture 的命令生成窗口级 KeyBinding；解析失败记英文 Warning 并跳过 |

`RegisterNativeMenu` 的转换保留标题和 `Command`，`MenuItemViewModel.Children` 中的子节点递归生成 `NativeMenuItem`，Avalonia `Separator` 映射为 `NativeMenuItemSeparator`。菜单模型仍只有一份，不维护第二套平台专用贡献。

枚举→资源键映射（:96-102）：

| PanelAlignment | 资源键 | BottomPanel 跨度（模板内 `Grid.Column`/`ColumnSpan`） |
|---|---|---|
| `Left` | `WindowLayoutLeft` | 列 1 跨 2（SideBar + MainContent 列下方）；AuxiliaryPanel 及其分隔条 `RowSpan=2` 通高到底 |
| `Right` | `WindowLayoutRight` | 列 2 跨 2（MainContent + AuxiliaryPanel 列下方）；SideBar 及其分隔条 `RowSpan=2` 通高到底 |
| `Center`（默认，switch 兜底） | `WindowLayoutCenter` | 列 2 跨 1（仅 MainContent 列下方）；SideBar 与 AuxiliaryPanel 及各自分隔条 `RowSpan=2` 通高到底 |
| `Justify` | `WindowLayoutJustify` | 列 1 跨 3（三列全宽）；侧栏只占第 0 行 |

## 5. `FrameworkWindowTheme`（Windows/FrameworkWindowTheme.cs）

```csharp
public class FrameworkWindowTheme : Styles
```

FrameworkWindow 的基础布局主题。加载机制：构造函数创建 `StyleInclude`（BaseUri `avares://DigitalWorkstation.Core.Framework/Windows/`；Source 相对 `FrameworkWindowTheme.axaml`），先 `_ = include.Loaded` **强制加载**（保证窗口构造期即可查到布局模板资源）再 `Add(include)`。与 Semi/Ursa 主题同款机制；不用 x:Class code-behind 的原因见 common.md「核心设计逻辑」。

`public static CustomPopupPlacementCallback MenuPopupPlacement { get; }` 是 axaml 的定位回调入口，仅用于 `Menu.chrome-menu > MenuItem /template/ Popup#PART_Popup`（`Placement=Custom`）。它按锚定矩形中心所在屏幕裁剪 AnchorRectangle，设置 BottomLeft 锚点和 BottomRight 重力；Offset、ConstraintAdjustment 保留调用方值。无 TopLevel/屏幕信息时仍保持向下左对齐。子菜单保持主题原有定位。

资源清单（`Windows/FrameworkWindowTheme.axaml`，全部在 `Styles.Resources` 内）：

| 资源键 | 位置 | 内容 |
|---|---|---|
| `ShellActivityBar` / `ShellSideBar` / `ShellMainContent` / `ShellAuxiliaryPanel` / `ShellBottomPanel` / `ShellStatusBar` | :27 / :59 / :75 / :87 / :142 / :198 | 六个共享部件 DataTemplate（三处可投放 Bar 用 ToolViewBar 承载；ActivityBar 底部段为 StackPanel（:41-53）＝钉住区 ItemsControl（`BottomNavigationItems`，当前无钉住项实例、可为空）+ shell 内置"设置"导航按钮（`Command={Binding OpenSettingsCommand}`，[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 6），与顶部段以 `Grid RowDefinitions="*,Auto"` 分隔——不用 DockPanel bottom dock，见 pitfalls.md） |
| `WindowLayoutLeft` / `WindowLayoutRight` / `WindowLayoutCenter` / `WindowLayoutJustify` | :227 / :293 / :359 / :424 | 四份布局 DataTemplate：布局 Grid 列 `Auto,{Binding SideBarColumnWidth},*,{Binding AuxiliaryColumnWidth}`、行 `*,Auto`；差异为 BottomPanel 及分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`（见上表）；ActivityBar 恒 `RowSpan=2` 通高 |
| `{x:Type layout:ToolViewBar}` | :492 | ToolViewBar 的 ControlTheme（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：Background=Transparent 使整条带参与命中测试；模板为 Border + Panel(ItemsPresenter + `PART_InsertionLine` 拖拽占位线） |

样式（:513 起）：nav-item/panel-tab/GridSplitter/panel-collapse/region-title/placeholder/status-item 自 Modules/Workstation/MainWindow.axaml 迁入；`layout|ToolViewBar.drag-over`（:554，拖拽悬停整 Bar 高亮，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）；4 个标题栏菜单样式（:611-628，[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md) 菜单栏内置配套）：`Menu.chrome-menu > MenuItem` 紧凑行高（MinHeight=30、Padding=10,0，:611-614）、`Popup#PART_Popup` VerticalOffset=-8 让弹出层贴合标题栏下缘（:616-619）、`Menu.chrome-menu MenuItem` 的 `ItemsSource={Binding Children}`/`Command={Binding Command}`/`AutomationProperties.Name={Binding Title}` 样式绑定（:621-624，叶子 Children 为空、节点 Command 为 null，均无副作用）、`PathIcon` 前景色 SemiColorText1（:626-628）。**菜单项的内容模板不在本主题内**——无 x:Key 的 DataTemplate 不能放 `Styles.Resources`（AVLN3000），故由 `FrameworkWindow` 构造时在窗口 `DataTemplates` 代码注册（见第 4 节）。分隔条改用 `layout:PanelResizer` 的 `Target`+`ResizeCommand` 声明式绑定（如 :240-252；axaml 以 `xmlns:layout="clr-namespace:DigitalWorkstation.Core.Framework.Layout"` 引入，:3）。**所有绑定为宽松反射绑定**——Framework 不引用具体 ViewModel 类型。面板对齐的切换入口不在本主题内（在视图菜单，见 Modules/Workstation 的 `Menus/ViewAlignmentMenus.cs`）。

命令面板样式（:630-667，[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md) 配套）：`windows|CommandPalette` 浮层外观（SemiColorBackground1 底 + 边框 + 圆角 6 + 阴影，宽 600、顶部居中、上缘距 48）、`TextBox.command-input` 透明无边框、`ListBox.command-list` 透明底与条目圆角、`TextBlock.gesture` 右侧快捷键文本（SemiColorText2）、`PathIcon.command-icon` 条目左侧图标槽位（12×12、SemiColorText2、右间距 8、垂直居中；始终渲染，`IconPath` 为 null 时为空占位，文本对齐）。

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
**调用方式**：布局模板内声明式使用——`<layout:PanelResizer Target="SideBar" ResizeCommand="{Binding ResizePanelCommand}" .../>`（真实用例 `Windows/FrameworkWindowTheme.axaml:240-252` 等，每份布局模板三枚：SideBar/AuxiliaryPanel/BottomPanel）。ViewModel 侧契约：提供接受 `PanelResize` 参数的 `ResizePanelCommand`（真实实现 `Modules/Workstation/MainWindowViewModel.cs` 转调 `ShellLayoutState.Resize`）。

### 工具视图拖拽控件（ToolViewButton / ToolViewBar / ToolViewMove / ToolViewDragSession，均位于 Layout/，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）

```csharp
public class ToolViewButton : Button                                    // ToolViewButton.cs:14
public class ToolViewBar : ItemsControl                                 // ToolViewBar.cs:19
public sealed record ToolViewMove(string TabId, ToolViewPlacement TargetBar, int Index); // ToolViewMove.cs:12
public static class ToolViewDragSession                                 // ToolViewDragSession.cs:11
```

- `ToolViewButton`（拖拽源）：`DragTabId`（StyledProperty，负载视图 Id）与 `CanDrag`（StyledProperty，默认 true；钉住项绑 `Contribution.AllowMove`=false → 不发起拖拽、保持普通点击）。左键按下后位移超 4px 经 `DragDrop.DoDragDropAsync`（Avalonia 11.3 DataTransfer API，负载为 `ToolViewDragSession.TabIdFormat` 应用格式）发起拖拽，前后 `ToolViewDragSession.Begin/End`。`StyleKeyOverride => typeof(Button)` 使 `Button.nav-item`/`Button.panel-tab` 类样式命中。
- `ToolViewBar`（投放目标）：`TargetBar`（ToolViewPlacement）、`Orientation`（横向面板条/纵向 ActivityBar，决定插入序号轴向与占位线方向）、`MoveCommand`（ICommand）。构造置 `DragDrop.SetAllowDrop(true)` 并挂 DragOver/Drop/DragLeave：DragOver 按指针位置算插入序号（条目前半→插其前）并把模板内 `PART_InsertionLine` 占位线（2px 蓝线）移到落点缝隙，Drop 执行 `MoveCommand(new ToolViewMove(id, TargetBar, index))`；DragLeave 有冒泡守卫（指针真正离开才清除指示）。**不得声明 StyleKeyOverride**：Avalonia 类型选择器匹配 StyleKey，其 ControlTheme（透明背景使整条带可命中）在 FrameworkWindowTheme.axaml:492。
- `ToolViewMove`：落点参数；`Index` 按目标 Bar 移除前的列表计，同 Bar 重排的修正在 `ShellLayoutState.MoveTab` 内部。
- `ToolViewDragSession`：`IsActive` + `ActiveChanged` 静态信号；拖拽进行中 shell 临时显露隐藏面板作为投放区（「向隐藏面板拖入则自动显示」的先决条件）。

**调用方式**：主题模板内声明式使用——导航项/tab 头用 `<layout:ToolViewButton DragTabId="{Binding Id}" CanDrag="{Binding Contribution.AllowMove}" .../>`，三处可投放 Bar 用 `<layout:ToolViewBar TargetBar="..." Orientation="..." MoveCommand="{Binding MoveTabCommand}">`（真实用例 `Windows/FrameworkWindowTheme.axaml:35-40、106-133、162-189`）。ViewModel 侧契约：提供接受 `ToolViewMove` 参数的 `MoveTabCommand`（真实实现 `Modules/Workstation/MainWindowViewModel.cs:440`）与 `AuxiliaryPanelRevealed`/`BottomPanelRevealed` 布尔属性。


## 7. `ShellLayoutDto` 族与 `LayoutPersistence`（均位于 Layout/，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）

```csharp
public sealed record ShellLayoutDto          // ShellLayoutDto.cs:10，const CurrentVersion = 1（:15）
public sealed record ToolViewPlacementEntry  // ShellLayoutDto.cs:41
public sealed record SideBarLayoutDto        // ShellLayoutDto.cs:54
public sealed record PanelLayoutDto          // ShellLayoutDto.cs:69
public sealed record BottomPanelLayoutDto    // ShellLayoutDto.cs:81
public sealed class LayoutPersistence(ConfigurationPersistence persistence)
```

落盘格式（`ShellLayoutDto` 族）独立于 `ShellLayoutState`——状态机只管流转语义，不管序列化兼容（文件注释自述）。`ShellLayoutDto` 成员：`Version`（:17，`Load` 对不识别版本整份丢弃）、`PanelAlignment`（:22，对齐档位由 `FrameworkWindow` 依赖属性持有、不在状态机内，一并持久化）、`Placements: Dictionary<string, ToolViewPlacementEntry>`（:29，可移动工具视图 Id → `{ Bar, Index }`，恢复时优先于 attribute 的 `Default`；**钉住项恒在 ActivityBar 底部段、不入此表**；无对应贡献的孤儿条目丢弃，无条目的新工具视图落回 `Default`）、三个可空子 DTO `SideBar`/`AuxiliaryPanel`/`BottomPanel`（:31-35，各记显隐/尺寸/选中项或活动 tab；为 null 表示该区域无持久化数据，恢复时保持默认）。序列化选项（`LayoutPersistence.cs:23-28`）：`WriteIndented` + camelCase 属性名 + `JsonStringEnumConverter`——枚举落成 `"Center"`/`"BottomPanel"` 形态字符串。

LayoutPersistence 以 singleton 注册，构造时接收统一的 ConfigurationPersistence；路径与 JSON 格式保持不变。

| 成员 | 接口 | 语义 |
|---|---|---|
| FilePath | public static readonly string | %AppData%/Digital.Workstation/layout.json |
| Load | public ShellLayoutDto? Load() | 缺失静默返回 null；空内容、未知 Version、读取/反序列化失败记 LayoutPersistence Warning 并返回 null |
| ScheduleSave | public void ScheduleSave(ShellLayoutDto layout) | 提交独立 DTO 快照，由 DebouncedJsonFile 合并 500ms 内变更 |
| Delete | public void Delete() | 等待同文件在途写入，在同一锁内作废 pending 并删文件；删除失败记 ConfigurationPersistence Warning |

### 配置写入生命周期（Persistence/）

```csharp
public sealed class ConfigurationPersistence : IDisposable
{
    public bool FlushPending();
    public void Dispose();
}
```

FrameworkApplication 注册唯一 owner；SettingsService 与 LayoutPersistence 通过内部 `CreateFile<T>` 各注册一个文件。FlushPending 等待各文件在途写入并保存 pending，任一失败返回 false，但仍尝试其余文件；失败快照留待下一次修改或显式刷新时重试。Dispose 在真正 Exit 时进行最后一次保存并释放计时器，幂等；此后 ScheduleSave/Delete 属生命周期误用，会抛 ObjectDisposedException。

内部 `DebouncedJsonFile<T>` 不向业务模块公开。文件锁覆盖 ScheduleSave、Timer 回调、FlushPending、Delete、Dispose；JSON 先写同目录唯一临时文件并 Flush(true)，成功后 File.Move(overwrite:true) 替换。异常就地记录英文 Warning，旧目标文件与 pending 保留，临时文件尽力清理。回调不获取 SettingsService 的内存锁，设置提交快照的锁顺序固定为“设置内存 → 文件”。

## 8. 贡献目录与收集器（Contributions/）

```csharp
public sealed class ShellContributionCatalog(IContainerProvider provider);
public IReadOnlyList<T> Get<T>() where T : class;
public static void RegisterShellContribution<T>(this IContainerRegistry registry,
    Func<IContainerProvider, T> factory) where T : class;
public static void RegisterShellContribution<T, TImplementation>(this IContainerRegistry registry)
    where T : class where TImplementation : class, T;
public class ShellContributionCollector(ShellContributionCatalog catalog, SettingCatalog settings);
```

登记入口把工厂封装为内部 descriptor，捕获当前启动批次，按 Lazy 单次构造。内部 BeginBatch/Prepare/Reject/Dispose 由启动序列编排；模块不操纵批次。Prepare 关闭登记后解析该批全部工厂，全部成功才发布；失败批次留存的 descriptor 永不被 Get 返回。无批次的框架设置可在应用语言前读取，宿主其余贡献在 Ready 前准备。普通 DI 服务不回滚，工具视图内容不预创建。手写贡献必须用上述入口，直接注册贡献接口不能被该目录收集。

| 收集入口 | 规则 |
|---|---|
| GetToolViews() | 从目录读取显式工具视图，按 Order；零登记返回空集合 |
| GetMainViews() | 按登记顺序返回主视图元数据 |
| GetMenuItems() | 返回预构造的菜单项；建树与排序仍由 MenuTreeBuilder 负责 |
| GetCommands() | Id 首个生效，再按 Order、Title Ordinal 排序 |
| GetStatusBarItems() | 按 Order 排序 |
| GetSettingItems()/GetSettingGroups() | 委托同一 SettingCatalog，与 SettingsService 共享声明规则 |

### 布局配置投影与模板尺寸

ShellLayoutConfiguration.Restore(contributions, layout, mainContent) 返回 (State, Alignment)：保存当前主视图状态，恢复可移动项归属和顺序、活动项、选中项、尺寸与对齐；丢弃孤儿与非法位置并使用默认值。Capture(state, alignment) 只读取状态的有序 Id，不枚举呈现集合；钉住项不写入 placements。

ShellLayoutMetrics.CardMargin/ContainerPadding/ActivityBarMargin 被主题 x:Static 引用；PanelColumn(contentWidth, visible) 使用同一 CardMargin 水平尺寸生成 GridLength，隐藏时为零。

## 9. `MenuItemViewModel`（Menus/MenuItemViewModel.cs:13）

```csharp
public class MenuItemViewModel
```

菜单项的呈现模型（自 Modules/Workstation 迁入，命名空间 `DigitalWorkstation.Core.Framework.Menus`）：叶子（`Command` 非空）或子菜单节点（`Children` 非空），由菜单树（`MenuTreeSubmenu`）递归转换而来；分隔线直接是 Avalonia `Separator` 控件。成员：

| 成员 | 签名 | 说明 |
|---|---|---|
| `Title` | `public required string Title { get; init; }`（:19） | 已解析的显示标题 |
| `Icon` | `public Geometry? Icon { get; init; }`（:24） | 图标几何（随主题变色）；null = 无图标——弹出层内模板仍渲染空 `PathIcon` 占位对齐，顶层菜单不预留 |
| `Command` | `public ICommand? Command { get; init; }`（:26） | 叶子命令；节点为 null |
| `IsTopLevel` | `public bool IsTopLevel { get; init; }`（:32） | 是否标题栏顶层菜单项：顶层项不预留图标槽位（避免标题文本缩进）；仅 `MainWindowViewModel` 建根项时经 `FromSubmenu(…, isTopLevel: true)` 置位，递归子项恒为 false |
| `Children` | `public ObservableCollection<object> Children { get; }`（:34） | 子项（`MenuItemViewModel` 或 `Separator`） |
| `FromSubmenu` | `public static MenuItemViewModel FromSubmenu(MenuTreeSubmenu, bool isTopLevel = false)`（:39） | 递归转换：`MenuTreeItem` → 叶子（`IconPath` 经 `StreamGeometry.Parse` 转几何，:49）、`MenuTreeSubmenu` → 递归（不带 isTopLevel，:52）、分隔线 → `new Separator()`（:44-54） |

**消费约定**：shell 宿主窗口的 ViewModel 暴露 `MenuBarItems` 顶层集合（宽松绑定约定），`FrameworkWindow` 内置的 `Menu` 直接绑定它（见第 4 节）；真实实现 `Modules/Workstation/MainWindowViewModel.cs:108`。

## 10. 菜单树数据结构 `MenuTreeEntry` 族（Menus/MenuTreeEntry.cs:10）

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

建树产物：叶子菜单项（`MenuTreeItem`，标题已按当前 UI 区域性解析）、子菜单节点（`MenuTreeSubmenu`，含顶层菜单；`Children` 中分隔线已按分组规则插好，不存在开头/结尾/连续分隔线）、组间分隔线标记（`MenuTreeSeparator`，私有构造的单例）。由 `MenuTreeBuilder` 生成，本模块的 `MenuItemViewModel.FromSubmenu`（见第 9 节）再转换为 Avalonia 控件（分隔线转 `Separator`）。

## 11. `MenuTreeBuilder`（Menus/MenuTreeBuilder.cs:13）

```csharp
public static class MenuTreeBuilder
{
    public static IReadOnlyList<MenuTreeSubmenu> Build(IEnumerable<IMenuItemContribution> contributions); // :18
}
```

纯函数、无状态建树器：把扁平贡献构建为分组排序好的顶层菜单列表。只消费已解析文本，完全不查资源；`Path` 是稳定 Id 路径，`PathTitle` 是可空末端标题。规则：

- **路径切分与跳过**：`Path.Split('/', TrimEntries)`，按 Ordinal 稳定段逐层索引；含空段记 Warning 跳过该条目。同资源键或同翻译的不同路径不会合并。
- **标题声明**：只把末端节点的第一个非 null `PathTitle` 记入 `DeclaredTitle`；引用贡献的 null 无标题意见，不占首个位置。隐式祖先或只有引用的节点先显示稳定 Id 段；之后到达的所有者声明仍能命名它。
- **单段路径**：`NodeOrder` 经 `MergeNodeOrder` 声明顶层位次，多处取最小；`Group`/`GroupOrder` 描述条目分组。
- **多段路径**：`Group`/`GroupOrder`/`NodeOrder` 经 `MergePlacement` 声明末端子菜单位次；`NodeOrder`/`GroupOrder` 取最小，Group 取 GroupOrder 最小声明的组名、同值取 Ordinal 小者；条目进末端默认组。位次合并不覆盖已声明标题。
- **顶层排序**：按 `(NodeOrder, Title Ordinal)`，不分组不插分隔线。
- **子菜单内排序**：条目与递归子节点按 `(GroupOrder, null 组优先, Group Ordinal, Order, Title Ordinal)` 排序，相邻组变化处插单例分隔线，不产生开头/结尾/连续分隔线。

内部临时结构：`LeafAccum` 保存叶子的分组/位次/标题/图标/命令；`NodeAccum` 的 `Children` 按稳定段索引，`Items` 保存叶子，`DeclaredTitle` 可空，`Title` 在无声明时回退段 Id。`MergeNodeOrder`/`MergePlacement` 保留既有最小位次规则。

## 12. `MenuRegistration.RegisterMenus`（Menus/MenuRegistration.cs:14）

```csharp
public static void RegisterMenus(this IContainerRegistry registry, Assembly assembly); // :20
```

attribute 菜单注册扩展（[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）。模块在自身 `RegisterTypes` 中调用并传入本模块程序集，**不做全局程序集扫描**；扫描只在注册时发生一次。规则（:21-68）：

1. 遍历 `assembly.DefinedTypes`，取标注 `MenuGroupAttribute` 的类（:22-28）；类路径含空段记 `Logger.Warning` 整类跳过（:30-35）。
2. 取该类 `Public | Instance | DeclaredOnly` 方法中标注 `MenuItemAttribute` 者（:37-40）；带参或返回值非 `void`/`Task` 的记 `Logger.Warning` 跳过（:44-51）——因此**静态方法与泛型方法**（非实例/含参）天然进不了候选或被签名校验挡下。
3. 类内无合法方法则整体跳过（:55-58）；否则菜单类本体 `RegisterSingleton(menuType)`（:61），每个合法方法经 RegisterShellContribution 注册一个 `IMenuItemContribution` 工厂（:62-66），工厂内 `provider.Resolve(menuType)` 取菜单类 singleton 实例（启动准备时经容器解析一次，之后复用）。

### `ReflectedMenuItemContribution`（internal，:75）

由 `RegisterMenus` 生成的 `IMenuItemContribution` 实现，首次从容器解析 singleton 时构造。`Title = ResourceText.Get(item.ResourceType, item.Title)`；类级 `MenuGroup` 同时有来源与键时 `PathTitle = ResourceText.Get(group.ResourceType, group.TitleKey)`，仅引用路径时为 null。`Path`/`Group`/`GroupOrder` 来自类级，`NodeOrder=group.Order`、条目 Order/图标来自方法级，`Command` 包装反射执行。`ExecuteAsync` 等待 Task，解包 `TargetInvocationException` 后记 Error，不抛出。外部模块用 `[MenuGroup("shell.file")]` 挂接已有根、用自己资源声明条目即可，不依赖 WorkstationResources；所有者标题可晚于引用贡献到达。

## 13. `ToolViewRegistration.RegisterToolViews`（Contributions/ToolViewRegistration.cs:14）

```csharp
public static void RegisterToolViews(this IContainerRegistry registry, Assembly assembly); // :20
```

attribute 工具视图注册扩展（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)），与 `RegisterMenus` 同构。模块在自身 `RegisterTypes` 中调用并传入本模块程序集，**不做全局程序集扫描**；扫描只在注册时发生一次。规则（:21-59）：

1. 遍历 `assembly.DefinedTypes`，取标注 `ToolViewAttribute` 的类（:23-29）。
2. 类非可实例化 `Control`（abstract 或非 `Control` 派生）记 `Logger.Warning` 跳过（:31-36）。
3. `Id` 在**本程序集内**重复（`seenIds` 局部 HashSet，:22）记 `Logger.Warning` 跳过（:38-44）；跨程序集重复不在此处检测。
4. 合法者注册 View 类型并生成 singleton 元数据；`Title = ResourceText.Get(attribute.ResourceType, attribute.TitleKey)` 在扫描时解析，`Placement` 取 attribute.Default。资源所属程序集由显式 Type 决定，与被扫描程序集可以不同。

真实调用点：`Modules/Workstation/WorkstationApplication.cs:27`、`Modules/DashBoard/DashBoardModule.cs:14`。

## 14. `CommandRegistration.RegisterCommands`（Commands/CommandRegistration.cs:13，[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）

```csharp
public static void RegisterCommands(this IContainerRegistry registry, Assembly assembly); // :19
```

attribute 命令注册扩展，与 `RegisterMenus` 同构但**免类级 attribute**。模块在自身 `RegisterTypes` 中调用并传入本模块程序集，**不做全局程序集扫描**；扫描只在注册时发生一次。规则（:21-55）：

1. 遍历 `assembly.DefinedTypes`，取 `Public | Instance | DeclaredOnly` 方法中标注 `CommandAttribute` 者（:24-26）——任何类的方法都可成为命令，类仅作 DI 宿主。
2. 带参或返回值非 `void`/`Task` 的记 `Logger.Warning` 跳过（:30-36）——静态方法与泛型方法天然进不了候选或被签名校验挡下。
3. 类内无合法方法则整体跳过（:39-42）；否则宿主类本体 `RegisterSingleton(hostType)`（:45），每个合法方法经 RegisterShellContribution 注册一个 `ICommandContribution` 工厂（:46-50），工厂内 `provider.Resolve(hostType)` 取宿主类 singleton 实例（启动准备时经容器解析一次，之后复用）。

### `ReflectedCommandContribution`（internal，:60）

由 `RegisterCommands` 生成的 `ICommandContribution` 实现。构造期（启动准备）以 `ResourceText.Get(attribute.ResourceType, attribute.Title)` 解析标题；Id 缺省「声明类全名.方法名」，Gesture/IconPath/Order 透传，Command 包装反射执行。执行路径与菜单同构：fire-and-forget 调异步执行，Task 等待，异常解包后记 Error、不抛出。解析后文本随该 singleton 存活，语言切换下次启动生效。

## 15. `CommandPalette`（Windows/CommandPalette.cs:18，[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）

```csharp
public class CommandPalette : Border
```

命令面板控件：窗口顶部居中的命令检索浮层，自包含——搜索框 + 列表 + 过滤 + 键盘导航 + MRU 全部内聚（控件模式同 `PanelResizer`/`ToolViewBar`），VM 只暴露 `Commands` 集合、零交互逻辑。成员：

| 成员 | 签名 | 说明 |
|---|---|---|
| `ItemsSourceProperty` / `ItemsSource` | `StyledProperty<IEnumerable<ICommandContribution>?>`（:20） | 命令数据源（`FrameworkWindow` 构造时宽松绑定 `"Commands"`）；变更且面板可见时重建列表 |
| `Open` | `public void Open()`（:76） | 显示、清空输入、重建列表、聚焦输入框；并开始监听 TopLevel 的 PointerPressed（Tunnel）实现面板外点击关闭 |
| `Close` | `public void Close()`（:89） | 隐藏并摘掉面板外点击监听；Esc、失焦、执行命令后均走此 |

快捷键标签：`BuildItem` 经私有 `FormatGesture(string?)` 使用 `KeyGesture.Parse(...).ToString("p", null)` 生成平台可读文本；例如 Windows 上 `Ctrl+OemComma` 显示为 `Ctrl+,`。原始 `ICommandContribution.Gesture` 继续供窗口级 KeyBinding 解析。空值不格式化；`FormatException` 时保留原声明，命令列表仍可打开。

行为细节：过滤为子串、不区分大小写、匹配本地化后 `Title`（`RefreshItems`，:153）；MRU 内存列表 `_recentIds`（:29，新者在前，重启即清）执行后置顶、过滤后仍浮到最前；`OnKeyDown`（:105）处理 Esc/Enter/↑/↓（输入框单行，这些键不被吞，冒泡到控件）；单击条目即执行（`OnItemTapped`，:187，守卫点在条目容器内）；执行先 `Close()` 再 `Command.Execute(null)`（:204）。列表项模板为代码创建的 `FuncDataTemplate<ICommandContribution>`（:44，三列 Grid：左侧 `PathIcon.command-icon` 槽位始终渲染（`IconPath` 为 null 时为空占位，文本与有图标命令对齐——同菜单弹出层惯例）+ 标题 + 右侧 gesture 文本，`Gesture` 为 null 时隐藏）；空态「无匹配命令」与列表同格切换。样式在 `FrameworkWindowTheme.axaml:630-667`；`StyleKeyOverride` 未声明——类型选择器 `windows|CommandPalette` 按 StyleKey 匹配（同 `ToolViewBar` 的坑，见 pitfalls.md）。

## 16. 设置管线（Settings/，[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md)）

### `SettingRegistration.RegisterSettings`（Settings/SettingRegistration.cs:13）

```csharp
public static void RegisterSettings(this IContainerRegistry registry, Assembly assembly); // :19
```

attribute 设置注册扩展（[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 1），与 `RegisterMenus`/`RegisterCommands` 同构。模块在自身 `RegisterTypes` 中调用并传入本模块程序集，**不做全局程序集扫描**；扫描只在注册时发生一次。规则（:21-66）：

1. 遍历 `assembly.DefinedTypes`，类上每个 `SettingGroupAttribute` 生成一个 singleton 元数据，透传 `Id`/`ResourceType`/`Name`/`Order`；扫描期不查资源。同 Id 合并、首个来源/名称保留与最小 Order 均在收集侧完成。
2. 取该类 `Public | Static | DeclaredOnly` 属性中标注 `SettingItemAttribute` 者（:29-36）；无 getter（:38-43）或 `DefaultValue` 非空且类型与属性类型不匹配（:45-51）记 `Logger.Warning` 跳过。属性只是声明锚点——扫描不读属性值，读写一律走 `ISettingsService`。
3. 合法属性生成 singleton 元数据，Id 缺省「声明类全名.属性名」、ValueType 取属性类型，透传稳定 `Group`、`ResourceType`、名称键 `Name`、默认值/位次/重启标志。设置页分组 Key 使用 Id；显式分组、设置项与枚举选项在页面构造时各自从元数据来源查询，隐式分组直接显示 Id。

真实调用点：`FrameworkApplication.cs:168`（Framework 自身「常规/语言」设置项）。

### SettingCatalog（Settings/SettingCatalog.cs）

构造注入 ShellContributionCatalog。GetItems() 对当前可见声明按 Id 首个生效，再排序；Find(id) 使用同一有效集合，不按无关查询覆盖缓存。重复实例只记一次英文 Warning。GetGroups() 对显式分组按 Id 合并，保留首个名称/来源、Order 取最小；隐式分组仅由有效设置项补齐，被丢弃的重复项不会产生空分组。每次读取当前目录，接受后加载模块，并撤销失败批次的候选声明。

### SettingsService（Settings/SettingsService.cs）

```csharp
public sealed class SettingsService(
    IEventAggregator eventAggregator,
    SettingCatalog catalog,
    ConfigurationPersistence persistence) : ISettingsService
```

FrameworkApplication 显式构造并 Load，再以 ISettingsService 工厂单例注册。FilePath 仍指向 %AppData%/Digital.Workstation/settings.json，枚举使用原有字符串格式。

| 成员 | 接口/语义 |
|---|---|
| Load() | 一次性加载内存值和启动值快照；缺失静默返回，损坏记 Warning 按默认值处理 |
| `Get<T>(string settingId)` | 纯内存读取，单项反序列化失败回落贡献默认值；未声明返回 default 并记 Warning |
| `Set<T>(string settingId, T value)` | 内存锁内更新值、向文件写入模块提交独立字典快照、维护重启标记；锁外广播 SettingChangedEvent |
| IsPendingRestart(string settingId) | 回答值是否偏离启动值；RequiresRestart 过滤仍由消费方负责 |
| FindContribution(string settingId) | 委托 SettingCatalog.Find；无服务私有声明缓存，拒绝批次不会污染后续查询 |

SettingsService 不再拥有 Timer、FlushPending、TakeSnapshot 或 Save。生命周期刷新统一通过 ConfigurationPersistence；调用方不应将 ISettingsService 转为具体类做保存。SettingsService 内存锁内提交快照以保持 Set 顺序，文件回调只处理收到的快照。

### `UiLanguage` 与 `UiLanguageExtensions`（Settings/UiLanguage.cs:12）

```csharp
public enum UiLanguage { [JsonStringEnumMemberName("zh-CN")] ZhCN, [JsonStringEnumMemberName("en-US")] EnUS } // :12-25
public static CultureInfo ToCultureInfo(this UiLanguage language);                                            // :32
```

Framework 预置「常规/语言」设置项的值类型（[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 9）。成员的 `JsonStringEnumMemberName` 值即对应 `CultureInfo` 名称——settings.json 落盘值与区域性名称同源；`ToCultureInfo`（:32-37）反射读该 attribute 值作为 `CultureInfo.GetCultureInfo` 的名称。

### `GeneralSettings`（Settings/GeneralSettings.cs:10）

```csharp
[SettingGroup(GeneralSettings.GroupId, typeof(FrameworkResources), nameof(FrameworkResources.SettingsGeneralGroupName), Order = 0)]
public static class GeneralSettings
```

Framework 预置常规/语言设置声明类。`public const string GroupId = "framework.general"` 是稳定分组 Id；`LanguageSettingId` 继续使用「声明类全名.属性名」，持久化 key 不变。语言属性声明为 `[SettingItem(GroupId, typeof(FrameworkResources), nameof(FrameworkResources.SettingsLanguageName), DefaultValue = UiLanguage.ZhCN, RequiresRestart = true)]`。属性体只作声明锚点，读写经 `ISettingsService`。

### `FrameworkResources`（Resources/）

`DigitalWorkstation.Core.Framework.Resources.FrameworkResources` 为 public static 资源所属类型，`.cs`、中性中文 `.resx`、`.en-US.resx` 同目录同基名。包含命令面板水印/空态与常规/语言设置名称、两种枚举选项名称；强类型属性经 `ResourceText.Get(typeof(FrameworkResources), nameof(Key))` 查找。不承载模块私有文字，产品名使用 Core/Resource 的 `SharedResources`。

## 17. `ApplicationRestarter`（ApplicationRestarter.cs:14，[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 7）

```csharp
public static class ApplicationRestarter { public static void Restart(); } // :19
```

“立即重启”仍由设置页横幅触发。ApplicationRestarter.Restart 先解析 ConfigurationPersistence，统一 FlushPending 设置和布局；返回 false 时记录 "Failed to save pending configuration; restart aborted" 并保留当前进程。成功后检查 Environment.ProcessPath，沿用原始参数启动新进程，再 Shutdown 当前进程；Exit 会进行最后收尾且不会重复写入已清空的 pending。路径缺失时记录 Error 中止。它不再解析或强转 ISettingsService。

## 容器注册清单（对外可解析的服务）

`RegisterFrameworkServices`（FrameworkApplication.cs:146-173）注册：

| 服务 | 注册方式 | 实现 |
|---|---|---|
| `IMainWindowManager` | Singleton | 同一 `FrameworkWindowManager` 实例 |
| `IWindowManager` | Singleton | 同一 `FrameworkWindowManager` 实例 |
| `ShellContributionCatalog` | Singleton | 显式贡献、批次可见性与单次构造 |
| `SettingCatalog` | Singleton | 设置声明唯一解释入口 |
| `ShellContributionCollector` | Singleton | 从目录读取并排序 |
| `ConfigurationPersistence` | 显式实例 | 统一持久化 owner；Exit 时 Dispose，重启前 FlushPending |
| `LayoutPersistence` | Singleton | 自身（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)；机制在 Framework、接线在 shell 模块） |
| `ISettingsService` | Singleton（工厂） | 显式构造的 `SettingsService` 实例，注册前已 `Load()`（[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 3/4） |
| `IoC.Registry` / `IoC.Provider` | 静态初始化 | `IoC.Initialize(containerRegistry, Container)`（Common 模块） |
