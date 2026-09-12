# Framework — 文件结构与功能

目录树（相对 `Core/Framework/`，省略 `obj/` 与 `Output/` 构建产物）：

```
Core/Framework/
├── Framework.csproj                       项目文件：net10.0；引用 Abstractions/Common/Models/UIPackage/Resource 五项目与 Avalonia/Prism/Ursa 相关包
├── FrameworkApplication.cs                应用入口基类与启动序列（含设置服务注册与启动时语言应用，ADR-0006）
├── ApplicationRestarter.cs                「立即重启」（ADR-0006 决策 7）：强制落盘设置 → 启动新进程 → 正常生命周期退出当前进程
├── Layout/                                  命名空间 DigitalWorkstation.Core.Framework.Layout
│   ├── ShellLayoutState.cs                布局状态根 record + 全部转换方法
│   ├── SideBarState.cs                    SideBar 区域状态 record
│   ├── AuxiliaryPanelState.cs             AuxiliaryPanel 区域状态 record
│   ├── BottomPanelState.cs                BottomPanel 区域状态 record
│   ├── MainContentState.cs                MainContent 区域状态 record
│   ├── ShellLayoutDto.cs                  布局持久化落盘 DTO record 族（独立于 ShellLayoutState，ADR-0002）
│   ├── LayoutPersistence.cs               布局持久化服务：layout.json 读/防抖写/删，全路径容错只记日志
│   ├── PanelResizeTarget.cs               可调尺寸区域枚举
│   ├── PanelAlignment.cs                  面板对齐枚举（FrameworkWindow 基础布局四档）
│   ├── PanelResize.cs                     分隔条命令参数 record struct
│   ├── SetPanelAlignmentEvent.cs          面板对齐切换事件契约（PubSubEvent<PanelAlignment>）
│   ├── PanelResizer.cs                    面板分隔条（GridSplitter 改造，自 Modules/Workstation 迁入）
│   ├── ToolViewMove.cs                    工具视图拖拽落点参数 record（TabId/TargetBar/Index，ADR-0002）
│   ├── ToolViewDragSession.cs             工具视图拖拽会话：DataFormat + IsActive 静态信号（拖拽期间显露隐藏面板）
│   ├── ToolViewButton.cs                  可拖拽工具视图按钮（tab 头/导航项）：PointerPressed+位移阈值 → DoDragDropAsync
│   └── ToolViewBar.cs                     工具视图 Bar 投放目标：DragOver 算插入序号并移动占位线，Drop 执行 MoveCommand
├── Menus/                                   命名空间 DigitalWorkstation.Core.Framework.Menus
│   ├── MenuItemViewModel.cs               菜单项呈现模型（Title/Icon?/Command?/IsTopLevel/Children + FromSubmenu 递归转换，自 Modules/Workstation 迁入）
│   ├── MenuTreeEntry.cs                   菜单树条目 record 族（叶子/子菜单/分隔线单例）
│   ├── MenuTreeBuilder.cs                 菜单建树器（纯函数：路径切分、分组排序、插分隔线，ADR-0001）
│   └── MenuRegistration.cs                attribute 菜单注册扩展 + internal 反射贡献实现
├── Commands/                                命名空间 DigitalWorkstation.Core.Framework.Commands（ADR-0005）
│   └── CommandRegistration.cs             attribute 命令注册扩展（免类级 attribute）+ internal 反射贡献实现
├── Contributions/                           命名空间 DigitalWorkstation.Core.Framework.Contributions
│   ├── ShellContributionCollector.cs      shell 贡献收集器（含设置分组/设置项收集，ADR-0006）
│   └── ToolViewRegistration.cs            attribute 工具视图注册扩展（ADR-0002）
├── Settings/                                命名空间 DigitalWorkstation.Core.Framework.Settings（ADR-0006）
│   ├── SettingRegistration.cs             attribute 设置注册扩展（与 RegisterMenus/RegisterCommands 同构）
│   ├── SettingsService.cs                 ISettingsService 实现：settings.json 启动一次加载、内存读、防抖落盘、事件广播、「重启后生效」判定（IsPendingRestart）与立即落盘（FlushPending）
│   ├── UiLanguage.cs                      界面语言枚举（zh-CN/en-US）+ ToCultureInfo 扩展
│   └── GeneralSettings.cs                 Framework 预置「常规/语言」设置项声明类
├── Windows/                                 命名空间 DigitalWorkstation.Core.Framework.Windows（窗口基类与主题）
│   ├── FrameworkWindow.cs                 带基础布局的窗口基类（内置 VS Code 式五区 shell + 标题栏菜单栏 + 命令面板浮层与 Ctrl+P）
│   ├── CommandPalette.cs                  命令面板控件（ADR-0005）：自包含检索浮层（过滤/键盘导航/MRU 内存置顶），ItemsSource 宽松绑定 Commands
│   ├── FrameworkWindowTheme.axaml         基础布局主题资源（四份布局模板 + 共享部件模板 + shell 样式 + 菜单样式 + 命令面板样式；ActivityBar 底部段 Grid(*,Auto) 分隔、StackPanel 承载钉住区 + 内置"设置"导航按钮；PanelResizer 经 xmlns:layout 引用）
│   └── FrameworkWindowTheme.cs            主题加载器（StyleInclude 强制加载，BaseUri 指向 Windows/ 目录）
└── WindowManager/
    └── FrameworkWindowManager.cs          IWindowManager + IMainWindowManager 实现
```

