# Framework — 文件结构与功能

目录树（相对 `Core/Framework/`，省略 `obj/` 与 `Output/` 构建产物）：

```
Core/Framework/
├── Framework.csproj                       项目文件：net10.0；引用 Abstractions/Common/Models/UIPackage/Resource 五项目与 Avalonia/Prism/Ursa 相关包
├── FrameworkApplication.cs                应用入口基类与启动序列（含设置服务注册、启动时语言应用及本地化 Application.Name，[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md)）
├── ApplicationRestarter.cs                统一保存配置，成功后启动新进程并退出；保存失败取消重启
├── Plugins/
│   ├── PluginDescriptor.cs                 插件发现结果，包含入口 Type、依赖与错误归属
│   ├── PluginDiscovery.cs                  PE 元数据识别入口、校验重复身份并加载候选
│   └── PluginLoadContext.cs                Release 私有依赖与宿主共享程序集解析
├── Persistence/
│   ├── ConfigurationPersistence.cs         配置文件 owner：统一刷新、退出收尾；内部 IConfigurationFile 契约
│   └── DebouncedJsonFile.cs                单文件防抖、写盘/删除互斥、完整临时文件替换与失败重试
├── Layout/                                  命名空间 DigitalWorkstation.Core.Framework.Layout
│   ├── ShellLayoutState.cs                布局状态根 record + 全部转换方法
│   ├── SideBarState.cs                    SideBar 区域状态 record
│   ├── AuxiliaryPanelState.cs             AuxiliaryPanel 区域状态 record
│   ├── BottomPanelState.cs                BottomPanel 区域状态 record
│   ├── MainContentState.cs                MainContent 区域状态 record
│   ├── ShellLayoutConfiguration.cs      State 与 DTO 的恢复/捕获边界
│   ├── ShellLayoutMetrics.cs            模板与列宽共用尺寸来源
│   ├── ShellLayoutDto.cs                  布局持久化落盘 DTO record 族（独立于 ShellLayoutState，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）
│   ├── LayoutPersistence.cs               布局持久化服务：layout.json 读/防抖写/删，全路径容错只记日志
│   ├── PanelResizeTarget.cs               可调尺寸区域枚举
│   ├── PanelAlignment.cs                  面板对齐枚举（FrameworkWindow 基础布局四档）
│   ├── PanelResize.cs                     分隔条命令参数 record struct
│   ├── SetPanelAlignmentEvent.cs          面板对齐切换事件契约（PubSubEvent<PanelAlignment>）
│   ├── PanelResizer.cs                    面板分隔条（GridSplitter 改造，自 Modules/Workstation 迁入）
│   ├── ToolViewMove.cs                    工具视图拖拽落点参数 record（TabId/TargetBar/Index，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）
│   ├── ToolViewDragSession.cs             工具视图拖拽会话：DataFormat + IsActive 静态信号（拖拽期间显露隐藏面板）
│   ├── ToolViewButton.cs                  可拖拽工具视图按钮（tab 头/导航项）：PointerPressed+位移阈值 → DoDragDropAsync
│   └── ToolViewBar.cs                     工具视图 Bar 投放目标：DragOver 算插入序号并移动占位线，Drop 执行 MoveCommand
├── Menus/                                   命名空间 DigitalWorkstation.Core.Framework.Menus
│   ├── MenuItemViewModel.cs               菜单项呈现模型（Title/Icon?/Command?/IsTopLevel/Children + FromSubmenu 递归转换，自 Modules/Workstation 迁入）
│   ├── MenuTreeEntry.cs                   菜单树条目 record 族（叶子/子菜单/分隔线单例）
│   ├── MenuTreeBuilder.cs                 菜单建树器（纯函数：路径切分、分组排序、插分隔线，[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）
│   └── MenuRegistration.cs                attribute 菜单注册扩展 + internal 反射贡献实现
├── Commands/                                命名空间 DigitalWorkstation.Core.Framework.Commands（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）
│   └── CommandRegistration.cs             attribute 命令注册扩展（免类级 attribute）+ internal 反射贡献实现
├── Contributions/                           命名空间 DigitalWorkstation.Core.Framework.Contributions
│   ├── ShellContributionCatalog.cs      贡献登记描述、批次可见性与单次构造
│   ├── ShellContributionCollector.cs      shell 贡献收集器（含设置分组/设置项收集，[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md)）
│   └── ToolViewRegistration.cs            attribute 工具视图注册扩展（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）
├── Settings/                                命名空间 DigitalWorkstation.Core.Framework.Settings（[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md)）
│   ├── SettingRegistration.cs             attribute 设置注册扩展（与 RegisterMenus/RegisterCommands 同构）
│   ├── SettingCatalog.cs               有效设置声明与分组合并
│   ├── SettingsService.cs                 ISettingsService 实现：加载、内存读写、独立快照提交、事件和重启标记
│   ├── UiLanguage.cs                      界面语言枚举（zh-CN/en-US）+ ToCultureInfo 扩展
│   └── GeneralSettings.cs                 Framework 预置「常规/语言」设置项声明类
├── Resources/                               命名空间 DigitalWorkstation.Core.Framework.Resources
│   ├── FrameworkResources.cs              public static 资源所属类型，强类型属性调用 ResourceText
│   ├── FrameworkResources.resx            Framework 私有中性中文文案
│   └── FrameworkResources.en-US.resx      Framework 私有英文文案
├── Windows/                                 命名空间 DigitalWorkstation.Core.Framework.Windows（窗口基类与主题）
│   ├── FrameworkWindow.cs                 带基础布局的窗口基类（内置 VS Code 式五区 shell + 标题栏菜单栏 + 命令面板浮层与 Ctrl+P）
│   ├── CommandPalette.cs                  命令面板控件（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）：过滤/键盘导航/MRU 内存置顶、平台快捷键标签格式化，ItemsSource 宽松绑定 Commands
│   ├── FrameworkWindowTheme.axaml         基础布局主题资源（四份布局模板 + 共享部件模板 + shell 样式 + 菜单样式 + 命令面板样式；ActivityBar 底部段 Grid(*,Auto) 分隔、StackPanel 承载钉住区 + 内置"设置"导航按钮；PanelResizer 经 xmlns:layout 引用）
│   └── FrameworkWindowTheme.cs            主题加载器（StyleInclude 强制加载）与顶层菜单跨屏锚定矩形裁剪
└── WindowManager/
    └── FrameworkWindowManager.cs          IWindowManager + IMainWindowManager 实现
```

