# Framework — 对外接口与调用方式

命名空间八组：`DigitalWorkstation.Core.Framework`（根）、`.Framework.Layout`（布局状态机、面板分隔条与布局持久化）、`.Framework.Menus`（菜单建树/注册/呈现模型）、`.Framework.Commands`（命令注册，ADR-0005）、`.Framework.Contributions`（贡献收集器与工具视图注册）、`.Framework.Settings`（设置注册与持久化，ADR-0006）、`.Framework.Windows`（窗口基类、命令面板与主题）、`.Framework.WindowManager`。类型均 public，例外：`Menus/MenuRegistration.cs` 的 `ReflectedMenuItemContribution` 与 `Commands/CommandRegistration.cs` 的 `ReflectedCommandContribution` 两个 attribute 扫描生成的贡献实现为 internal（见第 12、14 节）。

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
| `CreateShell` | `protected override AvaloniaObject CreateShell()` | `Container.Resolve<TWindow>()`（第 224 行）——主窗口经容器解析，支持构造注入 |
| `ConfigureViewModelLocator` | `protected override void ConfigureViewModelLocator()` | 约定式 ViewModel 定位（第 237-269 行），见下 |

### 私有启动序列成员（改行为时直接面对）

- `RunStartupSequenceAsync()`（第 68 行）：三阶段启动，见 common.md 状态流转。阶段 2 取 `moduleCatalog.Modules.ToList()` 快照后以 `for (var i = 0; i < total; i++)` 按下标推进（`total = modules.Count`）：每模块先 `Publish(new StartupProgress(StartupPhase.LoadingModules, module.ModuleName, i + 1, total))`（序号从 1 起），再 `await Task.Run(() => moduleManager.LoadModule(module.ModuleName))`（第 92 行）。
- `WaitForFailureActionAsync(IEventAggregator)`（第 121 行）：一次性订阅 `StartupFailureActionEvent`，返回 `true` = Continue。
- `ShowMainWindow()`（第 131 行）：先做两个模式匹配守卫——`MainWindow is not Window window` 或 `ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime` 时**静默 return**（不抛异常、不动作）；通过后依次 `lifetime.MainWindow = window` → `_windowManager?.ShowMainWindow()` → `_windowManager?.CloseWindowsExceptMain()`。
- `RegisterFrameworkServices`（第 146 行）/ `ResolveFrameworkServices`（第 175 行）：前者依次注册窗口管理器双接口单例、`ShellContributionCollector` 单例、`LayoutPersistence` 单例，随后**显式构造 `SettingsService` 并立即 `Load()`**（第 163-165 行，仿 windowManager 工厂注册模式，时机明确）注册为 `ISettingsService` 单例、`RegisterSettings` 注册 Framework 自身「常规/语言」设置项（第 168 行）、`ApplyLanguageSetting` 按已存语言设置应用 UI 区域性（第 170 行，必须先于一切模块 `RegisterTypes`——标题注册期经 `Language.Get` 解析定死，见第 16 节），末尾调后者；后者用 `Container.Resolve<IEventAggregator>()` 与 `Container.Resolve<IMainWindowManager>()` 把两个服务存入 `_eventAggregator`/`_windowManager` 私有字段。RegisterTypes 阶段 `Container` 已可用（`RegisterFrameworkServices` 首行 `IoC.Initialize(containerRegistry, Container)` 即以它为参），且 `IMainWindowManager` 单例刚在本方法前段注册，故可立即解析。

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
    public IReadOnlyList<string> ActivityBarItems { get; init; } = [];  // 顶部段有序 Id（钉住项不入列，ADR-0002）
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
| `MoveTab(string tabId, ToolViewPlacement targetBar, int index)`（:109，ADR-0002） | 跨 Bar 迁移/同 Bar 重排：从源 Bar 移除、按 index 插入目标。跨 Bar：目标面板强制 `Visible=true` 且激活该 tab（目标 ActivityBar → 选中该导航项、展开 SideBar 并置 `ContentFor`）；源面板拖空 → `Visible=false`；源面板活动 tab 被拖走 → 回退到其**前一个** tab（原首位取移除后首个）；源为 ActivityBar 且被拖走的是选中项 → 顶部段仍有项则改选中其前一项、SideBar 保持展开，顶部段拖空才取消选中并收起。同 Bar 为纯重排（index 按移除前列表计，`sourceIndex < index` 时内部减一修正），不改激活状态。tabId 不属于任何 Bar（如钉住项）或原地落放 → 拒绝返回 `this` |
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