## 各文件功能

### `Framework.csproj`
`net10.0`、`ImplicitUsings`+`Nullable` 开启。ProjectReference：`..\Abstractions`、`..\Common`、`..\Models`、`..\UIPackage`、`..\Resource`（第 10-14 行）。PackageReference：`Avalonia.Desktop`/`Avalonia.Fonts.Inter`/`Avalonia.Themes.Fluent` 11.3.20、`AvaloniaUI.DiagnosticsSupport` 2.1.1、`CommunityToolkit.Mvvm` 8.4.0、`Irihi.Ursa` 1.15.1（第 18-23 行，FrameworkWindow 直接继承 UrsaWindow）。

### `FrameworkApplication.cs`
定义 `public abstract class FrameworkApplication<TWindow> : PrismApplication where TWindow : Window`（第 20 行，命名空间 `DigitalWorkstation.Core.Framework`）。关键入口：
- `Initialize()`（第 25 行）：主题装载（Dark + `WorkstationTheme` + `VSCodePalette.ApplyTo`）。
- `OnFrameworkInitializationCompleted()`（第 39 行）→ `RunStartupSequenceAsync()`（第 68 行）：三阶段启动序列（ADR-0004）。
- `OnInitialized()`（第 48 行）、`InitializeModules()`（第 55 行）：两个故意的空覆盖。
- `CreateSplashWindow()`（第 62 行，abstract）/`RegisterCustomService()`（第 215 行，virtual）：子类扩展点。
- `RegisterTypes()`（第 189 行）→ `RegisterFrameworkServices`（第 146 行）：`IoC.Initialize`、窗口管理器双接口单例、`ShellContributionCollector` 单例、`LayoutPersistence` 单例（机制在 Framework、接线在 shell 模块，ADR-0002）；随后显式构造 `SettingsService` 并立即 `Load()`、注册 `ISettingsService` 单例（:163-165）、`RegisterSettings` 注册 Framework 自身「常规/语言」设置项（:168）、`ApplyLanguageSetting`（:200，私有静态）按已存语言设 `DefaultThreadCurrentCulture/UICulture` 与 `Current`——必须先于一切模块 `RegisterTypes`（ADR-0006 决策 7）。
- `ConfigureViewModelLocator()`（第 237 行）：约定式 ViewModel 定位解析器。

### `ApplicationRestarter.cs`
`public static class ApplicationRestarter`（第 14 行，命名空间 `DigitalWorkstation.Core.Framework`，ADR-0006 决策 7）：`Restart()`（:19）——① 经 `IoC.Provider` 解析 `ISettingsService`，是 `SettingsService` 则 `FlushPending()` 强制落盘（防抖 500ms 窗口内重启会让新进程读到旧配置）；② `Environment.ProcessPath` + 原始命令行参数 `Process.Start` 启动新进程（路径取不到记 `Logger.Error` 中止）；③ `IClassicDesktopStyleApplicationLifetime.Shutdown()` 退出当前进程（与启动失败退出同路径）。真实调用点：设置页重启横幅按钮（`Modules/Settings` 的 `RestartNowCommand`）。

