# Models — 不变量与陷阱

## 隐含不变量

1. **序号从 1 开始**：`StartupProgress.ModuleIndex` 与 `ModuleLoadFailure.ModuleIndex` 都约定从 1 开始（XML 注释明确标注），发布方 `FrameworkApplication.cs` 第 86/95 行用 `i + 1` 实现；非加载模块阶段用 0 占位（第 75 行 `0, 0`）。消费方 UI 直接展示该值，改成 0 起始会让启动台显示"0/N"。
2. **`ModuleName` 可空性按阶段约定**：仅 `StartupPhase.LoadingModules` 阶段非 null（StartupProgress.cs 注释）。类型系统不强制——订阅方 `DashBoardWindowViewModel.OnProgress` 第 48 行用 `progress.Phase == StartupPhase.LoadingModules` 判断后才读 `ModuleName`。在非 LoadingModules 阶段传非 null 值不会报错，但违反契约；在 LoadingModules 阶段传 null 会让启动台模块名显示空白。
3. **线程亲和性不对称**：启动台订阅显式用 `ThreadOption.UIThread`（DashBoardWindowViewModel.cs 第 19-20 行），主窗口订阅用默认选项（MainWindowViewModel.cs 第 48-51 行）——后者成立只是因为当前发布方都在 UI 线程上下文发布。若将来把 `OpenMainViewEvent`/`TogglePanelVisibilityEvent` 移到后台线程发布，主窗口侧会在非 UI 线程触碰 UI 绑定。
4. **`StartupFailureActionEvent` 是一次性请求/响应，必须成对**：`WaitForFailureActionAsync`（FrameworkApplication.cs 第 118-126 行）订阅后 await，收到第一个决策即 `Unsubscribe(token)`。隐含约束：**每次失败必须恰好有一次决策发布**——无人发布则启动序列永久挂起（无超时无取消）；多订阅方同时发布也只有第一个生效。
5. **`OpenMainViewEvent` 的发布方是封闭集合**：SideBar 内交互与 shell 的"设置"导航按钮（`MainWindowViewModel.OpenSettings`，ADR-0006 决策 6）。注释明确 **ActivityBar 导航切换**不发布本事件——注意设置按钮虽位于 ActivityBar 底部，但它是纯导航按钮而非导航切换，不在此限。绕过这两类语义在别处发布会打破"ActivityBar 切换不改 MainContent"的设计区分。
6. **弱引用订阅的生命周期假设**：MainWindowViewModel 用 `Subscribe(OpenMainView)` 默认参数（弱引用）；DashBoard 显式传 `keepSubscriberReferenceAlive: true`。订阅方对象若早于事件源被回收，弱引用订阅会**静默失效**——没有任何异常，事件照常发布但无人接收。

## 易错改法

- **加枚举成员后编译全绿但行为错**：`MainWindowViewModel.TogglePanel`（第 488-497 行）与 `DashBoardWindowViewModel.OnProgress`（第 41-47 行）用 `_` 兜底分支。给 `TogglePanelTarget` 加新成员后，实际翻转的面板会静默落到兜底值（都翻转 BottomPanel）；同时别忘了在 `ViewPanelMenus`（`Modules/Workstation/Menus/ViewPanelMenus.cs`）补对应 `[MenuItem]` 方法——菜单项是逐方法声明的，不加编译也不报错。C# switch 表达式对枚举无穷尽性强制。
- **删掉 Models.csproj 里"看似无用"的 Common 引用**：本模块源码零引用 Common 类型（不用 Logger/IoC），引用看似可删；但 Prism 程序集与 `Prism.Events` global using（obj/Debug/Models.GlobalUsings.g.cs 第 6 行）全经此传递，删掉后 `PubSubEvent<T>` 立即无法解析。
- **把 record 改成 class 或加可变属性**：`StartupProgress`/`ModuleLoadFailure` 的值相等与不可变性是"消息"语义的组成部分；改成可变 class 后，订阅方若在异步处理期间共享实例，发布方再改字段会造成跨订阅方串扰。
- **在另一个程序集里"复制"同名事件类**：`EventAggregator.GetEvent<T>()` 按类型身份撮合，同名同结构的重复定义互不互通，发布与订阅各拿各的事件实例，症状是"订阅了但永远收不到"且无报错。
- **给事件类加成员**：空子类的全部价值是类型身份；一旦加逻辑，逻辑归属就跑偏（机制应在发布/订阅方或 PubSubEvent 本身），且无法被 Prism 机制感知。
- **`ThreadOption.UIThread` 订阅里做重活**：启动台回调在 UI 线程执行，`OnProgress`/`OnModuleFailed` 里若加阻塞操作会直接卡住启动序列（发布方在 UI 上下文同步派发）。

## 历史踩坑线索

- **ADR-0004 是"悬空引用"**：`StartupPhase.cs`/`StartupProgressEvent.cs`/`ModuleLoadFailedEvent.cs` 的注释与 README 第 47-51 行都引用 ADR-0004，但仓库中不存在该 ADR 文档（docs/ 下无 adr 目录）。决策细节只能从代码注释还原；改启动流程前无法回查原始决策，属已知风险。
- **防御性注释即契约**：`StartupPhase` 注释特意声明"Launcher 引导发生在 Avalonia 启动前，不在进度覆盖范围内"——说明曾有人（或预期有人）疑惑启动台为何不覆盖最早阶段；该注释是防止错误扩展进度范围的防线。
- **`OpenMainViewEvent` 注释的否定句式**：「ActivityBar 导航切换不发布本事件，MainContent 保持不变」——否定式契约通常对应一次实际踩坑或评审纠正，改导航行为时优先尊重它。ADR-0006 决策 6 后发布方扩为「SideBar 内交互 + shell"设置"导航按钮」两处，该否定句仍精确成立：它限定的是**导航切换**这一交互，而设置按钮是纯导航命令。
- **`TaskCreationOptions.RunContinuationsAsynchronously`**（FrameworkApplication.cs 第 121 行）：没有这个选项时 `TrySetResult` 会让 await 续体在发布者（按钮点击）线程内联执行，把启动序列的后续模块加载跑到 UI 事件回调里——该选项的存在暗示此坑被考虑过。