带基础布局的窗口基类：内置 VS Code 式五区 shell（ActivityBar/SideBar/MainContent/AuxiliaryPanel/BottomPanel + 状态栏），**标题栏左侧的菜单栏**（全部菜单贡献建树生成，ADR-0001；宽松绑定 ViewModel 的 `MenuBarItems`），**以及顶部居中的命令面板**（`CommandPalette`，ADR-0005；宽松绑定 ViewModel 的 `Commands`，Ctrl+P 开关）。真实子类：`Modules/Workstation/MainWindow`（`MainWindow.axaml.cs:5`，菜单栏与命令面板已由基类内置，axaml 侧只保留应用级 chrome——标题、快捷键）。成员：

| 成员 | 签名 | 说明 |
|---|---|---|
| `PanelAlignmentProperty` | `public static readonly StyledProperty<PanelAlignment>`（:24） | 布局档位的依赖属性，**默认 `PanelAlignment.Center`**（= 历史布局）；是布局定义的唯一入口，与 ViewModel 双向绑定 |
| `PanelAlignment` | `public PanelAlignment PanelAlignment`（:79） | CLR 包装；写值触发 `OnPropertyChanged` → `UpdateLayoutTemplate`，**整体替换** `ContentTemplate`，不做动态调整 |
| `StyleKeyOverride` | `protected override Type StyleKeyOverride => typeof(UrsaWindow)`（:74） | 继承 UrsaWindow 的窗口主题（标题栏 chrome、模板与焦点行为） |
| 构造函数 | `protected FrameworkWindow()`（:31） | 依次：`Styles.Add(_theme)`（`_theme` 为 `FrameworkWindowTheme` 实例字段，:27）→ `_layoutHost`（ContentControl 布局宿主，:28）的 `Content` 经 `this[!DataContextProperty]` 绑定窗口 DataContext（:35，布局模板以 ViewModel 为绑定源，全部宽松绑定）→ **内置命令面板**（:36-44，ADR-0005）：`_palette[!ItemsSource]` 宽松绑定 `"Commands"`（:38），`Content = new Panel { _layoutHost, _palette }`（:39，面板叠在布局宿主之上，不动四份布局模板），注册 Ctrl+P KeyBinding（:40-44，`DelegateCommand(_palette.Open)` 直接开关）→ **内置菜单栏**：`LeftContent = new Menu { Classes = { "chrome-menu" }, VerticalAlignment.Center, ItemsSource 宽松绑定 "MenuBarItems" }`（:47-52）→ `DataTemplates.Add(new FuncDataTemplate<MenuItemViewModel>(...))`（:53，项模板入窗口 DataTemplates 而非主题资源，子菜单任意深度经模板查找递归复用）→ `UpdateLayoutTemplate()`（:54） |
| `BuildMenuItemHeader` | `private static Control`（:62） | 菜单项头部：水平 StackPanel（Spacing=6）+ 图标槽位 + 标题 `TextBlock`；`Icon` 非 null 渲染 14×14 `PathIcon`，为 null 时弹出层内的项仍渲染同尺寸空 `PathIcon` 占位（文本与有图标项对齐），标题栏顶层菜单（`IsTopLevel`）不预留槽位；图标前景色由 chrome-menu 样式接管 |
| `OnPropertyChanged` | `protected override void`（:85） | `e.Property == PanelAlignmentProperty` 时调 `UpdateLayoutTemplate()` |
| `UpdateLayoutTemplate` | `private void`（:94） | 枚举→资源键映射后 `_theme.TryGetResource(key, null, out var template)` 查找并强转 `IDataTemplate` 赋给 `_layoutHost.ContentTemplate`；**缺失抛 `InvalidOperationException($"布局模板资源缺失：{key}")`**（:106），窗口构造期即失败 |
| `RegisterCommandGestures` | `public void RegisterCommandGestures(IEnumerable<ICommandContribution>)`（:114，ADR-0005） | 为带 `Gesture` 的命令生成窗口级 KeyBinding：机制在 Framework、接线在 shell 模块（同 `LayoutPersistence` 惯例），shell 收集命令后调用一次；`KeyGesture.Parse` 抛 `FormatException` 的文本记 `Logger.Warning` 跳过 |