### `Layout/ShellLayoutState.cs`
`public sealed record ShellLayoutState`（第 9 行）：`SelectedActivity`（:14）与 `ActivityBarItems`（:20，ActivityBar 顶部段有序 Id 列表，钉住项不入列，ADR-0002）+ 五个区域状态属性 + `static Initial`（:30）。转换方法：`SelectActivity`（:36）、`ToggleSideBar`（:53）、`ToggleAuxiliaryPanel`（:61）、`ToggleBottomPanel`（:69）、`ActivateAuxTab`（:77）、`ActivateBottomTab`（:90）、`MoveTab`（:109，跨 Bar 迁移/同 Bar 重排）、`OpenMainView`（:236）、`Resize`（:244）、私有 `Clamp`（:275）。注释自述："原型验证过的 reducer 的正式实现"。

### `Layout/SideBarState.cs`
`public sealed record SideBarState`（第 6 行）：`const MinWidth=120 / MaxWidth=480`；`Visible`（默认 false）、`Width=240`、`ContentFor`（string?，收起时保留导航项 Id）。

### `Layout/AuxiliaryPanelState.cs`
`public sealed record AuxiliaryPanelState`（第 6 行）：`const MinWidth=120 / MaxWidth=480`；`Visible=true`、`Width=280`、`Tabs=[]`、`ActiveTab`（string?）。

### `Layout/BottomPanelState.cs`
`public sealed record BottomPanelState`（第 6 行）：`const MinHeight=80 / MaxHeight=480`；`Visible=true`、`Height=160`、`Tabs=[]`、`ActiveTab`（string?）。

### `Layout/MainContentState.cs`
`public sealed record MainContentState`（第 6 行）：仅 `ActiveView`（string?），单视图切换。

### `Layout/ShellLayoutDto.cs`
布局持久化 DTO record 族（ADR-0002）：layout.json 落盘的专用格式，独立于 `ShellLayoutState`（状态机只管流转语义，不管序列化兼容）。`ShellLayoutDto`（第 10 行）：`const CurrentVersion=1`（:15）与 `Version`（:17，不识别的文件由 LayoutPersistence 整体丢弃）、`PanelAlignment`（:22，对齐档位不在状态机内、由 FrameworkWindow 依赖属性持有，一并持久化）、`Placements: Dictionary<string, ToolViewPlacementEntry>`（:29，可移动工具视图 Id → 归属 Bar 与 Bar 内序号；钉住项恒在 ActivityBar 底部段不入表；孤儿条目丢弃、无条目的新工具视图落回 Default）、三个可空子 DTO `SideBar`/`AuxiliaryPanel`/`BottomPanel`（:31-35）。`ToolViewPlacementEntry`（:41）：`Bar`（`ToolViewPlacement`）+ `Index`（Bar 内序号，小者靠前）。`SideBarLayoutDto`（:54）：`Visible`、`Width=240`、`Selected`（收起时也保留，与 `SideBarState.ContentFor` 同语义）。`PanelLayoutDto`（:69）：`Visible=true`、`Width=280`、`ActiveTab`。`BottomPanelLayoutDto`（:81）：`Visible=true`、`Height=160`、`ActiveTab`。

### `Layout/LayoutPersistence.cs`
`public sealed class LayoutPersistence`（第 12 行）：布局持久化服务（ADR-0002），%AppData%/Digital.Workstation/layout.json 的读/写/删，全部失败路径只记日志不打断应用。成员：`public static readonly string FilePath`（:17-19）；`Load()`（:37，文件缺失返回 null 且无日志——首次启动常态；内容为空/`Version≠CurrentVersion` 记 `Logger.Warning` 后返回 null；`catch (Exception)` 全捕获兜底，:62-67）；`ScheduleSave(ShellLayoutDto)`（:73，`System.Threading.Timer` 防抖 500ms——`DebounceMilliseconds` :21——内的连续布局变更合并为最后一次落盘）；`Delete()`（:86，先在锁内作废 pending 保存——清 `_pending`、停 Timer，:88-92——再 `File.Delete`，否则防抖回调会把文件重建；删除失败记 Warning）；私有 `Flush`（:104，Timer 回调：取出并清空 `_pending` 后 `Directory.CreateDirectory` + 序列化写盘，try/catch 全捕获记 Warning——注释自述"回调里的异常无人处理会拖垮进程"，:118）。序列化选项（:23-28）：`WriteIndented`、camelCase 属性名、`JsonStringEnumConverter`（枚举落成 `"Center"`/`"BottomPanel"` 形态字符串）。线程安全经 `Lock _gate`（:30）。

