# Workstation — 不变量与陷阱

## 隐含不变量

- **贡献收集只发生一次且晚于构造**：`MainWindowViewModel` 构造函数不读任何贡献；`EnsureContributionsLoaded()`（MainWindowViewModel.cs:170-193）由 `MainWindow.axaml.cs:13-17` 的 `Opened` 事件触发，`_contributionsLoaded`（:38）守卫保证只执行一次。推论：**窗口首次显示之后才注册进容器的贡献永远不会出现**；也不能指望构造函数里就有导航项。
- **`State` 是布局的唯一事实来源（其管辖范围内）**：所有显隐/宽高/选中/tab/拖拽迁移状态都必须经 `ShellLayoutState` 转换方法改（`State = State.Xxx(...)`），不许另存布尔或宽度字段。布局 XAML 全部单向绑定 `State.*` 或其派生属性（现位于 Framework 的 `FrameworkWindowTheme.axaml`，如 :65、:93、:149——后两个绑 `AuxiliaryPanelRevealed`/`BottomPanelRevealed` 派生属性，拖拽会话期间临时显露隐藏面板，ADR-0002）。`ResizePanel`（:429-432）是分隔条增量进入状态的唯一路径；`TogglePanel`（:488-497）是显隐切换的唯一路径；`MoveTab`（:440-483）是拖拽落放的唯一路径。**例外：面板对齐档位不在 `State` 里**——`FrameworkWindow.PanelAlignment` 依赖属性是布局定义的唯一入口，`MainWindowViewModel.PanelAlignment`（:85）只是双向绑定的镜像属性。
- **UI 线程亲和**：全部事件订阅（:48-51）、`ToolViewDragSession.ActiveChanged` 订阅（:53）、命令、集合变更都假定 UI 线程；模块从后台线程发布 `OpenMainViewEvent` 时由 Prism 事件聚合器的线程选项决定（默认订阅在发布线程执行——本模块订阅未指定 `ThreadOption`，发布方若在后台线程会直接碰 `ObservableCollection`，调用方责任）。
- **视图实例寿命 = 应用寿命**：`_toolViewContents`（工具视图统一内容缓存，ADR-0002，:37）与 `_mainViewContents`（:27）永不失效、无 Dispose 路径；视图应假设自己会被长期持有、反复进出可视树（`ResetLayout` 也不清这两个缓存，:503-521）。跨 Bar 迁移时实例随 tab 走：`MoveTab` 先把被拖内容从 `SideBarContent`/`AuxiliaryContent`/`BottomContent` 置空脱离源视觉树（:454-468），再挂到目标——**同一 Control 不能同时挂两棵视觉树**，省略脱离步骤会在目标 ContentControl 呈现时抛"已有父级"异常。
- **Id 是字符串级契约**：`OpenMainViewEvent` 负载必须逐字符等于 `IMainViewContribution.Id`。不匹配不报错，静默无反应。（菜单贡献已无 Id——ADR-0001 后菜单项靠路径/分组/Order 定位。）
- **`LoadPanelTabs` 假设列表同面板**：`LoadPanelTabs`（:586-618）的目标面板由显式传入的 `panel` 参数决定（switch 在 :605-615）——依赖调用方已按目标 bar 过滤（`LoadToolViews` 的 `MovableIn(bar)` 局部函数保证，:209-219），混入另一面板的工具视图会静默写错状态分支。
- **布局变更必须经过 `ScheduleSave` 才持久化**：七个变更点（`SelectActivity` :295、`ActivateAuxTab` :375、`ActivateBottomTab` :392、`SetPanelAlignment` :420、`ResizePanel` :432、`MoveTab` :482、`TogglePanel` :496）末尾统一调 `ScheduleSave()`（:578-581）经 Framework `LayoutPersistence` 防抖 500ms 落盘。**新增任何布局变更路径（如面板 tab 拖拽排序、新命令）时忘记挂上，该变更就静默不持久化**——重启后回到上次落盘状态，无报错。反过来，纯展示性变更（如 `OpenMainView` 换主视图、`OpenSettings` 打开设置页）本就不入 DTO，不要随手加。
- **`placements` 不含钉住项**：`CaptureLayout`（:527-573）只遍历 `TopNavigationItems`/`AuxiliaryTabs`/`BottomTabs` 三个可变集合，钉住项（`AllowMove=false`）恒由 `LoadToolViews` 按贡献迭代落到 ActivityBar 底部段（:225-226），不入表也不受配置影响——当前无内置钉住项实例（原"设置"钉住项已删除，ADR-0006 决策 6），机制保留；不要为钉住项手写 placements 条目指望它生效。
- **`ResetLayout` 依赖 `_toolViews` 缓存，时序天然安全**：`ResetLayout`（:503-521）全默认重建走 `LoadToolViews(null)`，其数据源是 `EnsureContributionsLoaded` 时缓存的 `_toolViews` 字段（:39、:178）。因为"重置布局"菜单项本身就是菜单贡献、只在贡献装载完成后才存在，事件不可能先于 `_toolViews` 赋值到达——不要在构造函数里给 `ResetLayout` 加"兜底再收集"逻辑，那会破坏"只收集一次"不变量。
- **防抖窗口内连续变更只落最后一次**：`LayoutPersistence.ScheduleSave` 是 500ms `Timer` 防抖，连续拖拽/连点只落最终态（这是特性）；`Delete()`（重置路径）会先停 Timer、清 pending 再删文件，避免 pending 回调在删完后又把文件写回。