枚举→资源键映射（:96-102）：

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
| `ShellActivityBar` / `ShellSideBar` / `ShellMainContent` / `ShellAuxiliaryPanel` / `ShellBottomPanel` / `ShellStatusBar` | :27 / :59 / :75 / :87 / :142 / :198 | 六个共享部件 DataTemplate（三处可投放 Bar 用 ToolViewBar 承载；ActivityBar 底部段为 StackPanel（:41-53）＝钉住区 ItemsControl（`BottomNavigationItems`，当前无钉住项实例、可为空）+ shell 内置"设置"导航按钮（`Command={Binding OpenSettingsCommand}`，ADR-0006 决策 6），与顶部段以 `Grid RowDefinitions="*,Auto"` 分隔——不用 DockPanel bottom dock，见 pitfalls.md） |
| `WindowLayoutLeft` / `WindowLayoutRight` / `WindowLayoutCenter` / `WindowLayoutJustify` | :227 / :293 / :359 / :424 | 四份布局 DataTemplate：布局 Grid 列 `Auto,{Binding SideBarColumnWidth},*,{Binding AuxiliaryColumnWidth}`、行 `*,Auto`；差异为 BottomPanel 及分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`（见上表）；ActivityBar 恒 `RowSpan=2` 通高 |
| `{x:Type layout:ToolViewBar}` | :492 | ToolViewBar 的 ControlTheme（ADR-0002）：Background=Transparent 使整条带参与命中测试；模板为 Border + Panel(ItemsPresenter + `PART_InsertionLine` 拖拽占位线） |

样式（:513 起）：nav-item/panel-tab/GridSplitter/panel-collapse/region-title/placeholder/status-item 自 Modules/Workstation/MainWindow.axaml 迁入；`layout|ToolViewBar.drag-over`（:554，拖拽悬停整 Bar 高亮，ADR-0002）；4 个标题栏菜单样式（:611-628，ADR-0001 菜单栏内置配套）：`Menu.chrome-menu > MenuItem` 紧凑行高（MinHeight=30、Padding=10,0，:611-614）、`Popup#PART_Popup` VerticalOffset=-8 让弹出层贴合标题栏下缘（:616-619）、`Menu.chrome-menu MenuItem` 的 `ItemsSource={Binding Children}`/`Command={Binding Command}`/`AutomationProperties.Name={Binding Title}` 样式绑定（:621-624，叶子 Children 为空、节点 Command 为 null，均无副作用）、`PathIcon` 前景色 SemiColorText1（:626-628）。**菜单项的内容模板不在本主题内**——无 x:Key 的 DataTemplate 不能放 `Styles.Resources`（AVLN3000），故由 `FrameworkWindow` 构造时在窗口 `DataTemplates` 代码注册（见第 4 节）。分隔条改用 `layout:PanelResizer` 的 `Target`+`ResizeCommand` 声明式绑定（如 :240-252；axaml 以 `xmlns:layout="clr-namespace:DigitalWorkstation.Core.Framework.Layout"` 引入，:3）。**所有绑定为宽松反射绑定**——Framework 不引用具体 ViewModel 类型。面板对齐的切换入口不在本主题内（在视图菜单，见 Modules/Workstation 的 `Menus/ViewAlignmentMenus.cs`）。

命令面板样式（:630-667，ADR-0005 配套）：`windows|CommandPalette` 浮层外观（SemiColorBackground1 底 + 边框 + 圆角 6 + 阴影，宽 600、顶部居中、上缘距 48）、`TextBox.command-input` 透明无边框、`ListBox.command-list` 透明底与条目圆角、`TextBlock.gesture` 右侧快捷键文本（SemiColorText2）、`PathIcon.command-icon` 条目左侧图标槽位（12×12、SemiColorText2、右间距 8、垂直居中；始终渲染，`IconPath` 为 null 时为空占位，文本对齐）。

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

### 工具视图拖拽控件（ToolViewButton / ToolViewBar / ToolViewMove / ToolViewDragSession，均位于 Layout/，ADR-0002）

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


## 7. `ShellLayoutDto` 族与 `LayoutPersistence`（均位于 Layout/，ADR-0002）

```csharp
public sealed record ShellLayoutDto          // ShellLayoutDto.cs:10，const CurrentVersion = 1（:15）
public sealed record ToolViewPlacementEntry  // ShellLayoutDto.cs:41
public sealed record SideBarLayoutDto        // ShellLayoutDto.cs:54
public sealed record PanelLayoutDto          // ShellLayoutDto.cs:69
public sealed record BottomPanelLayoutDto    // ShellLayoutDto.cs:81
public sealed class LayoutPersistence        // LayoutPersistence.cs:12
```

落盘格式（`ShellLayoutDto` 族）独立于 `ShellLayoutState`——状态机只管流转语义，不管序列化兼容（文件注释自述）。`ShellLayoutDto` 成员：`Version`（:17，`Load` 对不识别版本整份丢弃）、`PanelAlignment`（:22，对齐档位由 `FrameworkWindow` 依赖属性持有、不在状态机内，一并持久化）、`Placements: Dictionary<string, ToolViewPlacementEntry>`（:29，可移动工具视图 Id → `{ Bar, Index }`，恢复时优先于 attribute 的 `Default`；**钉住项恒在 ActivityBar 底部段、不入此表**；无对应贡献的孤儿条目丢弃，无条目的新工具视图落回 `Default`）、三个可空子 DTO `SideBar`/`AuxiliaryPanel`/`BottomPanel`（:31-35，各记显隐/尺寸/选中项或活动 tab；为 null 表示该区域无持久化数据，恢复时保持默认）。序列化选项（`LayoutPersistence.cs:23-28`）：`WriteIndented` + camelCase 属性名 + `JsonStringEnumConverter`——枚举落成 `"Center"`/`"BottomPanel"` 形态字符串。