### `Layout/PanelResizeTarget.cs`
`public enum PanelResizeTarget`（第 6 行）：`SideBar` / `AuxiliaryPanel` / `BottomPanel`，`ShellLayoutState.Resize` 的目标参数。

### `Layout/PanelAlignment.cs`
`public enum PanelAlignment`（第 7 行）：`Left`/`Right`/`Center`/`Justify` 四档，FrameworkWindow 基础布局的档位，决定 BottomPanel 在窗口底部的水平跨度；注释自述"面板不占的列由侧栏通高到底吸收，不留空挡"。

### `Layout/PanelResize.cs`
`public readonly record struct PanelResize(PanelResizeTarget Target, double Delta)`（第 6 行）：分隔条命令参数，Delta 为已换算方向的尺寸增量。

### `Layout/PanelResizer.cs`
`public class PanelResizer : GridSplitter`（第 13 行，命名空间 `DigitalWorkstation.Core.Framework.Layout`），自 Modules/Workstation 迁入并改造。成员：`ResizeCommandProperty` StyledProperty（:15）、CLR 属性 `Target`（:31，决定增量取轴与取反）与 `ResizeCommand`（:36）、`StyleKeyOverride => typeof(GridSplitter)`（:26，继承 GridSplitter 主题）、`GetParentGrid() => null`（:46，禁用原生重排）、私有 `OnDragDelta`（:54，方向换算 SideBar=+X、AuxiliaryPanel=-X、BottomPanel=-Y 后 `ResizeCommand?.Execute(new PanelResize(Target, delta))`）。

### `Layout/ToolViewMove.cs`
`public sealed record ToolViewMove(string TabId, ToolViewPlacement TargetBar, int Index)`（第 12 行，ADR-0002）：一次拖拽落放的命令参数，`ToolViewBar.OnDrop` 构造、经 MoveCommand 发给 ViewModel；`Index` 按目标 Bar 移除前的列表计，同 Bar 重排的索引修正在 `ShellLayoutState.MoveTab` 内部。

### `Layout/ToolViewDragSession.cs`
`public static class ToolViewDragSession`（第 11 行）：工具视图拖拽会话。`static readonly DataFormat<string> TabIdFormat`（:15，应用格式 "DigitalWorkstation.ToolView"，负载为视图 Id 字符串）；`IsActive`（:20）与 `event Action<bool>? ActiveChanged`（:25）——`ToolViewButton` 在 `DoDragDropAsync` 期间 `Begin()`/`End()`（:28/:33），shell 借此临时显露隐藏面板作为投放区（「向隐藏面板拖入」的先决条件）。

### `Layout/ToolViewButton.cs`
`public class ToolViewButton : Button`（第 14 行，ADR-0002）：可拖拽的工具视图按钮（面板 tab 头 / ActivityBar 导航项）。StyledProperty：`DragTabIdProperty`（:21，负载 Id）、`CanDragProperty`（:24，默认 true，钉住项绑 false 不发起拖拽、保持普通点击）。`StyleKeyOverride => typeof(Button)`（:32，继承 Button 主题使 `Button.nav-item`/`Button.panel-tab` 类样式命中）。手势：OnPointerPressed 记录起点（:62）、OnPointerMoved 超 4px 阈值（`DragThreshold` :19）发起 `DragDrop.DoDragDropAsync`（:92，Avalonia 11.3 新 DataTransfer API），前后 `ToolViewDragSession.Begin/End`（finally 保证）；注释自述拖拽开始即移交指针捕获、Click 不触发（:85）。

