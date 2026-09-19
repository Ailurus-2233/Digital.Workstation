# Workstation — 不变量与陷阱

## 隐含不变量

- **主页依赖在 ViewModel 注入**：`EmptyStateView()` 保持公开无参构造，避免 AVLN3001；通过 Prism AutoWireViewModel 和既有命名规则找到 `ViewModels.EmptyStateViewModel`。不要把 IModuleCatalog 再移回 View 构造，也不要用全局容器查找代替注入。ViewModel 构造不读取加载清单：启动时 Prism 尚未完成初始化，读取放在 View 挂载时调用的 Refresh。
- **主页分类使用实际类型与初始化状态**：仅处理 `ModuleState.Initialized` 的模块目录项，以已加载程序集完整身份解析 `ModuleType` 后检查宿主 `[Plugin]` 标记。Debug 插件也在默认加载上下文，不能按路径或上下文名称分类；不要改成默认 `Type.GetType` 导致 Release 插件解析丢失或再次加载 DLL。
- **主页条目是快照，不是导航状态**：三个列表元素均为 `LoadedComponentItem`，统一模板绑定 `Name` 与 `Description`，不是字符串叶子。挂载只清空树选择并刷新列表；右侧插件列表独立于左侧选择，不把树事件接入 Shell 状态或配置。
- **悬浮说明保留真实来源**：`AssemblyDescriptionAttribute` 非空时使用声明值，否则回退程序集或入口类型身份。模块目录与插件标记没有业务描述字段，不应从名称推测说明；程序集描述本身不由 Workstation 的资源系统翻译。
- **贡献收集只发生一次且晚于模块准备**：VM 构造不收集，由 PrepareShell → PrepareContributions 在 Ready 前调用 EnsureContributionsLoaded；只有成功末尾才设置标志。启动后动态增加贡献不会自动重建 Shell。
- **State 是布局事实来源**：所有布局转换结果交 ApplyLayout，同步宿主、四个集合、高亮与配置。PanelAlignment 仍在 State 外，但也由同一出口协调；不得新增局部 State=next + Sync 路径。
- **UI 线程亲和**：全部事件订阅（:48-51）、`ToolViewDragSession.ActiveChanged` 订阅（:53）、命令、集合变更都假定 UI 线程；模块从后台线程发布 `OpenMainViewEvent` 时由 Prism 事件聚合器的线程选项决定（默认订阅在发布线程执行——本模块订阅未指定 `ThreadOption`，发布方若在后台线程会直接碰 `ObservableCollection`，调用方责任）。
- **视图实例长期缓存、宿主归属必须唯一**：ApplyLayout 在安装任何目标内容前，先清空所有变化的旧宿主；重置和空面板也走相同路径。不要仅在 MoveTab 中针对被拖项脱离，重置同样会改变归属。当前没有内置工具视图，跨区视觉行为须使用临时贡献验证。
- **Id 是字符串级契约**：`OpenMainViewEvent` 负载必须逐字符等于 `IMainViewContribution.Id`。不匹配不报错，静默无反应。（菜单贡献已无 Id——[ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md) 后菜单项靠路径/分组/Order 定位。）
- **恢复规则归 Framework**：ShellLayoutConfiguration.Restore 统一恢复各 Bar 的有序 Id、活动项与尺寸；不要重新创建局部 LoadPanelTabs 去修改 State 和内容。
- **保存来自同一提交状态**：ApplyLayout(persist:true) 捕获 ShellLayoutConfiguration.Capture(next, alignment)；恢复/重置传 false。不要从 UI 集合再捕获 placements，也不要让新增动作绕开 ApplyLayout。
- **placements 不包含钉住项**：Framework Capture 只读取 State.ActivityBarItems 和两个面板 Tabs。钉住区按贡献默认位置呈现；保存的合法钉住选中 Id 可在恢复时保留，但钉住项不参与移动。
- **ResetLayout 保留主视图与缓存**：默认布局经 LoadToolViews(null) 和 ApplyLayout 提交，保留 State.MainContent 与 MainContent 实例；成功后 Delete 配置。不能只把 State 改为 Initial 而留下原 MainContent 或旧面板宿主。
- **防抖窗口内变更合并**：LayoutPersistence 委托 ConfigurationPersistence 的文件写入器。Delete 等待在途写入再作废 pending 并删除；普通退出统一最终保存和释放。

