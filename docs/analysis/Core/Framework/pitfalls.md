# Framework — 不变量与陷阱

## 隐含不变量

1. **启动序列依赖三个"空覆盖"，缺一不可**（`FrameworkApplication.cs`）：
   - `OnInitialized()`（:48）必须保持为空——base 会在框架初始化阶段直接显示 MainWindow；
   - `InitializeModules()`（:55）必须保持为空——base 会同步一次性加载全部模块；
   - `OnFrameworkInitializationCompleted()`（:39）**不调用 base**——base 会把尚未完成模块加载的 MainWindow 直接设为桌面生命周期主窗口（第 37 行注释）。
   任何一处"顺手补上 base 调用"都会让主窗口在模块加载完成前出现，启动台与失败决策机制失效。
2. **调用顺序约束**：`HandleMainWindow()`（`FrameworkWindowManager.cs:158`）必须先于一切 `ShowWindow`/`ShowDialog`——启动序列在阶段 1 第一步就调它（`FrameworkApplication.cs:76`），随后才显示启动台（第 77 行）。模块代码在任何窗口操作前都依赖这个时序。
3. **`IoC.Initialize` 恰好一次**（`FrameworkApplication.cs:148`）：重复调用抛 `InvalidOperationException`；之前访问 `IoC.Provider` 得 null。`RegisterFrameworkServices` 是进程内唯一调用点，新增第二个调用点会直接炸。
4. **窗口类型单实例**：`_windowMap: Dictionary<Type, Window>`（`FrameworkWindowManager.cs:17`）以运行时类型为键，同类型窗口同时只能存在一个注册实例；再次 Show 前必须等上一个实例触发 `Closing`（事件处理器在第 50 行把类型移出映射）。`CloseWindow`/`HideWindow` 按类型索引的前提也由此而来。
5. **`HandleMainWindow` 只能调一次**：其实现（第 158-167 行）在 `_mainWindow != null && !_windowMap.ContainsKey(type)` 时登记，**否则抛 `InvalidOperationException`**——第二次调用时主窗口已在映射中，走 else 分支抛错。不要把它当幂等的"刷新主窗口引用"用。
6. **`ShowDialog` 要求主窗口活跃**：`_mainWindow is { IsActive: true }`（第 127、137 行）才允许弹模态；主窗口被 Hide 期间弹对话框会抛异常，而 `ShowWindow` 只要求主窗口非 null。
7. **布局状态必须整体替换**：`ShellLayoutState` 所有转换返回新实例（非法操作返回 `this`）；消费方若丢弃返回值（`state.Resize(...)` 不赋值回 `_state`）改动静默丢失。`Modules/Workstation/MainWindowViewModel.cs:60` 的 `[ObservableProperty] _state` 是唯一的当前实例持有者。
8. **UI 线程亲和性**：`FrameworkWindowManager` 的 Show/Hide/Close 与 `ShellContributionCollector` 的容器解析都假定在 UI 线程调用；模块加载被刻意 `Task.Run` 移出 UI 线程（`FrameworkApplication.cs:92`），模块 `Initialize` 里直接操作窗口需自行切回 UI 线程。`SettingsService` 的声明默认值容器解析同样假定 UI 线程（`Settings/SettingsService.cs:15` 注释）。
9. **`SideBarState.Visible` 默认 false，两个面板默认 true**：初始布局里 SideBar 收起、AuxiliaryPanel/BottomPanel 展开（`SideBarState.cs:11`、`AuxiliaryPanelState.cs:11`、`BottomPanelState.cs:11`）；改默认值会改变首屏布局且现有测试以 `Initial` 为基准。

## 易错改法