### `Layout/ToolViewBar.cs`
`public class ToolViewBar : ItemsControl`（第 19 行，ADR-0002）：工具视图 Bar 的投放目标（ActivityBar 顶部段与两个面板 tab 条；底部钉住段不用本控件，自然禁止光标）。StyledProperty：`TargetBarProperty`（:33）、`OrientationProperty`（:36，决定插入序号轴向与占位线方向）、`MoveCommandProperty`（:39）。构造（:44）：`DragDrop.SetAllowDrop(this, true)` + AddHandler 挂 DragOver/Drop/DragLeave；另在 OnAttachedToVisualTree/OnDetachedFromVisualTree（:65-76）配对订阅 `ToolViewDragSession.ActiveChanged`——会话结束一律 `ClearInsertion`（:81-87），兜底 Esc 取消/窗外松手等收不到 DragLeave 的路径。**不得声明 StyleKeyOverride**——Avalonia 类型选择器匹配 StyleKey 而非运行时类型，override 会让 `layout|ToolViewBar` 选择器与 ControlTheme 查找失效；其 ControlTheme 在 FrameworkWindowTheme.axaml（透明背景使命中测试覆盖整条带、模板含 `PART_InsertionLine`）。`OnApplyTemplate`（:93）按 Orientation 配置占位线横/竖（横向 Bar 竖线 2px、纵向 Bar 横线 2px，`InsertionLineThickness` :26）。`ComputeInsertionIndex`（:157，指针在条目前半→插其前，否则末尾）；`ShowInsertion`（:181，经 `TranslatePoint` 把落点缝隙坐标换算为占位线 Margin）；`OnDragLeave`（:149）有冒泡守卫——指针真正离开本 Bar 才清除。

### `Windows/FrameworkWindow.cs`
`public abstract class FrameworkWindow : UrsaWindow`（第 22 行）——带基础布局的窗口基类，内置 VS Code 式五区 shell + 标题栏左侧菜单栏（ADR-0001）+ 命令面板浮层（ADR-0005）。成员：`PanelAlignmentProperty`（:24，StyledProperty，默认 `Center`）与 CLR 包装 `PanelAlignment`（:79）、`StyleKeyOverride => typeof(UrsaWindow)`（:74，继承窗口 chrome 主题）、构造函数（:31，`Styles.Add(_theme)` + ContentControl 布局宿主 + **内置命令面板**：`_palette` ItemsSource 宽松绑定 `"Commands"`、`Content = new Panel { _layoutHost, _palette }` 叠层、Ctrl+P KeyBinding（:36-44）+ **内置菜单栏**：`LeftContent = new Menu { Classes=chrome-menu, ItemsSource 宽松绑定 "MenuBarItems" }`（:47-52）与 `DataTemplates.Add(FuncDataTemplate<MenuItemViewModel>)` 注册项模板（:53，子菜单任意深度经模板查找递归复用）+ `UpdateLayoutTemplate`）、私有 `BuildMenuItemHeader`（:60，图标 null 不创建 PathIcon 不留占位间隙）、`OnPropertyChanged`（:85，监听 PanelAlignmentProperty）、私有 `UpdateLayoutTemplate`（:94，枚举→资源键映射 + `TryGetResource` 查找，缺失抛异常）、`RegisterCommandGestures`（:114，为带 Gesture 的命令生成窗口级 KeyBinding，`KeyGesture.Parse` 失败记日志跳过）。

### `Windows/CommandPalette.cs`
`public class CommandPalette : Border`（第 18 行，ADR-0005）：命令面板控件，窗口顶部居中的命令检索浮层。自包含——搜索框（`TextBox.command-input`，水印 `Language.CommandPaletteWatermark`）+ 列表（`ListBox.command-list`，代码创建的 `FuncDataTemplate<ICommandContribution>`：三列 Grid——左侧 `PathIcon.command-icon` 槽位始终渲染（`IconPath` 为 null 时为空占位，文本对齐）+ 标题 + 右侧 gesture 文本（`Gesture` 为 null 时隐藏））+ 空态（`TextBlock.placeholder`，`Language.NoMatchingCommands`）全部代码构建；子串过滤（不区分大小写、匹配本地化标题）、↑↓/Enter/Esc 键盘导航（`OnKeyDown`）、单击执行、面板外点击关闭（Open 时挂 TopLevel PointerPressed Tunnel 监听、Close 摘除）、MRU `_recentIds` 内存置顶（新者在前，重启即清）全部内聚。`ItemsSourceProperty` StyledProperty（:20）接命令数据源（宽松绑定 ViewModel 的 `Commands`）；公开方法 `Open()`（:76）/`Close()`（:89）。未声明 `StyleKeyOverride`——样式选择器 `windows|CommandPalette` 在 FrameworkWindowTheme.axaml:630-667。

### `Layout/SetPanelAlignmentEvent.cs`
`public class SetPanelAlignmentEvent : PubSubEvent<PanelAlignment>`（第 8 行）：请求切换布局档位的事件契约。注释自述放本模块而非 Core/Models 的原因（负载类型定义于此，Models 引用 Framework 会成环）。

