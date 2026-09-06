# Workstation — 不变量与陷阱

## 隐含不变量

- **贡献收集只发生一次且晚于构造**：`MainWindowViewModel` 构造函数不读任何贡献；`EnsureContributionsLoaded()`（MainWindowViewModel.cs:102-127）由 `MainWindow.axaml.cs:42` 的 `Opened` 事件触发，`_contributionsLoaded`（:25）守卫保证只执行一次。推论：**窗口首次显示之后才注册进容器的贡献永远不会出现**；也不能指望构造函数里就有导航项。
- **`State` 是布局的唯一事实来源**：所有显隐/宽高/选中/tab 状态都必须经 `ShellLayoutState` 转换方法改（`State = State.Xxx(...)`），不许另存布尔或宽度字段。XAML 全部单向绑定 `State.*`（如 `MainWindow.axaml:242、244、282-283、345、347`）。`ResizePanel`（:241-244）是拖拽增量进入状态的唯一路径；`TogglePanel`（:249-257）是显隐切换的唯一路径。
- **UI 线程亲和**：全部事件订阅（:32-33）、命令、集合变更都假定 UI 线程；模块从后台线程发布 `OpenMainViewEvent` 时由 Prism 事件聚合器的线程选项决定（默认订阅在发布线程执行——本模块订阅未指定 `ThreadOption`，发布方若在后台线程会直接碰 `ObservableCollection`，调用方责任）。
- **视图实例寿命 = 应用寿命**：五个 `*Contents` 缓存字典（:19-24）永不失效、无 Dispose 路径；视图应假设自己会被长期持有、反复进出可视树。
- **Id 是字符串级契约**：`OpenMainViewEvent` 负载必须逐字符等于 `IMainViewContribution.Id`；`TogglePanelContribution.Id` 由 `Target.ToString().ToLowerInvariant()` 派生（TogglePanelContribution.cs:27）。不匹配不报错，静默无反应。
- **`LoadPanelTabs` 假设列表同面板**：`LoadPanelTabs`（:271-302）用 `contributions[0].Panel`（:289）决定更新 `AuxiliaryPanel` 还是 `BottomPanel` 状态——依赖调用方已按 `PanelPlacement` 过滤（`ShellContributionCollector.GetPanelTabs` 保证），混入另一面板的 tab 会静默写错状态分支。

## 易错改法（看似合理但静默破坏行为）

1. **"修复" `PanelResizer.GetParentGrid` 返回 `base.GetParentGrid()`**：`PanelResizer.cs:20-23` 返回 `null` 是刻意的——`ResizeData` 为空使 GridSplitter 原生重排全部短路，只剩 DragDelta 事件。一旦返回真实父 Grid，GridSplitter 会直接改 `ColumnDefinitions`/`RowDefinitions`，与 `Width="{Binding State.SideBar.Width}"` 单向绑定打架，拖拽结果不可预测。
2. **`TogglePanelTarget` 新增枚举成员不加 case**：三处 switch 的默认分支会静默接管——
   - `MainWindowViewModel.TogglePanel`（:251-256）：`_ => State.ToggleBottomPanel()`，新 target 实际切换 BottomPanel；
   - `TogglePanelContribution.Title`（:29-34）/ `IconPath`（:36-41）：`_ =>` BottomPanel 文案与 `Icons.PanelBottom`；
   - `TogglePanelContribution.Order`（:43-48）：`_ => 30`。
   即新成员表现为"视图菜单里多一个叫'切换 BottomPanel'、Order 30、点击切换 BottomPanel 的项"，编译与运行均无任何告警。改枚举时必须同步这三处（另加 `WorkstationApplication.cs:37` 的注册循环与 `MainWindow.axaml` 快捷键）。
3. **在 `MainWindowViewModel` 构造函数里收集贡献**：模块贡献在 Prism 模块初始化（晚于 shell 创建）才注册——这正是 `OnOpened` 注释（MainWindow.axaml.cs:41）与 `EnsureContributionsLoaded` 注释（:99-101）说明的时序。提前收集会拿到空列表。
4. **给 `OpenMainView` 加"找不到就抛异常"**：当前契约是静默返回（:161-164），事件发布方（如 DashBoard 导航视图）没有错误处理路径；改语义要先看所有发布点。
5. **改 `WorkstationApplication.cs:37` 的注册循环为一次性注册**：`TogglePanelContribution` 是有状态的（每实例固定 `Target`），三个 target 必须三个实例；共享单例会让三个菜单项都切同一个面板。
6. **菜单项 `Command` 改到 ItemTemplate 里绑定**：`MainWindow.axaml:137-140` 用样式 setter 给容器生成的 `MenuItem` 绑 `Command`（因为 `MenuItem` 容器由 `ItemsSource` 生成，ItemTemplate 只控制 Header 内容），挪进 `MenuItemHeaderTemplate` 会绑不上。

## 历史踩坑（代码注释/防御性代码透露）

- `MainWindow.axaml.cs:41` 注释："模块贡献在 Prism 模块初始化（晚于 shell 创建）时才注册，首次显示时再收集"——曾踩过收集时机坑。
- `PanelResizer.cs:5-8、16-19` 两段注释详述禁用原生重排的原因与机制——说明"分隔条拖动直接改 Grid 行列"是需要主动防的行为。
- `MainWindow.axaml:132-135` 注释：顶层菜单弹出层 `VerticalOffset=-8` 是为抵消模板内边距 8 与默认偏移 -4 后的 4px 缝——像素级调过的坑。
- `MainWindow.axaml:218-219、251` 注释：卡片间隙恒 4px 的合成规则（容器 Padding 2 + 卡片 Margin 2）、分隔条 8px 热区负边距覆盖间隙并压两侧卡片边缘——改任一边距都会破坏对齐。
- `MainWindow.axaml:264`（BottomPanel 分隔条 `Margin="0,-4,0,0"`）与 `:395`（AuxiliaryPanel `Margin="-4,0,0,0"`）：负边距外探方向各不同，照抄会错位。
- `ExitMenuItem.cs:11-13` 注释："Order 取大值保持在文件菜单末尾"（Order=100）——模块贡献项应排在它前面，改小会破坏菜单布局约定。
- `Views/AboutWindow.axaml` 标题与正文为**硬编码中文**（"关于 Digital.Workstation"、"模块化 Avalonia 桌面工作站"），未走 `Language` 本地化——与模块内其他文案全部走 `Language.*` 的惯例不一致，en-US 环境下关于窗口仍显示中文。
- `TogglePanelContribution` 的 switch 结构不对称（Title/IconPath 的显式 case 是 SideBar/AuxiliaryPanel，Order 的显式 case 是 SideBar/BottomPanel）——不是笔误但极易误读，见上"易错改法 2"。