- **在 `Resize` 里"顺便"重置其他区域**：`Resize` 的不变量是只动目标区域（测试 `ResizeSideBar_LeavesOtherRegionsUntouched`、`ResizeBottomPanel_LeavesWidthsUntouched` 显式断言）；看似无害的"归一化"会破坏收起/展开后尺寸保留的语义。
- **把 `ActivateAuxTab`/`ActivateBottomTab` 的"拒绝"改成"自动展开面板再激活"**：注释明确"面板收起时拒绝（状态不变）"（`ShellLayoutState.cs:77、90`），消费方 `MainWindowViewModel.ActivateAuxTab`（`Modules/Workstation/MainWindowViewModel.cs:365`）依赖此行为直接返回；改成自动展开会改变点击已隐藏 tab 的 UX 契约。`MoveTab` 不受此限——拖拽落放是显式迁移，目标面板强制展开（`ShellLayoutState.cs:109`）。
- **`SelectActivity` 收起时清空 `SelectedActivity` 或 `ContentFor`**：设计上收起时两者都保留（`ShellLayoutState.cs:14-15`、`SideBarState.cs:15-17`），恢复展开后内容与选中项不丢；清空会导致重新展开后 SideBar 空白。注意 `MoveTab` 把选中项拖出 ActivityBar 时清空/改选是另一套语义（顶部段有项→改选前一项，拖空→收起），与此不冲突。
- **给 `CloseWindow(Type)` 加"未命中抛异常"**：它当前是刻意的静默 no-op（`FrameworkWindowManager.cs:144-148`），与 `HideWindow` 的抛异常语义不对称——`CloseWindowsExceptMain` 等路径依赖静默语义，对齐两者前先查调用点。
- **`ShowWindow(Window, object)`/`ShowDialog(Window, object)` 抛异常后窗口仍处注册态**：这两个重载的顺序是 `InitializeWindow` 注册 → 赋 `DataContext` → 检查主窗口（`FrameworkWindowManager.cs:98-111、133-141`）；主窗口缺失/不活跃抛 `InvalidOperationException` 时，窗口已留在 `_windowMap` 且 DataContext 已赋值，**无回滚**。调用方 catch 后若换个类型重试无妨，但若之后 `CloseWindow(type)` 会关掉这个从未显示的窗口；同类型再次 Show 前必须等其 `Closing` 触发移除。
- **在 `RegisterTypes` 之外注册框架服务或在子类重写 `RegisterTypes`**：注释明确"子类不需要重写此方法"（`FrameworkApplication.cs:183-185`），子类入口是 `RegisterCustomService`；重写 `RegisterTypes` 且不调 base 会丢掉 `IoC.Initialize` 与窗口管理器注册，整个应用起不来。
- **View/ViewModel 命名或目录偏离约定**：`ConfigureViewModelLocator`（第 237-269 行）只做字符串替换与后缀补全，通过 View 实际程序集查询，解析不到返回 null（不抛异常）——ViewModel 静默不绑定，界面空白无报错。`Replace("Views", "ViewModels")` 会替换 FullName 中**所有**出现的 "Views"，命名空间里多处含 "Views" 时结果可能意外。
- **改 `WaitForFailureActionAsync` 去掉 `RunContinuationsAsynchronously`**（第 124 行）：续体会在发布者（启动台 UI 线程）上下文内联执行，可能死锁；去掉 `Unsubscribe`（第 127 行）则每次失败累积一个订阅，第二次失败时旧订阅先 `TrySetResult` 已被释放的 completion（虽无害但泄漏订阅）。
- **新模块忘记在 `RegisterTypes` 调 `RegisterMenus`**：`MenuRegistration.RegisterMenus`（`Menus/MenuRegistration.cs:20`）只扫**调用方传入的那一个程序集**，刻意不做全局扫描（:16-19 注释）；新模块写了 `[MenuGroup]`/`[MenuItem]` 菜单类但没加 `containerRegistry.RegisterMenus(typeof(XxxModule).Assembly)`（真实调用点 `Modules/Workstation/WorkstationApplication.cs:31`），菜单**静默缺失**——无任何日志、无异常，建树时容器里根本没有对应的 `IMenuItemContribution`。
- **新模块忘记在 `RegisterTypes` 调 `RegisterToolViews`**：同构的陷阱（[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)）——`ToolViewRegistration.RegisterToolViews`（`Contributions/ToolViewRegistration.cs:20`）同样只扫调用方传入的程序集、不做全局扫描（:12 注释）；View 类标了 `[ToolView]` 但模块没调它，工具视图**静默缺失**（真实调用点 `Modules/Workstation/WorkstationApplication.cs:27`、`Modules/DashBoard/DashBoardModule.cs:12`）。被扫到但不合法也只是记一条 `Logger.Warning` 跳过：类非可实例化 `Control`（:31-36）、`Id` 在程序集内重复（:38-44）。排查"工具视图没出现"先翻日志的 Warning。
- **不要绕过贡献登记入口**：收集器现在只读取 RegisterShellContribution 登记的 descriptor；零登记返回空集合，不触发 DryIoc 具体类兜底。直接 RegisterSingleton<TContribution> 不会成为 Shell 贡献，也不能用于恢复旧的 Resolve<IEnumerable<TContribution>>() 路径；后者会绕过失败批次隔离。
- **新模块忘记在 `RegisterTypes` 调 `RegisterCommands`**：同构陷阱的第三例（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)）——`CommandRegistration.RegisterCommands`（`Commands/CommandRegistration.cs:19`）同样只扫调用方传入的程序集、不做全局扫描；方法标了 `[Command]` 但模块没调它，命令**静默缺失**（真实调用点 `Modules/Workstation/WorkstationApplication.cs:33`）。签名规则同菜单：只取 `Public | Instance | DeclaredOnly` 方法（静态/非公共连候选都进不了），带参/返回值非法记 `Logger.Warning` 跳过。
- **新模块忘记在 `RegisterTypes` 调 `RegisterSettings`**：同构陷阱的第四例（[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md)）——`SettingRegistration.RegisterSettings`（`Settings/SettingRegistration.cs:19`）同样只扫调用方传入的程序集、不做全局扫描（:11 注释）；属性标了 `[SettingItem]` 但模块没调它，设置项**静默缺失**——收集器里根本没有对应的 `SettingItemContribution`，设置页不显示、`ISettingsService.Get` 走「未声明」分支记 Warning 返回 `default`（Framework 自身「常规/语言」的调用点 `FrameworkApplication.cs:168`）。被扫到但不合法也只是记一条 `Logger.Warning` 跳过：属性无 getter、非 `Public | Static | DeclaredOnly` 连候选都进不了、`DefaultValue` 类型与属性类型不匹配（`SettingRegistration.cs:38-51`）。
- **语言应用必须先于文本实例化**：`ApplyLanguageSetting` 在框架服务注册中先加载设置、切换当前/默认线程区域性，再以 SharedResources.ProductName 设置 Application.Name；必须先于模块 RegisterTypes。工具视图扫描时解析，菜单/命令 singleton 启动准备时解析；设置只保存来源/键，设置页构造时才解析。提前在 Application 构造函数读产品名会固定为进程初始语言；只设 CurrentUICulture 则不能覆盖 Task.Run 模块加载线程。语言修改下次启动生效，重建菜单树不重新翻译旧贡献。
- **菜单挂接不等于标题所有权**：外部模块用 `[MenuGroup("shell.file")]` 挂接，无需引用 WorkstationResources 或复制 shell 标题；声明所有权时才传完整资源类型/键对。PathTitle=null 不占首个标题位置；末端第一个非 null 标题独立于最小位次合并，隐式祖先保留 Id 直到自身声明到达。别把引用先到误认成「标题已声明」。
- **设置组按 Id 而不是名称合并**：Group 引用稳定 Id，同 Id 保留首个声明来源与名称、Order 取最小；不同 Id 即便键/译文相同也不能合并。无声明组显示 Id、ResourceType=null，不把它当全局资源键。项与枚举名称使用项自己的来源，枚举缺键显示完整组合键；不得回退搜索其他模块。
- **写入回调的异常必须就地处理**：设置和布局现在统一走 DebouncedJsonFile.SavePending；序列化、I/O 或提交失败记 ConfigurationPersistence Warning 并保留 pending，不能让 Timer 异常逃逸。旧目标文件只在临时文件完整写入后替换。
- **手改 settings.json 枚举值写错形态**：`UiLanguage` 落盘值必须是 `"zh-CN"`/`"en-US"`（`JsonStringEnumMemberName`，`Settings/UiLanguage.cs:17、23`），写 `"ZhCN"` 或 `"0"` 这类形态会在 `Get` 反序列化时抛 `JsonException`——但与 layout.json 的**整份丢弃**不同，设置是**逐项回退**：只有那一项记 Warning 回退声明默认值，其余已存值不受影响（`SettingsService.cs:103-112`）。
- **生命周期收尾只挂真正的 Exit**：FrameworkApplication 统一 Dispose 配置 owner，正常关闭窗口、文件菜单退出与 Shutdown 都获得最终保存。Closing/ShutdownRequested 可以取消，不能在那里 Dispose，否则取消后继续修改会抛 ObjectDisposedException。重启先调用 owner.FlushPending，失败不启动新进程；强制杀进程或断电不经过 Exit，尚在防抖窗口内的修改不保证保存。
- **`IsPendingRestart` 不感知 `RequiresRestart`**：它只回答「值是否偏离本进程启动时的生效值」（`SettingsService.cs:146`、判定维护 `TrackPendingRestart` :158）——非需重启项被修改同样会进入 `_pendingRestartIds`，「重启后生效」语义靠调用方结合 `SettingItemContribution.RequiresRestart` 过滤（设置页的做法：`SettingsPageViewModel.ComputeHasPendingRestartChanges`）。另注意改回启动值即自动撤销 pending——标记/横幅会消失，这不是 bug 而是「值已回到生效值，无需重启」的刻意语义。
- **设置页值只在首次打开时读取**：设置页是普通主视图贡献，视图实例被 shell 缓存，ViewModel 只构造一次（`Modules/Settings/ViewModels/SettingsPageViewModel.cs:25-38`）——分组/设置项贡献与当前值都在构造期（首次打开）读取。运行期经 `SettingChangedEvent` 改了值，已缓存的设置页不会重读（编辑器显示值不随外部变更刷新）；仅「重启后生效」标记与横幅例外——ViewModel 订阅了 `SettingChangedEvent` 实时刷新（`OnSettingChanged`）。需要「外部变更同步到编辑器」时另行订阅事件重建，不要假定每次打开都刷新。
- **命令的 Gesture 不生效先查 shell 接线**：`Gesture` 只是契约上的文本——机制（`FrameworkWindow.RegisterCommandGestures`，`Windows/FrameworkWindow.cs:114`）与接线（shell 收集命令后调用一次，真实接线 `Modules/Workstation/MainWindow.axaml.cs:22` 的 `PrepareContributions`）分层；漏调接线方法则快捷键静默不存在（面板里 gesture 文本仍显示——它只读契约属性）。解析失败的文本记 `Logger.Warning` 跳过。
- **快捷键声明不是显示文案**：直接显示 `ICommandContribution.Gesture` 或调用 `KeyGesture.ToString()` 会暴露 `OemComma` 等枚举名。命令面板统一用 `ToString("p", null)` 走 Avalonia 平台格式化；不要为具体命令硬编码标签，也不要维护第二份 OEM 键名映射。
- **命令面板数据源是宽松绑定名 `"Commands"`**：`FrameworkWindow` 构造时 `_palette[!ItemsSource] = new Binding("Commands")`（`Windows/FrameworkWindow.cs:38`），与 `MenuBarItems` 同为约定而非类型约束——ViewModel 属性改名后绑定静默落空，面板打开是空列表且无异常。同理 Ctrl+P KeyBinding 硬编码在 `FrameworkWindow` 构造函数（:40-44），不在任何 axaml。
- **MRU 只在内存**：`CommandPalette._recentIds`（`Windows/CommandPalette.cs:29`）随控件存活，重启即清——这是 [ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md) 决策 8 的刻意取舍，不要当 bug 修；持久化时仿 `LayoutPersistence` 模式设计。
- **`FuncDataTemplate<T>` 会被虚拟化回收以 null 调用**：`ItemsSource` 替换时 `VirtualizingStackPanel` 回收旧容器走 `ClearContainerForItemOverride` → `ContentPresenter` 以 null 内容调模板 `Build`（真实踩坑：`CommandPalette.BuildItem` 解引用 `command.Gesture` 炸 NullReference，第二次 Ctrl+P 必现——第一次从空源添加不走回收路径）。代码创建的项模板必须容忍 null 数据（`CommandPalette.BuildItem` 以 `ICommandContribution?` 判空返回空 `Grid`，`Windows/CommandPalette.cs:133-140`）；菜单的 `MenuItemViewModel` 模板没踩到只是因为菜单容器从不清理。
- **ActivityBar 钉住段不能用 DockPanel bottom dock**：`ShellActivityBar` 曾用 `DockPanel` + `DockPanel.Dock="Bottom"` 放钉住项（设置齿轮）——bottom dock 把钉住段排到栏底之下约 36px（溢出进状态栏区域，被祖先裁剪**完全不可见**，但 UI 树里元素存在、BoundingRectangle 有效；真实踩坑，HEAD 基线与现版几何一致，属主题迁移遗留）。已改为 `Grid RowDefinitions="*,Auto"`（ToolViewBar 占 *、底部段占 Auto，硬约束在栏内底部，`FrameworkWindowTheme.axaml:34-54`）；底部段现为 StackPanel（:41-53）：钉住区 ItemsControl + shell 内置"设置"导航按钮（[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 6，设置齿轮已由钉住工具视图改为纯导航按钮）。排查"元素存在但看不见"用 UI Automation 读 BoundingRectangle 对比窗口/相邻元素位置，别只查数据绑定。
- **菜单方法必须是公共实例无参方法**：`RegisterMenus` 只取 `BindingFlags.Public | Instance | DeclaredOnly` 方法（:37），再校验无参且返回值 `void`/`Task`（:44-45）——**静态方法、非公共方法连候选都进不了**（连跳过日志都没有）；带参/返回值非法的方法只记一条 `Logger.Warning`（:47-50）就跳过，菜单同样静默少一项。同理类路径或条目路径含空段也只是记日志跳过（`MenuRegistration.cs:30-35`、`MenuTreeBuilder.cs:24-29`）。排查"菜单没出现"先翻日志的 Warning。
- **把菜单项 DataTemplate 挪进 `Styles.Resources`**：无 x:Key 的 `DataTemplate` 放在 Styles 的资源字典里会触发 AVLN3000（资源必须带键）；菜单项模板（`MenuItemViewModel` → 图标+标题）因此在 `FrameworkWindow` 构造函数里代码注册进窗口 `DataTemplates`（`FrameworkWindow.cs:53`）——窗口级模板同时保证子菜单任意深度经模板查找递归复用。配套样式可以留在 axaml（`FrameworkWindowTheme.axaml:611-628`），因为它们带完整 Selector 不需要键。
- **最大化菜单跨屏**：Avalonia 11.3.20 的 `ManagedPopupPositioner` 优先按锚定矩形左上角选屏。Windows 最大化时 chrome 左缘可能越出当前屏幕（实测右屏始于 X=1440，首个菜单按钮始于 X=1439），导致整份菜单被翻转/滑动到左屏。顶层弹出层必须保留 `Placement=Custom` 与 `FrameworkWindowTheme.MenuPopupPlacement`：按锚定矩形中心选屏后裁剪矩形，再沿用原生边界约束。只改 HorizontalOffset 不改变选屏输入；固定补 1px 也不表达 DPI/屏幕边界语义。回归步骤见 testing.md“多屏最大化菜单手动回归”。
- **移动 axaml 主题文件必须同步 `StyleInclude` 的 BaseUri**：avares 路径跟随项目内目录（`FrameworkWindowTheme.cs:14` 的 `avares://DigitalWorkstation.Core.Framework/Windows/`），编译不检查、运行时生效——移了 axaml 没改 BaseUri，编译绿灯但窗口构造期查不到布局模板/样式，主题静默缺失（布局模板缺失会抛"布局模板资源缺失"，样式缺失则连异常都没有）。同理 axaml 内 `xmlns:layout`（`FrameworkWindowTheme.axaml:3`）指向 `PanelResizer` 所在命名空间，移动 PanelResizer 要同步改。
- **锁必须覆盖文件提交**：仅在锁内取出 pending、锁外序列化写盘仍会让旧回调覆盖新结果。DebouncedJsonFile 的 Timer、FlushPending、Delete、Dispose 共用文件锁；不要把实际写盘搬到锁外，也不要在文件回调反向访问设置内存。
- **重置必须走 LayoutPersistence.Delete**：统一文件写入器等待在途写入、作废 pending、停止后续计时并删除文件，整个过程在同一锁内。只调用 Timer.Change(Infinite) 并不能等待已开始回调；绕过 Delete 直接删文件会重新引入旧布局复活。
- **Placements 不含钉住项**：ShellLayoutConfiguration.Capture 只读取 State.ActivityBarItems 和两个面板 Tabs；恢复按当前贡献过滤孤儿、处理默认归属。不要重新从 ObservableCollection 捕获布局，避免状态和呈现分裂。
- **手工编辑 layout.json 后"配置被吞"**：枚举值必须是 `"Center"`/`"BottomPanel"` 这类字符串（`JsonStringEnumConverter`，`LayoutPersistence.cs:27`）；非法枚举字符串让 System.Text.Json 抛 `JsonException`，`Load` 的 catch（:62-67）把**整份文件**丢弃回默认布局——这是容错设计，不是 bug。
- **自定义控件继承主题用 `StyleKeyOverride` 后，类型选择器全部失效**：Avalonia 样式的类型选择器（`Button.nav-item`、`layout|ToolViewBar`）匹配的是控件的 **StyleKey 而非运行时类型**。`ToolViewButton` override 成 `Button` 是刻意的（继承 nav-item/panel-tab 类样式）；`ToolViewBar` 则**不得** override 成 `ItemsControl`——否则 `layout|ToolViewBar` 选择器与其 ControlTheme 查找（按 StyleKey 作资源键）双双落空，表现为整条带不接收拖放、悬停高亮不显示（真实踩坑，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md) 片段 2）。需要继承基类模板又保留自身类型选择器时，为子类写专属 ControlTheme（模板可从基类主题复制），不要 override StyleKey。
- **Background=null 的区域不参与命中测试**：拖放/指针事件落不到无背景的容器空白区（真实踩坑：`ToolViewBar` 空条带无法落放）。修复是在 ControlTheme 里 `Background=Transparent`（模板含 `Border Background={TemplateBinding Background}` 才渲染），拖放目标控件务必保证背景非 null。
- **Avalonia 11.3 起 DnD 是新 API**：`DataObject`/`DragDrop.DoDragDrop`/`DragEventArgs.Data` 均已过时；用 `DataTransfer` + `DataTransferItem.Create(format, value)` + `DragDrop.DoDragDropAsync`，格式用 `DataFormat.CreateStringApplicationFormat`（标识符只允许 ASCII 字母/数字/点/连字符）；`AllowDrop` 变成附加属性，代码里用 `DragDrop.SetAllowDrop(control, true)`。读取侧 `DragEventArgs.DataTransfer.Contains/TryGetValue(format)`。
- **`DragLeave` 是冒泡路由事件**：指针在条目间移动时会从子元素冒泡到投放目标——在 DragLeave 里清视觉指示前要确认指针真正离开（`ToolViewBar.OnDragLeave` 用 `Bounds.Contains(e.GetPosition(this))` 守卫，`Layout/ToolViewBar.cs:149`），否则插入指示在条目间移动时闪烁消失。**反过来，DragLeave 也不足以兜底清除**：Esc 取消拖拽、窗口外松手等路径下最后悬停的 Bar 收不到 DragLeave，`drag-over` 高亮/占位线会残留（真实踩坑）——`ToolViewBar` 因此订阅 `ToolViewDragSession.ActiveChanged`，会话结束（false）时一律 `ClearInsertion`（:81-87；订阅在 OnAttachedToVisualTree/OnDetachedFromVisualTree 配对，:65-76）。