`LayoutPersistence` 是 `%AppData%/Digital.Workstation/layout.json` 的读/写/删，注册为单例（`FrameworkApplication.cs:159`；注释自述"机制在 Framework、接线在 shell 模块"）：

| 成员 | 签名/位置 | 语义 |
|---|---|---|
| `FilePath` | `public static readonly string`（:17-19） | 布局配置文件路径：`%AppData%/Digital.Workstation/layout.json` |
| `Load` | `public ShellLayoutDto? Load()`（:37） | 文件缺失返回 null（首次启动常态，**无日志**）；内容为空记 Warning 返回 null（:47-51）；`Version ≠ CurrentVersion` 记 Warning 返回 null（:53-58）；反序列化/IO 等一切异常 `catch (Exception)` 记 Warning 返回 null（:62-67） |
| `ScheduleSave` | `public void ScheduleSave(ShellLayoutDto)`（:73） | 防抖写：`lock (_gate)` 内记下 `_pending` 并把 `System.Threading.Timer` 重置到 500ms（`DebounceMilliseconds`，:21）后单次触发——500ms 内的连续调用只落盘最后一份布局 |
| `Delete` | `public void Delete()`（:86） | 删除布局配置文件（重置布局用）：先在锁内作废未落盘的防抖保存（清 `_pending`、停 Timer，:88-92）再 `File.Delete`——**顺序不可换**，否则在途的防抖回调会把文件重建；删除失败记 Warning（:98-101） |
| `Flush` | `private void Flush(object?)`（:104） | Timer 回调：锁内取出并清空 `_pending`（null 直接返回），`Directory.CreateDirectory` + 序列化写盘（:121-122）；注释自述"Timer 回调里的异常无人处理会拖垮进程"（:118），故 `catch (Exception)` 就地吞掉记 Warning（:124-127） |

容错矩阵（全部失败路径只记 `Logger.Warning`、来源标记 `LayoutPersistence`，不打断应用）：

| 场景 | 行为 |
|---|---|
| 文件缺失 | `Load` 返回 null，无日志（首次启动常态） |
| 内容为空 / 版本不识别 | `Load` 记 Warning 返回 null |
| JSON 损坏、枚举字符串非法等反序列化失败 | `Load` 记 Warning 返回 null（整份文件丢弃，回默认布局——容错设计不是 bug） |
| 写盘失败（`Flush`） | 记 Warning，静默放弃本次保存 |
| 删文件失败（`Delete`） | 记 Warning；pending 保存已作废 |

**典型消费**（真实代码）：`Modules/Workstation/MainWindowViewModel.cs:41-42` 构造注入；`EnsureContributionsLoaded` 里 `_toolViews = _collector.GetToolViews(); LoadToolViews(_persistence.Load());`（:178-179）——`LoadToolViews`（:200）按「钉住项恒落 ActivityBar 底部段、可移动项配置优先默认兜底」分派三处 Bar，layout 非 null 再调 `RestoreLayout`（:242）恢复显隐/尺寸（clamp 到各区域 record 常量）/选中项/对齐档位，layout 为 null 即全默认（重置布局复用此路径）；`ResetLayout`（:503，订阅 `ResetLayoutEvent`）先 `_persistence.Delete()` 再 `LoadToolViews(null)` 重建默认；`CaptureLayout`（:527）+ `ScheduleSave()`（:578）在 `SelectActivity`/`ActivateAuxTab`/`ActivateBottomTab`/`SetPanelAlignment`/`ResizePanel`/`TogglePanel`/`MoveTab`（拖拽落放，:440）末尾调度防抖保存。事件契约 `ResetLayoutEvent`（无负载）在 Core/Models/Events，由视图菜单「重置布局」项发布（`Modules/Workstation/Menus/ViewLayoutMenus.cs`）。

## 8. `ShellContributionCollector`（Contributions/ShellContributionCollector.cs:12）

```csharp
public class ShellContributionCollector(IContainerProvider containerProvider)
```

主构造注入 Prism `IContainerProvider`。注册为单例（`FrameworkApplication.cs:156`）。七个收集方法；菜单方法自 ADR-0001 起不过滤不排序（建树器负责分组排序），工具视图自 ADR-0002 起不再按定位枚举过滤（三处 Bar 与钉住区的分派由消费方按 `Placement`/`AllowMove` 决定）：