## 易错改法（看似合理但静默破坏行为）

1. **"修复" `PanelResizer.GetParentGrid` 返回 `base.GetParentGrid()`**：`PanelResizer` 已迁入 Framework（`Core/Framework/Layout/PanelResizer.cs:46-49`）——返回 `null` 是刻意的，`ResizeData` 为空使 GridSplitter 原生重排全部短路，只剩 DragDelta 事件（换算后经 `ResizeCommand` 出口）。一旦返回真实父 Grid，GridSplitter 会直接改 `ColumnDefinitions`/`RowDefinitions`，与布局模板里 `{Binding SideBarColumnWidth}`/`{Binding State.BottomPanel.Height}` 单向绑定打架，拖拽结果不可预测。
2. **`TogglePanelTarget` 新增枚举成员只改一半**：`MainWindowViewModel.TogglePanel`（:488-497）的 `_` 默认分支保持原 State——新 target 不会错误切换 BottomPanel，编译与运行均无告警；菜单侧则相反：`ViewPanelMenus` 不会自动出现新项，必须手工加一个 `[MenuItem]` 方法（旧版按枚举循环注册工厂的"自动覆盖"已随 [ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md) 消失）。改枚举时同步：该 switch、`Menus/ViewPanelMenus.cs` 加方法、`Commands/ViewCommands.cs` 加带 `Gesture` 的命令方法。
2b. **`PanelAlignment` 新增枚举成员只改一半**：菜单侧需手工在 `ViewAlignmentMenus` 加对应 `[MenuItem]` 方法（无默认分支兜底，静默缺项）；且 `FrameworkWindow.UpdateLayoutTemplate` 的兜底分支是 Center（详见 Framework 文档）。改枚举时两处同步：`Menus/ViewAlignmentMenus.cs`、Framework 侧模板与键映射。
3. **在 `MainWindowViewModel` 构造函数里收集贡献**：模块贡献在 Prism 模块初始化（晚于 shell 创建）才注册——由 PrepareShell 在模块准备完成后接线。提前收集会拿到空列表。
4. **给 `OpenMainView` 加"找不到就抛异常"**：当前契约是静默返回（:346-349），事件发布方（如 DashBoard 导航视图、shell"设置"导航按钮）没有错误处理路径；改语义要先看所有发布点。
5. **菜单分隔线手工插入或菜单方法乱签名**：分隔线由 Framework 的 `MenuTreeBuilder` 按分组自动生成（组间插入），不要在 ViewModel/XAML 手工插 `Separator`；`[MenuItem]` 方法必须是无参 `void`/`Task`——带参或返回值不合约的方法**编译不报错**，只在注册时记日志跳过，表现为菜单项静默缺席。**工具视图同理**（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）：`[ToolView]` 标在抽象类或非 `Control` 上、或同程序集 Id 重复，`ToolViewRegistration` 只记 `Logger.Warning` 跳过（`ToolViewRegistration.cs:31-44`），编译不报错、条目静默缺席；`TitleKey` 是 ResourceType 所指定的资源键不是标题本身，写错键名会显示键名（`ResourceText.Get` 缺键回退）。
6. **菜单项模板/样式在 MainWindow.axaml 里找**：菜单栏已随 [ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md) 迁入 Framework——`Menu` 实例与项模板由 `FrameworkWindow` 构造函数在代码中创建（`Core/Framework/Windows/FrameworkWindow.cs:47-52`），容器 `MenuItem` 的 `ItemsSource`/`Command`/`AutomationProperties.Name` 经 `Core/Framework/Windows/FrameworkWindowTheme.axaml:621-625` 的样式 setter 绑定（ItemTemplate 只控制 Header 内容）。本模块不再涉及菜单呈现，改菜单样式/模板去 Framework。
7. **给 Framework 的 `FrameworkWindowTheme.axaml` 加 `x:Class` 配 code-behind**：Framework 项目里 Avalonia.Generators 不为它产出 `InitializeComponent`——该主题经 `FrameworkWindowTheme.cs`（:16-24）的 `StyleInclude` 从编译进程序集的 axaml 资源加载（与 Semi/Ursa 主题同款机制），构造时强制 `Loaded`。加 `x:Class` 指望生成器只会编译失败或加载落空；主题扩展走 `StyleInclude`/`Styles` 体系。
8. **重命名 ViewModel 的命令/属性后只看编译结果**：布局模板（`FrameworkWindowTheme.axaml`）与内置菜单栏（`Core/Framework/Windows/FrameworkWindow.cs:39` 的 `new Binding("MenuBarItems")`）里全部是宽松反射绑定（Framework 不引用 `MainWindowViewModel` 类型，`SelectActivityCommand`、`ResizePanelCommand`、`OpenSettingsCommand`、`SettingsIcon`/`SettingsTitle`、`SideBarColumnWidth`/`AuxiliaryColumnWidth`、`State.*`、`MenuBarItems` 等都无编译期检查）。重命名或改签名会**静默失效**（运行时绑定错误日志，界面无反应），改完必须对照 Framework 侧全部绑定点核一遍。