## 历史踩坑（注释/防御性代码透露）

- `FrameworkApplication.cs:27`："固定 Dark：当前设计目标为 VS Code Dark+ 单一色调，未做亮色适配"——不要假设主题可切换，`VSCodePalette` 色值全部写死。
- `FrameworkApplication.cs:35-38` 注释说明不调 base 的原因（[ADR-0004](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0004-startup-sequence.md)）；`OnInitialized`/`InitializeModules` 的空方法体各带"阻止/抑制 base"注释——这三个空方法是**有意的**，不是待实现 TODO。
- `ShellLayoutState.cs:4` 注释："原型验证过的 reducer 的正式实现"——该状态机先有原型验证，转换语义（收起保留、拒绝非法）是验证过的契约，改语义前先找原型依据。
- `SideBarState.cs:16`、`BottomPanelState.cs:19`、`AuxiliaryPanelState.cs:19` 注释反复强调"收起时保留"——曾因收起丢内容踩坑，保留语义是修复结果。
- `FrameworkWindowManager.cs:27` 把错误消息提为 `const NullMainWindowError` 并在四处复用——主窗口缺失是高频错误路径，消息统一便于日志检索。
- Abstractions 侧已知瑕疵（详见该模块文档）：`IWindowManager` 实例版 `ShowWindow` 的 XML 注释误写为"对话框窗口"；`WindowManagerExtenstion` 拼写错误；泛型扩展 `GetWindow<TWindow>` 返回 `Window?` 与接口非空返回不一致——Framework 的实现不受影响，但经泛型扩展调用时注意可空标注。