| 方法 | 过滤 | 排序 |
|---|---|---|
| `GetToolViews()`（:18） | 过滤 DryIoc 零注册幽灵实例（默认构造、`Id=null` 的条目，:23 的 `Where`；三处 Bar 的分派由消费方决定，ADR-0002） | `Order` 升序 |
| `GetMainViews()`（:27） | 无（全部） | 无（保持容器解析顺序） |
| `GetMenuItems()`（:34，**无参数**） | 无（路径/分组模型下不再按定位枚举过滤） | 无（分组排序建树由 `MenuTreeBuilder` 负责，见第 11 节） |
| `GetStatusBarItems()`（:63） | 无 | `Order` 升序 |
| `GetCommands()`（:42，ADR-0005） | Id 冲突去重：保留先注册者，后者丢弃并记 `Logger.Warning` | `Order` 升序，同 Order 按解析后 `Title` 字典序（Ordinal） |
| `GetSettingGroups()`（:74，ADR-0006） | 按名称全局合并：同名 `SettingGroupAttribute` 多处声明时位次取最小；仅被设置项引用而无声明的隐式分组补出、`Order` 视为 0 | `Order` 升序，同 Order 按名称键字典序（Ordinal） |
| `GetSettingItems()`（:99，ADR-0006） | Id 冲突去重：保留先注册者，后者丢弃并记 `Logger.Warning`（同 `GetCommands`） | `Order` 升序，同 Order 按名称键字典序（Ordinal） |

返回类型均为 `IReadOnlyList<T>`（快照数组）。贡献类型中 `ToolViewContribution`（sealed class，由 `RegisterToolViews` 扫描 `[ToolView]` 生成，见第 13 节）、`IMainViewContribution`、`IStatusBarItemContribution` 与枚举 `ToolViewPlacement` 在 Core/Abstractions 的 `Contributions/` 目录；`IMenuItemContribution` 在 `Menus/` 目录（形状已按 ADR-0001 改为路径/分组模型）；`ICommandContribution` 在 `Commands/` 目录（ADR-0005 扁平模型）；`SettingGroupContribution`/`SettingItemContribution` 在 `Settings/` 目录（ADR-0006，见 Abstractions 文档）。

**典型消费**：`Modules/Workstation/MainWindowViewModel.cs:41-42` 构造注入 `ShellContributionCollector`，`EnsureContributionsLoaded()`（:170）先调一次 `GetToolViews()`（:178）存 `_toolViews`，再 `LoadToolViews(_persistence.Load())`（:179）把分派与持久化恢复交给 `LoadToolViews`（:200-236：钉住项恒落 ActivityBar 底部段，可移动项持久化 placements 优先、attribute `Default` 兜底），并收集主视图与状态栏项；菜单走 `MenuTreeBuilder.Build(_collector.GetMenuItems())`（:184）建树后转为菜单 ViewModel。

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

纯函数、无状态建树器：把扁平贡献列表构建为分组排序好的顶层菜单列表，标题（路径段与条目）在建树时经 `Language.Get` 一次性解析（:59、:74、:98）。规则（ADR-0001）：

- **路径切分与跳过**：`Path.Split('/', TrimEntries)`（:23），含空段记 `Logger.Warning` 跳过该条目（:24-29）。
- **单段路径**（顶层菜单直下条目）：`Order` 经 `NodeAccum.MergeNodeOrder` 声明顶层菜单位次（多处声明取最小，:126-135）；`Group`/`GroupOrder` 描述条目分组（:41-47）。
- **多段路径**：`Group`/`GroupOrder`/`NodeOrder` 经 `MergePlacement` 声明末端子菜单节点的位次（多处声明：`NodeOrder`/`GroupOrder` 取最小，`Group` 取 `GroupOrder` 最小声明的组名、同值取组名 Ordinal 小者，:141-159）；条目进末端菜单默认组（:48-54）。
- **顶层排序**：只按 `(NodeOrder, Language.Get(段) Ordinal)` 排序，不分组不插分隔线（:57-61）。
- **子菜单内排序与分组**（`Emit`，:64）：条目与递归子节点统一按 `(GroupOrder, null 组优先, Group Ordinal, Order, Title Ordinal)` 排序（:77-83）；相邻条目组变化处插 `MenuTreeSeparator.Instance`（:87-96），天然不产生开头/结尾/连续分隔线。

内部累积结构（建树期临时对象，不外泄）：`LeafAccum` record（:101，一条叶子的分组/位次/标题/图标/命令）与 `NodeAccum` class（:104，路径段节点：子节点字典 `Children`（Ordinal 键）、条目列表 `Items`、合并位次的 `MergeNodeOrder`/`MergePlacement`）。

## 12. `MenuRegistration.RegisterMenus`（Menus/MenuRegistration.cs:14）

```csharp
public static void RegisterMenus(this IContainerRegistry registry, Assembly assembly); // :20
```

attribute 菜单注册扩展（ADR-0001）。模块在自身 `RegisterTypes` 中调用并传入本模块程序集，**不做全局程序集扫描**；扫描只在注册时发生一次。规则（:21-68）：