## 各文件功能

### `Framework.csproj`
`net10.0`、`ImplicitUsings`+`Nullable` 开启。ProjectReference：`..\Abstractions`、`..\Common`、`..\Models`、`..\UIPackage`、`..\Resource`（第 10-14 行）。PackageReference：`Avalonia.Desktop`/`Avalonia.Fonts.Inter`/`Avalonia.Themes.Fluent` 11.3.20、`AvaloniaUI.DiagnosticsSupport` 2.1.1、`CommunityToolkit.Mvvm` 8.4.0、`Irihi.Ursa` 1.15.1（第 18-23 行，FrameworkWindow 直接继承 UrsaWindow）。

### `FrameworkApplication.cs`
定义 `public abstract class FrameworkApplication<TWindow> : PrismApplication where TWindow : Window`（第 20 行，命名空间 `DigitalWorkstation.Core.Framework`）。关键入口：
- `Initialize()`（第 25 行）：主题装载（Dark + `WorkstationTheme` + `VSCodePalette.ApplyTo`）。
- `OnFrameworkInitializationCompleted()`（第 39 行）→ `RunStartupSequenceAsync()`（第 68 行）：三阶段启动序列（[ADR-0004](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md)）。
- `OnInitialized()`（第 48 行）、`InitializeModules()`（第 55 行）：两个故意的空覆盖。
- `CreateSplashWindow()`（第 62 行，abstract）/`RegisterCustomService()`（第 215 行，virtual）：子类扩展点。
- `RegisterTypes` → `RegisterFrameworkServices`：初始化 IoC，注册窗口管理器、贡献收集器、配置持久化 owner 与布局单例；owner 在真正 Exit 时 Dispose。显式构造 SettingsService(SettingCatalog, owner) 并 Load，注册 ISettingsService 与框架设置，再 ApplyLanguageSetting。语言应用时机仍先于模块注册。
- `ConfigureViewModelLocator()`（第 237 行）：约定式 ViewModel 定位解析器。