## 启动准备与设置目录不变量

- 模块按目录依赖顺序串行加载。RegisterTypes 必须同步完成登记，不应自行嵌套加载其他模块；关闭批次与登记插入使用同一锁，迟到登记直接失败。
- Continue 拒绝的是失败模块的 Shell 贡献，不回滚普通服务、事件订阅或构造副作用。依赖失败模块的后继不会调用 LoadModule，独立模块可以继续。
- 菜单/命令宿主在 UI 线程准备一次，Ready 前完成 Shell 集合和手势接线。工具视图/主视图内容仍按需创建，其打开阶段异常不是模块工厂预构造的保证范围。
- 设置默认值、ValueType、RequiresRestart 和页面分组必须来自 SettingCatalog。不能另建 _declared 缓存，尤其不能把候选批次声明缓存到拒绝之后。
- 卡片外边距或容器内边距改 ShellLayoutMetrics；四模板使用 x:Static 同源值，列宽不能在 Workstation 再写 +4。

## 插件上下文边界

- 不要把插件入口重新交给按程序集名称解析 Type 的路径；直接使用发现的 Type。ViewModel 同样从 View.Assembly 查询。
- 插件依赖只接受显式内置模块的 ModuleName；内置模块完成 Prism 初始化后若贡献准备失败，依赖它的插件仍须失败。
- 私有程序集不因宿主碰巧已加载同名库就共享；共享范围由 Build/PluginSharedAssemblies.txt 与显式内置模块实例确定。
- Debug 平铺输出只能留一种同名依赖版本；Release 隔离须在 plugins/<项目名>/ 布局验证。

- Release 的 runtimes 资产通过插件 .deps.json 与 AssemblyDependencyResolver 定位；未列入依赖描述的托管 DLL 仅回退插件顶层/对应文化目录，native 仅回退插件顶层及系统库目录，不递归猜测多个 RID。
- 插件 private 托管依赖缺失抛 FileLoadException 以结束当前解析；ResourceManager 对卫星资源仍执行文化回退。系统 native 用绝对系统路径 TryLoad，兼容 macOS dyld cache 中没有实体文件的系统库。