## 历史踩坑（代码注释/防御性代码透露）

- `MainWindow.PrepareContributions` 在 Ready 前初始化；不要移回构造或 Opened。
- `Core/Framework/Layout/PanelResizer.cs:8-12、42-45` 两段注释详述禁用原生重排的原因与机制——说明"分隔条拖动直接改 Grid 行列"是需要主动防的行为。
- `Core/Framework/Windows/FrameworkWindowTheme.axaml:221-224` 注释：卡片间隙恒 4px 的合成规则（容器 Padding 2 + 卡片 Margin 2）、分隔条 8px 热区负边距覆盖间隙并压两侧卡片边缘——改任一边距都会破坏对齐。
- `Core/Framework/Windows/FrameworkWindowTheme.axaml:281`（BottomPanel 分隔条 `Margin="0,-4,0,0"`）与 `:261`（AuxiliaryPanel `Margin="-4,0,0,0"`）：负边距外探方向各不同，照抄会错位。
- `Core/Framework/Windows/FrameworkWindowTheme.axaml:615` 注释：顶层菜单弹出层 `VerticalOffset=-8` 是为抵消模板内边距 8 与默认偏移 -4 后的 4px 缝——像素级调过的坑（该样式已随菜单栏从 MainWindow.axaml 迁入 Framework）。
- `Menus/FileMenus.cs:8-11` 注释："`退出`归入 Application 组（GroupOrder 1000）保持在文件菜单末尾，模块贡献的组排在其前"——模块贡献的组 GroupOrder 应小于 1000，改小会破坏菜单布局约定。
- `Views/AboutWindow.axaml` 产品名使用 `SharedResources.ProductName`，窗口标题使用 `WorkstationResources.AboutWindowTitle`；正文「模块化 Avalonia 桌面工作站」仍为硬编码中文，未纳入本次资源迁移。
- 旧版"视图菜单显隐组与对齐组之间手工插 Separator"的特判已随 [ADR-0001](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0001-attribute-menu-registration.md) 删除——分隔线由建树器按分组生成，见上"易错改法 5"。工具视图的两套旧贡献接口（`IPanelTabContribution`/`INavigationItemContribution`）与手写贡献类已随 [ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md) 整体删除——不要再新建 `XxxPanelTab`/`XxxNavigationItem` 贡献类，声明归 `[ToolView]`（见 common.md 修改场景 3）。

## 本地化边界

稳定菜单路径/工具视图 Id 与显示资源键分别维护，带文案的 attribute 必须显式指定 owner；仅挂接已有菜单的 MenuGroup 则只指定稳定路径；新私有文案放本模块 Resources，不向共享产品资源添加别名。资源重命名同时修改 facade、两份 resx 和 XAML/nameof 引用；资源清单缺失不等于普通缺键。