### ApplicationRestarter.cs

Restart 先通过 ConfigurationPersistence.FlushPending 完成设置与布局保存；失败则记英文 Error 并停止重启。成功后仍以当前可执行路径和原始参数启动新进程，再走正常 Shutdown。设置页调用方式不变。

### Persistence/ConfigurationPersistence.cs

公开 owner 由 FrameworkApplication 显式注册；内部 `CreateFile<T>` 登记两个真实配置文件，FlushPending 尝试全部文件并返回汇总结果，Dispose 最后保存并释放 Timer。接线使用 IControlledApplicationLifetime.Exit，取消关闭不会释放 owner。

### Persistence/DebouncedJsonFile.cs

内部泛型文件实现。ScheduleSave 缓存独立快照并防抖 500ms；同一文件锁覆盖写入、刷新、删除和释放。序列化到同目录唯一临时文件，Flush(true) 后替换目标。失败记 Warning 并保留 pending，删除先等待在途写入再作废 pending；释放后拒绝新写入/删除。

### `Layout/ShellLayoutState.cs`
`public sealed record ShellLayoutState`（第 9 行）：`SelectedActivity`（:14）与 `ActivityBarItems`（:20，ActivityBar 顶部段有序 Id 列表，钉住项不入列，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）+ 五个区域状态属性 + `static Initial`（:30）。转换方法：`SelectActivity`（:36）、`ToggleSideBar`（:53）、`ToggleAuxiliaryPanel`（:61）、`ToggleBottomPanel`（:69）、`ActivateAuxTab`（:77）、`ActivateBottomTab`（:90）、`MoveTab`（:109，跨 Bar 迁移/同 Bar 重排）、`OpenMainView`（:236）、`Resize`（:244）、私有 `Clamp`（:275）。注释自述："原型验证过的 reducer 的正式实现"。

### `Layout/SideBarState.cs`
`public sealed record SideBarState`（第 6 行）：`const MinWidth=120 / MaxWidth=480`；`Visible`（默认 false）、`Width=240`、`ContentFor`（string?，收起时保留导航项 Id）。

### `Layout/AuxiliaryPanelState.cs`
`public sealed record AuxiliaryPanelState`（第 6 行）：`const MinWidth=120 / MaxWidth=480`；`Visible=true`、`Width=280`、`Tabs=[]`、`ActiveTab`（string?）。

### `Layout/BottomPanelState.cs`
`public sealed record BottomPanelState`（第 6 行）：`const MinHeight=80 / MaxHeight=480`；`Visible=true`、`Height=160`、`Tabs=[]`、`ActiveTab`（string?）。

### `Layout/MainContentState.cs`
`public sealed record MainContentState`（第 6 行）：仅 `ActiveView`（string?），单视图切换。

### `Layout/ShellLayoutDto.cs`
布局持久化 DTO record 族（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：layout.json 落盘的专用格式，独立于 `ShellLayoutState`（状态机只管流转语义，不管序列化兼容）。`ShellLayoutDto`（第 10 行）：`const CurrentVersion=1`（:15）与 `Version`（:17，不识别的文件由 LayoutPersistence 整体丢弃）、`PanelAlignment`（:22，对齐档位不在状态机内、由 FrameworkWindow 依赖属性持有，一并持久化）、`Placements: Dictionary<string, ToolViewPlacementEntry>`（:29，可移动工具视图 Id → 归属 Bar 与 Bar 内序号；钉住项恒在 ActivityBar 底部段不入表；孤儿条目丢弃、无条目的新工具视图落回 Default）、三个可空子 DTO `SideBar`/`AuxiliaryPanel`/`BottomPanel`（:31-35）。`ToolViewPlacementEntry`（:41）：`Bar`（`ToolViewPlacement`）+ `Index`（Bar 内序号，小者靠前）。`SideBarLayoutDto`（:54）：`Visible`、`Width=240`、`Selected`（收起时也保留，与 `SideBarState.ContentFor` 同语义）。`PanelLayoutDto`（:69）：`Visible=true`、`Width=280`、`ActiveTab`。`BottomPanelLayoutDto`（:81）：`Visible=true`、`Height=160`、`ActiveTab`。