1. 遍历 `assembly.DefinedTypes`，取标注 `MenuGroupAttribute` 的类（:22-28）；类路径含空段记 `Logger.Warning` 整类跳过（:30-35）。
2. 取该类 `Public | Instance | DeclaredOnly` 方法中标注 `MenuItemAttribute` 者（:37-40）；带参或返回值非 `void`/`Task` 的记 `Logger.Warning` 跳过（:44-51）——因此**静态方法与泛型方法**（非实例/含参）天然进不了候选或被签名校验挡下。
3. 类内无合法方法则整体跳过（:55-58）；否则菜单类本体 `RegisterSingleton(menuType)`（:61），每个合法方法注册一个 `IMenuItemContribution` 工厂（:62-66），工厂内 `provider.Resolve(menuType)` 取菜单类 singleton 实例（建树时经容器解析一次，之后复用）。

### `ReflectedMenuItemContribution`（internal，:75）

由 `RegisterMenus` 生成的 `IMenuItemContribution` 实现。构造期（:80-93）把 attribute 元数据落成契约属性：`Title = Language.Get(item.Title)`（注册时即按 UI 区域性解析）、`IconPath = item.Icon`、`Path`/`Group`/`GroupOrder` 取自类级 `MenuGroupAttribute`、`NodeOrder = group.Order`、`Order = item.Order`、`Command = new DelegateCommand(Execute)`。点击时 `Execute` fire-and-forget 调 `ExecuteAsync`（:111-114）：反射 `_method.Invoke(_instance, null)`，返回 `Task` 则 `await`（:120-123）；异常解包 `TargetInvocationException` 后 `Logger.Error` 记录，**不抛出**（:125-132）。

## 13. `ToolViewRegistration.RegisterToolViews`（Contributions/ToolViewRegistration.cs:14）

```csharp
public static void RegisterToolViews(this IContainerRegistry registry, Assembly assembly); // :20
```

attribute 工具视图注册扩展（ADR-0002），与 `RegisterMenus` 同构。模块在自身 `RegisterTypes` 中调用并传入本模块程序集，**不做全局程序集扫描**；扫描只在注册时发生一次。规则（:21-59）：

1. 遍历 `assembly.DefinedTypes`，取标注 `ToolViewAttribute` 的类（:23-29）。
2. 类非可实例化 `Control`（abstract 或非 `Control` 派生）记 `Logger.Warning` 跳过（:31-36）。
3. `Id` 在**本程序集内**重复（`seenIds` 局部 HashSet，:22）记 `Logger.Warning` 跳过（:38-44）；跨程序集重复不在此处检测。
4. 合法者：`registry.Register(viewType)` 注册 View 类型本身（:47，供激活时按 `ViewType` 解析），并把 attribute 元数据落成 `ToolViewContribution` 后 `RegisterSingleton(typeof(ToolViewContribution), _ => metadata)`（:48-58）；`Title` 在扫描时经 `Language.Get(attribute.TitleKey)` 解析（:51），`Placement` 取 attribute 的 `Default`（:54）。

真实调用点：`Modules/Workstation/WorkstationApplication.cs:27`、`Modules/DashBoard/DashBoardModule.cs:14`。

## 14. `CommandRegistration.RegisterCommands`（Commands/CommandRegistration.cs:13，ADR-0005）

```csharp
public static void RegisterCommands(this IContainerRegistry registry, Assembly assembly); // :19
```

attribute 命令注册扩展，与 `RegisterMenus` 同构但**免类级 attribute**。模块在自身 `RegisterTypes` 中调用并传入本模块程序集，**不做全局程序集扫描**；扫描只在注册时发生一次。规则（:21-55）：

1. 遍历 `assembly.DefinedTypes`，取 `Public | Instance | DeclaredOnly` 方法中标注 `CommandAttribute` 者（:24-26）——任何类的方法都可成为命令，类仅作 DI 宿主。
2. 带参或返回值非 `void`/`Task` 的记 `Logger.Warning` 跳过（:30-36）——静态方法与泛型方法天然进不了候选或被签名校验挡下。
3. 类内无合法方法则整体跳过（:39-42）；否则宿主类本体 `RegisterSingleton(hostType)`（:45），每个合法方法注册一个 `ICommandContribution` 工厂（:46-50），工厂内 `provider.Resolve(hostType)` 取宿主类 singleton 实例（收集时经容器解析一次，之后复用）。

### `ReflectedCommandContribution`（internal，:60）

由 `RegisterCommands` 生成的 `ICommandContribution` 实现。构造期（:65-75）把 attribute 元数据落成契约属性：`Id = attribute.Id ?? $"{method.DeclaringType?.FullName}.{method.Name}"`（默认「声明类全名.方法名」）、`Title = Language.Get(attribute.Title)`（收集时按 UI 区域性解析）、`Gesture`/`IconPath`/`Order` 透传、`Command = new DelegateCommand(Execute)`。执行路径与菜单完全同构：`Execute` fire-and-forget 调 `ExecuteAsync`（:86-89）：反射调用，返回 `Task` 则 `await`；异常解包 `TargetInvocationException` 后 `Logger.Error` 记录，**不抛出**（:91-104）。