### `Windows/FrameworkWindowTheme.axaml`
无 x:Class 的 Styles 根（662 行）。`Styles.Resources`：`NavigationItemTemplate`（:9，按钮为 ToolViewButton，绑 CanDrag/DragTabId）、六个共享部件 DataTemplate（`ShellActivityBar` :27 / `ShellSideBar` :59 / `ShellMainContent` :75 / `ShellAuxiliaryPanel` :87 / `ShellBottomPanel` :142 / `ShellStatusBar` :198——三处可投放 Bar 用 layout:ToolViewBar 承载并绑 MoveCommand/TargetBar/Orientation，ActivityBar 底部段为 StackPanel（:41-53）：钉住区普通 ItemsControl（`BottomNavigationItems`，当前无钉住项实例）+ shell 内置"设置"导航按钮（`OpenSettingsCommand`，ADR-0006 决策 6））、四份布局 DataTemplate（`WindowLayoutLeft` :227 / `WindowLayoutRight` :293 / `WindowLayoutCenter` :359 / `WindowLayoutJustify` :424，差异为 BottomPanel 及分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`，ActivityBar 恒 `RowSpan=2` 通高）、`ToolViewBar` 的 ControlTheme（:492，透明背景使整条带参与命中测试，模板含 `PART_InsertionLine` 拖拽占位线）。样式（:513 起）自 Modules/Workstation/MainWindow.axaml 迁入；`layout|ToolViewBar.drag-over`（:554）为拖拽悬停整 Bar 高亮；Auxiliary/Bottom 面板卡片 `IsVisible` 绑 `AuxiliaryPanelRevealed`/`BottomPanelRevealed`（拖拽会话期间临时显露隐藏面板，ADR-0002）。末尾 4 个标题栏菜单样式（:611-628）：顶层 MenuItem 紧凑行高、Popup#PART_Popup VerticalOffset=-8 贴合标题栏下缘、`Menu.chrome-menu MenuItem` 的 ItemsSource/Command/AutomationProperties.Name 样式绑定、PathIcon 前景色。

### `Windows/FrameworkWindowTheme.cs`
`public class FrameworkWindowTheme : Styles`（第 12 行）：`StyleInclude`（BaseUri `avares://DigitalWorkstation.Core.Framework/Windows/`，:14；Source 相对 `FrameworkWindowTheme.axaml`，:20）加载主题，构造时 `_ = include.Loaded` 强制加载（:22）再 `Add(include)`（:23）。

### `Contributions/ShellContributionCollector.cs`
`public class ShellContributionCollector(IContainerProvider containerProvider)`（第 12 行，主构造；命名空间 `DigitalWorkstation.Core.Framework.Contributions`）。七个收集方法：`GetToolViews()`（:18，先 `Where` 过滤 DryIoc 零注册幽灵实例（:23，见 pitfalls.md）再 `Order` 升序，不按定位枚举过滤——三处 Bar 的分派由消费方按 `Placement`/`AllowMove` 决定，ADR-0002）、`GetMainViews()`（:27）、`GetMenuItems()`（:34，无参数——不过滤不排序，建树由 `MenuTreeBuilder` 负责，ADR-0001）、`GetCommands()`（:42，Order/标题排序 + Id 去重，ADR-0005）、`GetStatusBarItems()`（:63，`Order` 升序）、`GetSettingGroups()`（:74，ADR-0006）、`GetSettingItems()`（:99，ADR-0006）。

### `Contributions/ToolViewRegistration.cs`
`public static class ToolViewRegistration`（第 14 行）：`RegisterToolViews(this IContainerRegistry, Assembly)` 扩展（:20，ADR-0002）——扫描传入程序集中标注 `ToolViewAttribute` 的类（不做全局扫描），非可实例化 `Control` 或程序集内 `Id` 重复记 `Logger.Warning` 跳过；合法者 `Register(viewType)` 注册 View 类型本身，并把 attribute 元数据（标题经 `Language.Get` 解析）落成 `ToolViewContribution` 注册 singleton。与 `Menus/MenuRegistration.cs` 同构。