### Layout/LayoutPersistence.cs

构造接收 ConfigurationPersistence 并创建布局文件写入器；保留 FilePath、Load、ScheduleSave、Delete。Load 继续检查 Version、处理缺失/损坏；后两个操作委托 DebouncedJsonFile。它不再自行拥有 pending、Timer 或 Flush 回调。WriteIndented、camelCase、枚举字符串格式保持不变。

### `Layout/PanelResizeTarget.cs`
`public enum PanelResizeTarget`（第 6 行）：`SideBar` / `AuxiliaryPanel` / `BottomPanel`，`ShellLayoutState.Resize` 的目标参数。

### `Layout/PanelAlignment.cs`
`public enum PanelAlignment`（第 7 行）：`Left`/`Right`/`Center`/`Justify` 四档，FrameworkWindow 基础布局的档位，决定 BottomPanel 在窗口底部的水平跨度；注释自述"面板不占的列由侧栏通高到底吸收，不留空挡"。

### `Layout/PanelResize.cs`
`public readonly record struct PanelResize(PanelResizeTarget Target, double Delta)`（第 6 行）：分隔条命令参数，Delta 为已换算方向的尺寸增量。

### `Layout/PanelResizer.cs`
`public class PanelResizer : GridSplitter`（第 13 行，命名空间 `DigitalWorkstation.Core.Framework.Layout`），自 Modules/Workstation 迁入并改造。成员：`ResizeCommandProperty` StyledProperty（:15）、CLR 属性 `Target`（:31，决定增量取轴与取反）与 `ResizeCommand`（:36）、`StyleKeyOverride => typeof(GridSplitter)`（:26，继承 GridSplitter 主题）、`GetParentGrid() => null`（:46，禁用原生重排）、私有 `OnDragDelta`（:54，方向换算 SideBar=+X、AuxiliaryPanel=-X、BottomPanel=-Y 后 `ResizeCommand?.Execute(new PanelResize(Target, delta))`）。

### `Layout/ToolViewMove.cs`
`public sealed record ToolViewMove(string TabId, ToolViewPlacement TargetBar, int Index)`（第 12 行，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：一次拖拽落放的命令参数，`ToolViewBar.OnDrop` 构造、经 MoveCommand 发给 ViewModel；`Index` 按目标 Bar 移除前的列表计，同 Bar 重排的索引修正在 `ShellLayoutState.MoveTab` 内部。

### `Layout/ToolViewDragSession.cs`
`public static class ToolViewDragSession`（第 11 行）：工具视图拖拽会话。`static readonly DataFormat<string> TabIdFormat`（:15，应用格式 "DigitalWorkstation.ToolView"，负载为视图 Id 字符串）；`IsActive`（:20）与 `event Action<bool>? ActiveChanged`（:25）——`ToolViewButton` 在 `DoDragDropAsync` 期间 `Begin()`/`End()`（:28/:33），shell 借此临时显露隐藏面板作为投放区（「向隐藏面板拖入」的先决条件）。

### `Layout/ToolViewButton.cs`
`public class ToolViewButton : Button`（第 14 行，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：可拖拽的工具视图按钮（面板 tab 头 / ActivityBar 导航项）。StyledProperty：`DragTabIdProperty`（:21，负载 Id）、`CanDragProperty`（:24，默认 true，钉住项绑 false 不发起拖拽、保持普通点击）。`StyleKeyOverride => typeof(Button)`（:32，继承 Button 主题使 `Button.nav-item`/`Button.panel-tab` 类样式命中）。手势：OnPointerPressed 记录起点（:62）、OnPointerMoved 超 4px 阈值（`DragThreshold` :19）发起 `DragDrop.DoDragDropAsync`（:92，Avalonia 11.3 新 DataTransfer API），前后 `ToolViewDragSession.Begin/End`（finally 保证）；注释自述拖拽开始即移交指针捕获、Click 不触发（:85）。