## 15. `CommandPalette`（Windows/CommandPalette.cs:18，ADR-0005）

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

## 16. 设置管线（Settings/，ADR-0006）

### `SettingRegistration.RegisterSettings`（Settings/SettingRegistration.cs:13）

```csharp
public static void RegisterSettings(this IContainerRegistry registry, Assembly assembly); // :19
```

attribute 设置注册扩展（ADR-0006 决策 1），与 `RegisterMenus`/`RegisterCommands` 同构。模块在自身 `RegisterTypes` 中调用并传入本模块程序集，**不做全局程序集扫描**；扫描只在注册时发生一次。规则（:21-66）：

1. 遍历 `assembly.DefinedTypes`（:21），类上每条 `SettingGroupAttribute` 声明注册一个 `SettingGroupContribution` 单例（:23-27，attribute 允许 `AllowMultiple`；同名分组的合并与位次取最小发生在收集侧，见第 8 节 `GetSettingGroups`）。
2. 取该类 `Public | Static | DeclaredOnly` 属性中标注 `SettingItemAttribute` 者（:29-36）；无 getter（:38-43）或 `DefaultValue` 非空且类型与属性类型不匹配（:45-51）记 `Logger.Warning` 跳过。属性只是声明锚点——扫描不读属性值，读写一律走 `ISettingsService`。
3. 每个合法属性注册一个 `SettingItemContribution` 单例（:53-63）：`Id = attribute.Id ?? "声明类全名.属性名"`（:55，仿命令默认 Id 规则）、`ValueType` 取属性类型（:58）、`Group`/`Name`/`DefaultValue`/`Order`/`RequiresRestart` 透传。

真实调用点：`FrameworkApplication.cs:168`（Framework 自身「常规/语言」设置项）。

### `SettingsService`（Settings/SettingsService.cs:17）

```csharp
public sealed class SettingsService(IEventAggregator eventAggregator, IContainerProvider containerProvider)
    : ISettingsService
```

`ISettingsService` 实现（ADR-0006 决策 3/4），`%AppData%/Digital.Workstation/settings.json` 的读/防抖写。注册方式特殊：**显式构造实例、立即 `Load()`、以工厂注册**（`FrameworkApplication.cs:163-165`，仿 windowManager 模式），使启动时一次性加载的时机明确。成员：

| 成员 | 签名/位置 | 语义 |
|---|---|---|
| `FilePath` | `public static readonly string`（:23-25） | 设置配置文件路径：`%AppData%/Digital.Workstation/settings.json` |
| `Load` | `public void Load()`（:64） | 启动时一次性加载入内存镜像 `_values`，并把载入内容原样复制为 `_sessionStartValues`（:86，「重启后生效」判定的基准）；文件缺失静默返回（:68-71，首次启动常态）；内容为空（:75-79）或一切异常 `catch (Exception)`（:89-93）记 Warning 按无修改处理——容错仿 `LayoutPersistence` |
| `Get<T>` | `public T? Get<T>(string settingId)`（:97） | 纯内存读：`_values` 命中则按 `T` 反序列化返回，单项失败记 Warning 后**逐项**回退默认值（:103-112，与 layout.json 整份丢弃不同）；未修改时经 `FindContribution` 回退声明的 `DefaultValue`（:116-123）；未声明记 Warning 返回 `default` |
| `Set<T>` | `public void Set<T>(string settingId, T value)`（:126） | 未声明记 Warning 但仍写入（:129-132）；锁内更新 `_values`、把 500ms 防抖 Timer 重置到单次触发并 `TrackPendingRestart` 维护重启判定（:134-141），锁外广播 `SettingChangedEvent`（:143） |
| `IsPendingRestart` | `public bool IsPendingRestart(string settingId)`（:146） | 锁内查 `_pendingRestartIds`（ADR-0006 决策 7「重启后生效」判定；服务不感知 RequiresRestart，过滤在调用方） |
| `TrackPendingRestart` | `private void`（:158） | 重启判定维护（调用方须持 `_gate`）：当前值与启动时生效值经 `JsonElement.DeepEquals` 比较——快照命中取快照值，快照不含则以声明默认值序列化结果为基准（:160-163）；偏离记入 `_pendingRestartIds`、改回启动值即移出 |
| `FindContribution` | `private SettingItemContribution?`（:179） | 声明默认值缓存：按 Id 缓存，未命中重新枚举容器中的全部声明刷新缓存——模块在启动序列阶段 2 才注册各自设置项，缓存必须允许后到的声明（:42-46 注释） |
| `FlushPending` | `public void FlushPending()`（:198） | 立即落盘：锁内停掉在途防抖 Timer，锁外 `Save(TakeSnapshot())` 同步写盘。「立即重启」启动新进程前调用——防抖有 500ms 窗口，不强制落盘新进程可能读到旧配置 |
| `Flush` | `private void Flush(object?)`（:208） | Timer 回调：`Save(TakeSnapshot())` |
| `TakeSnapshot` / `Save` | `private`（:213 / :222） | 锁内快照 `_values` / `Directory.CreateDirectory` + 序列化写盘；`Save` 对一切异常 `catch (Exception)` 就地吞掉记 Warning（:221 注释，纪律同 `LayoutPersistence.Flush`） |