## 易错改法（看似合理但静默破坏行为）

1. **"修复" `PanelResizer.GetParentGrid` 返回 `base.GetParentGrid()`**：`PanelResizer` 已迁入 Framework（`Core/Framework/Layout/PanelResizer.cs:46-49`）——返回 `null` 是刻意的，`ResizeData` 为空使 GridSplitter 原生重排全部短路，只剩 DragDelta 事件（换算后经 `ResizeCommand` 出口）。一旦返回真实父 Grid，GridSplitter 会直接改 `ColumnDefinitions`/`RowDefinitions`，与布局模板里 `{Binding SideBarColumnWidth}`/`{Binding State.BottomPanel.Height}` 单向绑定打架，拖拽结果不可预测。
2. **`TogglePanelTarget` 新增枚举成员只改一半**：`MainWindowViewModel.TogglePanel`（:488-497）的 `_` 默认分支调 `State.ToggleBottomPanel()`——新 target 实际切换 BottomPanel，编译与运行均无告警；菜单侧则相反：`ViewPanelMenus` 不会自动出现新项，必须手工加一个 `[MenuItem]` 方法（旧版按枚举循环注册工厂的"自动覆盖"已随 ADR-0001 消失）。改枚举时同步：该 switch、`Menus/ViewPanelMenus.cs` 加方法、`Commands/ViewCommands.cs` 加带 `Gesture` 的命令方法。
2b. **`PanelAlignment` 新增枚举成员只改一半**：菜单侧需手工在 `ViewAlignmentMenus` 加对应 `[MenuItem]` 方法（无默认分支兜底，静默缺项）；且 `FrameworkWindow.UpdateLayoutTemplate` 的兜底分支是 Center（详见 Framework 文档）。改枚举时两处同步：`Menus/ViewAlignmentMenus.cs`、Framework 侧模板与键映射。
3. **在 `MainWindowViewModel` 构造函数里收集贡献**：模块贡献在 Prism 模块初始化（晚于 shell 创建）才注册——这正是 `OnOpened` 注释（MainWindow.axaml.cs:15）与 `EnsureContributionsLoaded` 注释（:166-169）说明的时序。提前收集会拿到空列表。
4. **给 `OpenMainView` 加"找不到就抛异常"**：当前契约是静默返回（:346-349），事件发布方（如 DashBoard 导航视图、shell"设置"导航按钮）没有错误处理路径；改语义要先看所有发布点。
5. **菜单分隔线手工插入或菜单方法乱签名**：分隔线由 Framework 的 `MenuTreeBuilder` 按分组自动生成（组间插入），不要在 ViewModel/XAML 手工插 `Separator`；`[MenuItem]` 方法必须是无参 `void`/`Task`——带参或返回值不合约的方法**编译不报错**，只在注册时记日志跳过，表现为菜单项静默缺席。**工具视图同理**（ADR-0002）：`[ToolView]` 标在抽象类或非 `Control` 上、或同程序集 Id 重复，`ToolViewRegistration` 只记 `Logger.Warning` 跳过（`ToolViewRegistration.cs:31-44`），编译不报错、条目静默缺席；`TitleKey` 是 Language 资源键不是标题本身，写错键名会显示键名（`Language.Get` 缺键回退）。
6. **菜单项模板/样式在 MainWindow.axaml 里找**：菜单栏已随 ADR-0001 迁入 Framework——`Menu` 实例与项模板由 `FrameworkWindow` 构造函数在代码中创建（`Core/Framework/Windows/FrameworkWindow.cs:47-52`），容器 `MenuItem` 的 `ItemsSource`/`Command`/`AutomationProperties.Name` 经 `Core/Framework/Windows/FrameworkWindowTheme.axaml:621-625` 的样式 setter 绑定（ItemTemplate 只控制 Header 内容）。本模块不再涉及菜单呈现，改菜单样式/模板去 Framework。
7. **给 Framework 的 `FrameworkWindowTheme.axaml` 加 `x:Class` 配 code-behind**：Framework 项目里 Avalonia.Generators 不为它产出 `InitializeComponent`——该主题经 `FrameworkWindowTheme.cs`（:16-24）的 `StyleInclude` 从编译进程序集的 axaml 资源加载（与 Semi/Ursa 主题同款机制），构造时强制 `Loaded`。加 `x:Class` 指望生成器只会编译失败或加载落空；主题扩展走 `StyleInclude`/`Styles` 体系。
8. **重命名 ViewModel 的命令/属性后只看编译结果**：布局模板（`FrameworkWindowTheme.axaml`）与内置菜单栏（`Core/Framework/Windows/FrameworkWindow.cs:39` 的 `new Binding("MenuBarItems")`）里全部是宽松反射绑定（Framework 不引用 `MainWindowViewModel` 类型，`SelectActivityCommand`、`ResizePanelCommand`、`OpenSettingsCommand`、`SettingsIcon`/`SettingsTitle`、`SideBarColumnWidth`/`AuxiliaryColumnWidth`、`State.*`、`MenuBarItems` 等都无编译期检查）。重命名或改签名会**静默失效**（运行时绑定错误日志，界面无反应），改完必须对照 Framework 侧全部绑定点核一遍。