### `Layout/ToolViewBar.cs`
`public class ToolViewBar : ItemsControl`（第 19 行，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：工具视图 Bar 的投放目标（ActivityBar 顶部段与两个面板 tab 条；底部钉住段不用本控件，自然禁止光标）。StyledProperty：`TargetBarProperty`（:33）、`OrientationProperty`（:36，决定插入序号轴向与占位线方向）、`MoveCommandProperty`（:39）。构造（:44）：`DragDrop.SetAllowDrop(this, true)` + AddHandler 挂 DragOver/Drop/DragLeave；另在 OnAttachedToVisualTree/OnDetachedFromVisualTree（:65-76）配对订阅 `ToolViewDragSession.ActiveChanged`——会话结束一律 `ClearInsertion`（:81-87），兜底 Esc 取消/窗外松手等收不到 DragLeave 的路径。**不得声明 StyleKeyOverride**——Avalonia 类型选择器匹配 StyleKey 而非运行时类型，override 会让 `layout|ToolViewBar` 选择器与 ControlTheme 查找失效；其 ControlTheme 在 FrameworkWindowTheme.axaml（透明背景使命中测试覆盖整条带、模板含 `PART_InsertionLine`）。`OnApplyTemplate`（:93）按 Orientation 配置占位线横/竖（横向 Bar 竖线 2px、纵向 Bar 横线 2px，`InsertionLineThickness` :26）。`ComputeInsertionIndex`（:157，指针在条目前半→插其前，否则末尾）；`ShowInsertion`（:181，经 `TranslatePoint` 把落点缝隙坐标换算为占位线 Margin）；`OnDragLeave`（:149）有冒泡守卫——指针真正离开本 Bar 才清除。