### `Settings/SettingRegistration.cs`
`public static class SettingRegistration`（第 13 行）：`RegisterSettings(this IContainerRegistry, Assembly)` 扩展（:19，ADR-0006 决策 1）——扫描传入程序集（不做全局扫描）：类上每条 `SettingGroupAttribute` 声明注册一个 `SettingGroupContribution` 单例（:23-27，同名分组的合并与位次取最小发生在收集侧）；`Public | Static | DeclaredOnly` 属性中标注 `SettingItemAttribute` 者（:29-36），无 getter 或 `DefaultValue` 类型不匹配记 `Logger.Warning` 跳过（:38-51），合法者落成 `SettingItemContribution` 单例（:53-63，`Id` 默认「声明类全名.属性名」，`ValueType` 取属性类型）。与 `Menus/MenuRegistration.cs` 同构。

### `Settings/SettingsService.cs`
`public sealed class SettingsService(IEventAggregator, IContainerProvider) : ISettingsService`（第 17 行，ADR-0006 决策 3/4）：%AppData%/Digital.Workstation/settings.json 的读/防抖写，启动时经 `Load()`（:64）一次性加载入内存并复制为启动值快照 `_sessionStartValues`（:52，「重启后生效」判定基准，决策 7）。成员：`public static readonly string FilePath`（:23-25）；`Load()`（文件缺失静默返回——首次启动常态；内容为空或一切异常记 `Logger.Warning` 按无修改处理，:75-93，容错仿 LayoutPersistence）；`Get<T>(string)`（:97，纯内存读——`_values` 命中反序列化返回、单项失败记 Warning 逐项回退默认值，未修改经 `FindContribution` 回退声明 `DefaultValue`，未声明记 Warning 返回 `default`）；`Set<T>(string, T)`（:126，锁内更新内存 + 500ms `Timer` 防抖 + `TrackPendingRestart`（:158，与启动值 `JsonElement.DeepEquals` 比较维护 `_pendingRestartIds`，:57），锁外广播 `SettingChangedEvent`）；`IsPendingRestart(string)`（:146，决策 7 判定查询）；`FlushPending()`（:198，停防抖 Timer + 同步写盘，供「立即重启」在启动新进程前调用）；私有 `FindContribution`（:179，声明缓存按 Id 惰性填充、未命中重新枚举容器以允许模块后到注册）；私有 `Flush`/`TakeSnapshot`/`Save`（:208/:213/:222，Timer 回调写盘，`Save` 对一切异常就地吞掉记 Warning——纪律同 `LayoutPersistence.Flush`）。序列化选项（:29-33）：`WriteIndented` + `JsonStringEnumConverter`——枚举落盘为 `JsonStringEnumMemberName` 字符串。

### `Settings/UiLanguage.cs`
`public enum UiLanguage`（第 12 行）：`[JsonStringEnumMemberName("zh-CN")] ZhCN` / `[JsonStringEnumMemberName("en-US")] EnUS`——attribute 值即对应 `CultureInfo` 名称，落盘值与区域性名称同源。同文件 `public static class UiLanguageExtensions`（:30）：`ToCultureInfo(this UiLanguage)`（:32，反射读 `JsonStringEnumMemberName` 值作 `CultureInfo.GetCultureInfo` 名称）。

### `Settings/GeneralSettings.cs`
`[SettingGroup("SettingsGeneralGroupName", Order = 0)] public static class GeneralSettings`（第 10 行，ADR-0006 决策 9）：Framework 预置「常规/语言」设置项的声明类。`public static readonly string LanguageSettingId`（:15，默认规则「声明类全名.属性名」，供启动序列等消费方读写）；`[SettingItem("SettingsGeneralGroupName", "SettingsLanguageName", DefaultValue = UiLanguage.ZhCN, RequiresRestart = true)] public static UiLanguage Language`（:20-22）——属性体只是声明锚点不会被读取，读写一律经 `ISettingsService`。

### `Menus/MenuItemViewModel.cs`
`public class MenuItemViewModel`（第 13 行），自 Modules/Workstation 迁入（命名空间 `DigitalWorkstation.Core.Framework.Menus`）。菜单项呈现模型：`Title`（required init，:19）、`Icon: Geometry?`（:24，null 时弹出层内仍渲染空 `PathIcon` 占位对齐）、`Command: ICommand?`（:26）、`IsTopLevel: bool`（:32，顶层菜单不预留图标槽位）、`Children: ObservableCollection<object>`（:34，子项为 MenuItemViewModel 或 Avalonia `Separator`）；静态 `FromSubmenu(MenuTreeSubmenu, bool isTopLevel = false)`（:39）把建树器产物递归转换（`IconPath` 经 `StreamGeometry.Parse` 转几何，:49；分隔线转 `Separator`，:53；仅 shell 建根项时传 `isTopLevel: true`）。