## 历史踩坑（代码注释/防御性代码透露）

- `MainWindow.axaml.cs:15` 注释："模块贡献在 Prism 模块初始化（晚于 shell 创建）时才注册，首次显示时再收集"——曾踩过收集时机坑。
- `Core/Framework/Layout/PanelResizer.cs:8-12、42-45` 两段注释详述禁用原生重排的原因与机制——说明"分隔条拖动直接改 Grid 行列"是需要主动防的行为。
- `Core/Framework/Windows/FrameworkWindowTheme.axaml:221-224` 注释：卡片间隙恒 4px 的合成规则（容器 Padding 2 + 卡片 Margin 2）、分隔条 8px 热区负边距覆盖间隙并压两侧卡片边缘——改任一边距都会破坏对齐。
- `Core/Framework/Windows/FrameworkWindowTheme.axaml:281`（BottomPanel 分隔条 `Margin="0,-4,0,0"`）与 `:261`（AuxiliaryPanel `Margin="-4,0,0,0"`）：负边距外探方向各不同，照抄会错位。
- `Core/Framework/Windows/FrameworkWindowTheme.axaml:615` 注释：顶层菜单弹出层 `VerticalOffset=-8` 是为抵消模板内边距 8 与默认偏移 -4 后的 4px 缝——像素级调过的坑（该样式已随菜单栏从 MainWindow.axaml 迁入 Framework）。
- `Menus/FileMenus.cs:8-11` 注释："`退出`归入 Application 组（GroupOrder 1000）保持在文件菜单末尾，模块贡献的组排在其前"——模块贡献的组 GroupOrder 应小于 1000，改小会破坏菜单布局约定。
- `Views/AboutWindow.axaml` 标题与正文为**硬编码中文**（"关于 Digital.Workstation"、"模块化 Avalonia 桌面工作站"），未走 `Language` 本地化——与模块内其他文案全部走 `Language`（属性调用或 attribute 资源键）的惯例不一致，en-US 环境下关于窗口仍显示中文。
- 旧版"视图菜单显隐组与对齐组之间手工插 Separator"的特判已随 ADR-0001 删除——分隔线由建树器按分组生成，见上"易错改法 5"。工具视图的两套旧贡献接口（`IPanelTabContribution`/`INavigationItemContribution`）与手写贡献类已随 ADR-0002 整体删除——不要再新建 `XxxPanelTab`/`XxxNavigationItem` 贡献类，声明归 `[ToolView]`（见 common.md 修改场景 3）。