### `Windows/FrameworkWindow.cs`
`public abstract class FrameworkWindow : UrsaWindow`（第 22 行）——带基础布局的窗口基类，内置 VS Code 式五区 shell + 标题栏左侧菜单栏（[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）+ 命令面板浮层（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）。成员：`PanelAlignmentProperty`（:24，StyledProperty，默认 `Center`）与 CLR 包装 `PanelAlignment`（:79）、`StyleKeyOverride => typeof(UrsaWindow)`（:74，继承窗口 chrome 主题）、构造函数（:31，`Styles.Add(_theme)` + ContentControl 布局宿主 + **内置命令面板**：`_palette` ItemsSource 宽松绑定 `"Commands"`、`Content = new Panel { _layoutHost, _palette }` 叠层、Ctrl+P KeyBinding（:36-44）+ **内置菜单栏**：`LeftContent = new Menu { Classes=chrome-menu, ItemsSource 宽松绑定 "MenuBarItems" }`（:47-52）与 `DataTemplates.Add(FuncDataTemplate<MenuItemViewModel>)` 注册项模板（:53，子菜单任意深度经模板查找递归复用）+ `UpdateLayoutTemplate`）、私有 `BuildMenuItemHeader`（:60，图标 null 不创建 PathIcon 不留占位间隙）、`OnPropertyChanged`（:85，监听 PanelAlignmentProperty）、私有 `UpdateLayoutTemplate`（:94，枚举→资源键映射 + `TryGetResource` 查找，缺失抛异常）、`RegisterCommandGestures`（:114，为带 Gesture 的命令生成窗口级 KeyBinding，`KeyGesture.Parse` 失败记日志跳过）。

### `Windows/CommandPalette.cs`
`public class CommandPalette : Border`：自包含命令搜索浮层。水印使用 `FrameworkResources.CommandPaletteWatermark`，空态使用 `FrameworkResources.NoMatchingCommands`；列表三列为图标槽位（null 仍占位）、标题、平台格式化快捷键（null 隐藏）。子串过滤、↑↓/Enter/Esc 导航、单击执行、面板外点击关闭、MRU 内存置顶在控件内完成。ItemsSource 宽松绑定 VM 的 Commands；Open/Close 配对维护 TopLevel PointerPressed 监听。无 StyleKeyOverride，样式在 FrameworkWindowTheme.axaml。

### `Layout/SetPanelAlignmentEvent.cs`
`public class SetPanelAlignmentEvent : PubSubEvent<PanelAlignment>`（第 8 行）：请求切换布局档位的事件契约。注释自述放本模块而非 Core/Models 的原因（负载类型定义于此，Models 引用 Framework 会成环）。

### `Windows/FrameworkWindowTheme.axaml`
无 x:Class 的 Styles 根（662 行）。`Styles.Resources`：`NavigationItemTemplate`（:9，按钮为 ToolViewButton，绑 CanDrag/DragTabId）、六个共享部件 DataTemplate（`ShellActivityBar` :27 / `ShellSideBar` :59 / `ShellMainContent` :75 / `ShellAuxiliaryPanel` :87 / `ShellBottomPanel` :142 / `ShellStatusBar` :198——三处可投放 Bar 用 layout:ToolViewBar 承载并绑 MoveCommand/TargetBar/Orientation，ActivityBar 底部段为 StackPanel（:41-53）：钉住区普通 ItemsControl（`BottomNavigationItems`，当前无钉住项实例）+ shell 内置"设置"导航按钮（`OpenSettingsCommand`，[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 6））、四份布局 DataTemplate（`WindowLayoutLeft` :227 / `WindowLayoutRight` :293 / `WindowLayoutCenter` :359 / `WindowLayoutJustify` :424，差异为 BottomPanel 及分隔条的 `Grid.Column`/`ColumnSpan` 与侧栏的 `Grid.RowSpan`，ActivityBar 恒 `RowSpan=2` 通高）、`ToolViewBar` 的 ControlTheme（:492，透明背景使整条带参与命中测试，模板含 `PART_InsertionLine` 拖拽占位线）。样式（:513 起）自 Modules/Workstation/MainWindow.axaml 迁入；`layout|ToolViewBar.drag-over`（:554）为拖拽悬停整 Bar 高亮；Auxiliary/Bottom 面板卡片 `IsVisible` 绑 `AuxiliaryPanelRevealed`/`BottomPanelRevealed`（拖拽会话期间临时显露隐藏面板，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）。末尾 4 个标题栏菜单样式（:611-628）：顶层 MenuItem 紧凑行高、Popup#PART_Popup VerticalOffset=-8 贴合标题栏下缘、`Menu.chrome-menu MenuItem` 的 ItemsSource/Command/AutomationProperties.Name 样式绑定、PathIcon 前景色。

### `Windows/FrameworkWindowTheme.cs`
`public class FrameworkWindowTheme : Styles`：`StyleInclude`（BaseUri `avares://DigitalWorkstation.Core.Framework/Windows/`；Source 相对 `FrameworkWindowTheme.axaml`）加载主题，构造时 `_ = include.Loaded` 强制加载后 `Add(include)`。静态 `MenuPopupPlacement` 回调供 axaml 顶层菜单 Popup 使用，按按钮中心所在屏幕裁剪锚定矩形，避免最大化 chrome 边缘越界导致选错屏幕。

### `Contributions/ShellContributionCollector.cs`
`ShellContributionCollector(ShellContributionCatalog, SettingCatalog)` 提供七个入口。工具视图/状态栏按 Order；主视图与菜单保持登记序；命令按 Id 首个生效再按 Order/Title 排序；设置项和分组统一由 SettingCatalog 解释，不再直接枚举容器的贡献类型。

### `Contributions/ToolViewRegistration.cs`
`ToolViewRegistration.RegisterToolViews(IContainerRegistry, Assembly)` 只扫描传入程序集。非可实例化 Control 或程序集内 Id 重复记 Warning 跳过；合法者注册 View 类型并生成 singleton 元数据，扫描时经 `ResourceText.Get(attribute.ResourceType, attribute.TitleKey)` 解析标题。

### `Settings/SettingRegistration.cs`
`SettingRegistration.RegisterSettings(IContainerRegistry, Assembly)` 只扫描传入程序集。分组声明透传 Id/ResourceType/Name/Order 生成 singleton；公共静态声明属性无 getter 或默认值类型不符记 Warning 跳过，合法项保存稳定 Group、资源来源、名称键与值元数据。扫描不执行属性体、不查资源；跨程序集分组归并由收集器完成。

### Settings/SettingsService.cs

构造接收 IEventAggregator、IContainerProvider、ConfigurationPersistence；Load 一次性建立内存和启动值快照，Get/Set、默认值查找、事件和重启标记仍由本类拥有。Set 在内存锁内提交独立字典快照给 DebouncedJsonFile，按 Set 的先后排序；没有独立 Timer 或 FlushPending，退出和重启刷新归统一 owner。

### `Settings/UiLanguage.cs`
`public enum UiLanguage`（第 12 行）：`[JsonStringEnumMemberName("zh-CN")] ZhCN` / `[JsonStringEnumMemberName("en-US")] EnUS`——attribute 值即对应 `CultureInfo` 名称，落盘值与区域性名称同源。同文件 `public static class UiLanguageExtensions`（:30）：`ToCultureInfo(this UiLanguage)`（:32，反射读 `JsonStringEnumMemberName` 值作 `CultureInfo.GetCultureInfo` 名称）。

### `Settings/GeneralSettings.cs`
Framework 预置常规/语言设置的声明类；`GroupId = "framework.general"` 为 public const 稳定分组 Id，`LanguageSettingId` 与 Language 属性保持既有持久化契约。类声明 `[SettingGroup(GeneralSettings.GroupId, typeof(FrameworkResources), nameof(FrameworkResources.SettingsGeneralGroupName), Order = 0)]`，语言属性使用 `[SettingItem(GroupId, typeof(FrameworkResources), nameof(FrameworkResources.SettingsLanguageName), DefaultValue = UiLanguage.ZhCN, RequiresRestart = true)]`；属性只是声明锚点，读写走 ISettingsService。

### `Resources/FrameworkResources.cs` / `.resx` / `.en-US.resx`
同基名、同目录的资源所属类型与中性中文/英文文案。包含 CommandPaletteWatermark、NoMatchingCommands、SettingsGeneralGroupName、SettingsLanguageName、SettingsLanguageNameZhCN、SettingsLanguageNameEnUS。public static 强类型属性调用 Core/Resource 的 ResourceText，以所属 Type 定位资源；不承载共享产品名或模块私有文案。

### `Menus/MenuItemViewModel.cs`
`public class MenuItemViewModel`（第 13 行），自 Modules/Workstation 迁入（命名空间 `DigitalWorkstation.Core.Framework.Menus`）。菜单项呈现模型：`Title`（required init，:19）、`Icon: Geometry?`（:24，null 时弹出层内仍渲染空 `PathIcon` 占位对齐）、`Command: ICommand?`（:26）、`IsTopLevel: bool`（:32，顶层菜单不预留图标槽位）、`Children: ObservableCollection<object>`（:34，子项为 MenuItemViewModel 或 Avalonia `Separator`）；静态 `FromSubmenu(MenuTreeSubmenu, bool isTopLevel = false)`（:39）把建树器产物递归转换（`IconPath` 经 `StreamGeometry.Parse` 转几何，:49；分隔线转 `Separator`，:53；仅 shell 建根项时传 `isTopLevel: true`）。

### `Menus/MenuTreeEntry.cs`
菜单树的条目 record 族（[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md)）：`abstract record MenuTreeEntry`（第 10 行）；`MenuTreeItem(Title, IconPath?, Command)`（:15，叶子，标题已解析）；`MenuTreeSubmenu(Title, Children)`（:21，子菜单节点含顶层菜单，Children 分隔线已插好）；`MenuTreeSeparator`（:26，组间分隔线标记，私有构造单例 `Instance`）。

### `Menus/MenuTreeBuilder.cs`
`MenuTreeBuilder.Build(IEnumerable<IMenuItemContribution>)` 是纯建树函数，无资源查找。稳定 Path 分段索引节点，含空段记 Warning 跳过；末端第一个非 null PathTitle 提供标题，引用不占首位，未声明祖先显示 Id 直到后到声明命名。顶层按 NodeOrder/Title 排序；子菜单按 GroupOrder/Group/Order/Title 分组排序并插分隔线。LeafAccum 保存叶子，NodeAccum 保存子节点、条目、可空 DeclaredTitle；MergeNodeOrder/MergePlacement 保留既有最小位次规则。

### `Menus/MenuRegistration.cs`
`MenuRegistration.RegisterMenus(IContainerRegistry, Assembly)` 扫描菜单类，非法路径或方法签名记 Warning 跳过，注册类与每个合法方法的 singleton 工厂。同文件 internal `ReflectedMenuItemContribution` 首次解析时按方法来源查 Title、按类级标题声明查 PathTitle；`MenuGroup(path)` 只引用时 PathTitle=null。点击反射调用并等待 Task，异常记日志、不抛出。

### `Commands/CommandRegistration.cs`
`CommandRegistration.RegisterCommands(IContainerRegistry, Assembly)` 扫描方法级声明，无需类级标记；非法签名记 Warning 跳过，注册宿主类及每个合法方法的 singleton 工厂。internal `ReflectedCommandContribution` 启动准备时按显式 ResourceType 查 Title；默认 Id 为声明类全名+方法名，Gesture/IconPath/Order 透传。执行反射调用、等待 Task、异常记日志不抛出。

### `WindowManager/FrameworkWindowManager.cs`
`public class FrameworkWindowManager : IWindowManager, IMainWindowManager`（第 12 行，命名空间 `DigitalWorkstation.Core.Framework.WindowManager`）。内部状态 `_windowMap: Dictionary<Type, Window>`（:17）与 `_mainWindow`（:22）。入口：`GetWindow`（:35）、`InitializeWindow`（:45，私有）、`ShowWindow` 四重载（:57-111）、`ShowDialog` 四重载（:113-141）、`CloseWindow`（:144）、`HideWindow`（:150）、`HandleMainWindow`（:158）、`HideMainWindow`/`ShowMainWindow`（:169-170）、`CloseWindowsExceptMain`（:172）。

## 对应测试文件（UnitTest/Framework/）

```
UnitTest/Framework/
├── Framework.csproj                  xUnit 测试项目：Microsoft.NET.Test.Sdk 17.6.0 + xunit 2.4.2 + xunit.runner.visualstudio 2.4.5
└── ShellLayoutStateResizeTests.cs    11 个 [Fact]：Resize 增减/clamp/区域隔离/收起恢复保留尺寸
```

### Contributions/ShellContributionCatalog.cs

ShellContributionCatalog 从内部 descriptor 集合读取贡献，按批次可见性过滤；ShellContributionRegistration 提供统一登记扩展；ContributionBatch 负责 AsyncLocal 归属、登记锁和准备/提交/拒绝生命周期。该文件不处理普通 DI 注册回滚。

### Layout/ShellLayoutConfiguration.cs

Restore 从贡献和可选 DTO 构造规范状态；Capture 从状态生成版本 1 DTO。保留 MainContent、过滤失效 Id、恢复归属顺序、校验位置/对齐并 clamp 尺寸，避免 Workstation 重复解释配置。

### Layout/ShellLayoutMetrics.cs

统一卡片外边距、容器内边距、ActivityBar 外边距及列宽投影；FrameworkWindowTheme.axaml 与 Workstation 的列宽派生属性使用同一来源。

### Settings/SettingCatalog.cs

设置项 Id 冲突首个生效；分组首个资源/名称、最小 Order；隐式分组只来自有效项。查询不保留未提交批次的声明缓存，重复声明日志按实例去重。

Framework.csproj 将 Build/PluginSharedAssemblies.txt 作为固定名称资源嵌入，供 PluginLoadContext 与构建过滤共用共享清单。FrameworkApplication.cs 包含内置模块与插件的统一贡献准备/失败流程，插件以实际 Type 在容器初始化。