### `Menus/MenuTreeEntry.cs`
菜单树的条目 record 族（ADR-0001）：`abstract record MenuTreeEntry`（第 10 行）；`MenuTreeItem(Title, IconPath?, Command)`（:15，叶子，标题已解析）；`MenuTreeSubmenu(Title, Children)`（:21，子菜单节点含顶层菜单，Children 分隔线已插好）；`MenuTreeSeparator`（:26，组间分隔线标记，私有构造单例 `Instance`）。

### `Menus/MenuTreeBuilder.cs`
`public static class MenuTreeBuilder`（第 13 行）：纯函数建树器。`Build(IEnumerable<IMenuItemContribution>) → IReadOnlyList<MenuTreeSubmenu>`（:18）：路径含空段记日志跳过（:24-29）；单段路径 Order 声明顶层位次、Group/GroupOrder 描述条目分组（:41-47）；多段路径三者声明末端子菜单节点位次、条目进默认组（:48-54）；顶层按 `(NodeOrder, Language.Get(段) Ordinal)` 排序不分组（:57-61）；子菜单内按 `(GroupOrder, null 组优先, Group Ordinal, Order, Title Ordinal)` 排序、组间插分隔线（:77-96）。内部累积结构 `LeafAccum` record（:101）与 `NodeAccum` class（:104，`MergeNodeOrder`/`MergePlacement` 位次冲突取最小）。

### `Menus/MenuRegistration.cs`
`public static class MenuRegistration`（第 14 行）：`RegisterMenus(this IContainerRegistry, Assembly)` 扩展（:20）——扫描传入程序集中标注 `MenuGroupAttribute` 的类（不做全局扫描），类路径含空段或方法签名非法（带参/返回值非 void/Task）记 `Logger.Warning` 跳过，菜单类 `RegisterSingleton(Type)`，每个合法 `MenuItemAttribute` 方法注册一个 `IMenuItemContribution` 工厂。同文件 `internal sealed class ReflectedMenuItemContribution`（:75）：构造期把 attribute 元数据落成契约属性（`Title` 经 `Language.Get` 解析），点击反射调用、`Task` 等待、异常记日志不抛出。

### `Commands/CommandRegistration.cs`
`public static class CommandRegistration`（第 13 行，ADR-0005）：`RegisterCommands(this IContainerRegistry, Assembly)` 扩展（:19）——扫描传入程序集中标注 `CommandAttribute` 的方法（免类级 attribute、不做全局扫描），签名非法（带参/返回值非 void/Task）记 `Logger.Warning` 跳过，宿主类 `RegisterSingleton(Type)`，每个合法方法注册一个 `ICommandContribution` 工厂。同文件 `internal sealed class ReflectedCommandContribution`（:60）：构造期把 attribute 元数据落成契约属性（`Id` 默认「声明类全名.方法名」、`Title` 经 `Language.Get` 解析、`Gesture`/`IconPath`/`Order` 透传），执行反射调用、`Task` 等待、异常记日志不抛出。与 `Menus/MenuRegistration.cs` 同构。

### `WindowManager/FrameworkWindowManager.cs`
`public class FrameworkWindowManager : IWindowManager, IMainWindowManager`（第 12 行，命名空间 `DigitalWorkstation.Core.Framework.WindowManager`）。内部状态 `_windowMap: Dictionary<Type, Window>`（:17）与 `_mainWindow`（:22）。入口：`GetWindow`（:35）、`InitializeWindow`（:45，私有）、`ShowWindow` 四重载（:57-111）、`ShowDialog` 四重载（:113-141）、`CloseWindow`（:144）、`HideWindow`（:150）、`HandleMainWindow`（:158）、`HideMainWindow`/`ShowMainWindow`（:169-170）、`CloseWindowsExceptMain`（:172）。

## 对应测试文件（UnitTest/Framework/）

```
UnitTest/Framework/
├── Framework.csproj                  xUnit 测试项目：Microsoft.NET.Test.Sdk 17.6.0 + xunit 2.4.2 + xunit.runner.visualstudio 2.4.5
└── ShellLayoutStateResizeTests.cs    11 个 [Fact]：Resize 增减/clamp/区域隔离/收起恢复保留尺寸
```