内部状态：`_values`（:40，落盘内容内存镜像）、`_declared`（:46，声明惰性缓存）、`_sessionStartValues`（:52，进程启动时生效值快照）、`_pendingRestartIds`（:57，已偏离启动值的项）、`_timer`（:59，防抖）；全部经 `_gate`（:35）保护。

序列化选项（:29-33）：`WriteIndented` + `JsonStringEnumConverter`——枚举落盘为 `JsonStringEnumMemberName` 指定的字符串（`UiLanguage` 为 `"zh-CN"`/`"en-US"`）。落盘文件只存**用户已修改的值**（`_values` 的镜像），默认值不进存储层。

### `UiLanguage` 与 `UiLanguageExtensions`（Settings/UiLanguage.cs:12）

```csharp
public enum UiLanguage { [JsonStringEnumMemberName("zh-CN")] ZhCN, [JsonStringEnumMemberName("en-US")] EnUS } // :12-25
public static CultureInfo ToCultureInfo(this UiLanguage language);                                            // :32
```

Framework 预置「常规/语言」设置项的值类型（ADR-0006 决策 9）。成员的 `JsonStringEnumMemberName` 值即对应 `CultureInfo` 名称——settings.json 落盘值与区域性名称同源；`ToCultureInfo`（:32-37）反射读该 attribute 值作为 `CultureInfo.GetCultureInfo` 的名称。

### `GeneralSettings`（Settings/GeneralSettings.cs:10）

```csharp
[SettingGroup("SettingsGeneralGroupName", Order = 0)]
public static class GeneralSettings
```

Framework 预置设置项的声明类（ADR-0006 决策 9）。成员：`public static readonly string LanguageSettingId`（:15，默认规则「声明类全名.属性名」，供启动序列等消费方读写）；`[SettingItem("SettingsGeneralGroupName", "SettingsLanguageName", DefaultValue = UiLanguage.ZhCN, RequiresRestart = true)] public static UiLanguage Language`（:20-22）——属性体只是声明锚点不会被读取，读写一律经 `ISettingsService`。

## 17. `ApplicationRestarter`（ApplicationRestarter.cs:14，ADR-0006 决策 7）

```csharp
public static class ApplicationRestarter { public static void Restart(); } // :19
```

「立即重启」：设置页重启横幅按钮的动作（真实调用点 `Modules/Settings/ViewModels/SettingsPageViewModel.cs` 的 `RestartNowCommand`）。顺序固定三步：① 经 `IoC.Provider` 解析 `ISettingsService`，是 `SettingsService` 则 `FlushPending()` 强制落盘（:21-25，防抖 500ms 窗口内重启会让新进程读到旧配置）；② `Environment.ProcessPath` 取当前可执行文件路径（为 null 记 `Logger.Error` 中止，:27-32），以原始命令行参数（`Environment.GetCommandLineArgs().Skip(1)`）`Process.Start` 启动新进程（:35）；③ `IClassicDesktopStyleApplicationLifetime.Shutdown()` 走正常桌面生命周期退出当前进程（:36，与启动失败退出同路径）。重启前记一行 `Logger.Information`（:34）。新进程会再次经过启动台，属预期行为。


## 容器注册清单（对外可解析的服务）

`RegisterFrameworkServices`（FrameworkApplication.cs:146-173）注册：

| 服务 | 注册方式 | 实现 |
|---|---|---|
| `IMainWindowManager` | Singleton | 同一 `FrameworkWindowManager` 实例 |
| `IWindowManager` | Singleton | 同一 `FrameworkWindowManager` 实例 |
| `ShellContributionCollector` | Singleton | 自身 |
| `LayoutPersistence` | Singleton | 自身（ADR-0002；机制在 Framework、接线在 shell 模块） |
| `ISettingsService` | Singleton（工厂） | 显式构造的 `SettingsService` 实例，注册前已 `Load()`（ADR-0006 决策 3/4） |
| `IoC.Registry` / `IoC.Provider` | 静态初始化 | `IoC.Initialize(containerRegistry, Container)`（Common 模块） |
