# Framework — 异常与排查

## 模块可能抛出的异常

| 异常类型 | 触发条件 | 抛出位置 |
|---|---|---|
| `ArgumentException("TargetWindow type is illegal")` | `IoC.Provider.Resolve(type)` 的解析结果为 null 或不是 `Window`（类型未注册到容器，或注册的不是窗口类型） | `FrameworkWindowManager.GetWindow`，`WindowManager/FrameworkWindowManager.cs:37` |
| `InvalidOperationException($"Window of type {type} is already registered.")` | 同一运行时类型的窗口在已注册（尚未关闭）时再次 `ShowWindow`/`ShowDialog` | `FrameworkWindowManager.InitializeWindow`，`FrameworkWindowManager.cs:49` |
| `InvalidOperationException("Main window is not set. Cannot show window.")`（常量 `NullMainWindowError`，第 27 行） | `_mainWindow` 为 null 时调用 `ShowWindow(Window)`/`ShowWindow(Window, object)`（第 89、110 行）；`_mainWindow` 为 null 或不活跃（`IsActive=false`）时调用 `ShowDialog` 实例版（第 130、140 行）；`HandleMainWindow()` 取不到主窗口**或主窗口已在 `_windowMap` 中**（第 166 行） | `FrameworkWindowManager` 各处 |
| `KeyNotFoundException($"No window of type {type} is currently open.")` | 对未打开（或已关闭被移除映射）的窗口类型调 `HideWindow(Type)` | `FrameworkWindowManager.HideWindow`，`FrameworkWindowManager.cs:155` |
| `InvalidOperationException("IoC is already initialized")` | 进程内第二次调用 `IoC.Initialize`（由 Common 模块抛出；正常路径下只有 `RegisterFrameworkServices` 调一次） | 经 `FrameworkApplication.cs:148` 触发，定义在 Core/Common/IoC.cs |
| `NullReferenceException` | `IoC.Initialize` 之前任何代码访问 `IoC.Provider`/`IoC.Registry`（字段以 `null!` 抑制编译警告） | 经 `FrameworkWindowManager.GetWindow`（第 37 行）等，根源在 Core/Common |
| 模块加载任意异常 | `moduleManager.LoadModule` 抛出的任何异常 | 不向上抛——`RunStartupSequenceAsync` 的 catch（`FrameworkApplication.cs:94`）捕获 |
| `InvalidOperationException($"布局模板资源缺失：{key}")` | `FrameworkWindow` 构造或 `PanelAlignment` 切换时，对应档位的 `WindowLayout*` 布局模板经 `TryGetResource` 查不到（模板资源未编译进程序集，或键名与 axaml 漂移）——窗口构造期即失败 | `FrameworkWindow.UpdateLayoutTemplate`，`Windows/FrameworkWindow.cs:94` |
| 无——**不抛异常** | layout.json 读/写/删的任何失败（JSON 损坏、枚举字符串非法、版本不识别、IO 失败） | `LayoutPersistence` 全路径 `catch (Exception)` + `Logger.Warning` 静默回落：`Load` 返回 null（`Layout/LayoutPersistence.cs:49、55、64`）、`Flush` 放弃本次写盘（:126）、`Delete` 放弃删除（:100） |

## 错误处理路径

### 启动序列（FrameworkApplication.cs:68-116）

```
RunStartupSequenceAsync
├── 模块加载、依赖不可用或贡献准备失败
│     → Logger.Error(ex, $"Failed to prepare module {name}")（第 93 行，Console sink）
│     → Reject 批次；先调用 WaitForFailureActionAsync 订阅决策
│     → Publish ModuleLoadFailedEvent；等待已订阅的决策
│         Continue → 拒绝该批贡献，继续其余模块；依赖者同样进入失败决策
│         Exit     → (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown()（第 98 行）
└── 序列级异常（阶段 1/3 或意料外异常，catch 第 108 行）
      → Logger.Fatal(ex, "Startup sequence failed")（第 110 行）
      → Shutdown()（第 111 行）——应用直接退出，无重试
```

注意：`RunStartupSequenceAsync` 是从 `OnFrameworkInitializationCompleted` 以 `_ =` fire-and-forget 启动的（第 38 行），若最外层 catch 也失效（极端情况），异常会成为未观察的 Task 异常。

### 窗口管理

窗口 API 不做防御性吞异常——非法调用（主窗口未登记、重复注册、隐藏未打开窗口、类型非窗口）全部直接抛给调用方。唯一的静默路径是 `CloseWindow(Type)`：类型未命中 `_windowMap` 时不抛也不记日志（`FrameworkWindowManager.cs:144-148`）。

### Shell 布局状态

`ShellLayoutState` 不抛任何异常：非法操作（面板收起时 `ActivateAuxTab`/`ActivateBottomTab`、未知 `PanelResizeTarget`）返回等值状态（`return this`，`ShellLayoutState.cs:73、86、130`）；`Resize` 用 `Clamp`（第 134 行）把越界值钳到 Min/Max。错误语义是"拒绝并保持现状"，由调用方（MainWindowViewModel）自然忽略。

### 配置持久化

LayoutPersistence.Load 保留文件缺失静默返回 null、空内容/未知版本/反序列化失败记 Warning 后回默认的语义。SettingsService.Load/Get 继续按原有规则回落默认值。

两类文件的写入和删除统一记录来源 ConfigurationPersistence 的英文日志：Failed to write configuration、Failed to delete configuration、Failed to remove temporary configuration。写入失败不覆盖旧目标，并保留待写快照供下一次调度或 FlushPending 重试。FlushPending 返回 false 时 ApplicationRestarter 记录 Failed to save pending configuration; restart aborted，不启动新进程。

正常 Exit 最后尝试保存并释放计时器；如果磁盘仍不可写，只能记录失败并继续退出，不能声称配置已保存。Dispose 后继续 ScheduleSave/Delete 是生命周期误用，抛 ObjectDisposedException。进程强制终止不经过该收尾。

### 命令面板快捷键标签

`CommandPalette.FormatGesture` 使用 Avalonia 的平台格式化，不直接显示枚举键名。声明无法被 `KeyGesture.Parse` 解析时捕获 `FormatException` 并保留原文，避免新增的显示格式化中断整个命令列表；窗口快捷键注册路径继续负责记录警告及跳过无效绑定。

## 排查方式

| 症状 | 看哪里 | 常见原因 |
|---|---|---|
| 启动即退出、无窗口 | Console 日志找 `[FTL] 启动序列执行失败`（`Logger.Fatal`，第 110 行） | 阶段 1 失败：`HandleMainWindow` 抛异常（`CreateShell` 解析 `TWindow` 失败导致 `PrismApplication.MainWindow` 为 null）；模块目录校验失败 |
| 启动台停在某个模块 | 日志找 `模块 X 加载失败`（第 93 行）；启动台收到 `ModuleLoadFailedEvent` | 模块的 `IModule.Initialize`/`RegisterTypes` 抛异常；决策事件 `StartupFailureActionEvent` 无人发布会**永久挂起**在 `WaitForFailureActionAsync`（第 123 行 await） |
| `ShowDialog` 抛 `InvalidOperationException` | `FrameworkWindowManager.cs:127、137` 的 `_mainWindow is { IsActive: true }` 检查 | 主窗口被 Hide 或未激活时弹对话框；或 `HandleMainWindow()` 尚未执行（启动序列阶段 1 之前） |
| 第二次打开同类窗口抛"already registered" | `InitializeWindow`（第 45-51 行）与 `Closing` 移除逻辑 | 上一个实例未触发 `Closing`（如进程异常路径）或调用方重复 Show 未先 Close |
| 主窗口登记失败但 MainWindow 明明存在 | `HandleMainWindow`（第 158-167 行）——`_mainWindow != null` 且已在 `_windowMap` 也走 else 抛异常 | **重复调用 `HandleMainWindow()`**（第二次调用时主窗口已注册，直接抛） |
| 面板 tab 点了没反应 | `ShellLayoutState.ActivateAuxTab/ActivateBottomTab` 的 `Visible` 检查（第 71、84 行） | 面板处于收起状态，激活被拒绝是设计行为，先展开面板 |
| 拖分隔条尺寸不动/跳变 | `Resize` 的 Clamp（第 103-132 行）与各 record 的 Min/Max 常量 | delta 累计后被钳在边界；或消费方未用返回的新实例替换旧状态 |
| 窗口构造即抛"布局模板资源缺失：{key}" | `FrameworkWindow.UpdateLayoutTemplate`（`Windows/FrameworkWindow.cs:82-95`）的键映射 vs `Windows/FrameworkWindowTheme.axaml` 的 `WindowLayout*` 资源键 | 键名漂移（改了一侧没改另一侧），或 axaml 未作为编译资源进程序集（`FrameworkWindowTheme.cs` 经 `StyleInclude` 从 `avares://` 加载，构造时已强制 `Loaded`） |
| 工具视图没出现在任何 Bar | 日志找 `Logger.Warning` 的 `工具视图 ... 已跳过`（`Contributions/ToolViewRegistration.cs:33、40`） | 类非可实例化 `Control` 或 `Id` 在程序集内重复被跳过；或模块 `RegisterTypes` 未调 `RegisterToolViews`（扫描不做全局发现，[ADR-0002](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0002-toolview-drag-persistence.md)） |
| 重启后布局回默认/布局改动没记住 | 读取看 LayoutPersistence Warning；写入看 ConfigurationPersistence Warning | 损坏/未知版本按默认读取；权限、磁盘或文件占用导致保存失败，待写快照在进程内可重试 |
| 手工编辑 layout.json 后布局全丢 | 同上，`Load` 的 Warning（:64，异常类型名会打在日志里） | 枚举值必须是 `"Center"`/`"BottomPanel"` 形态字符串（`JsonStringEnumConverter`，:27）；非法枚举字符串让 System.Text.Json 抛 `JsonException`，整份文件被丢弃回默认——容错设计不是 bug |
| 「重置布局」后旧布局又复活 | LayoutPersistence.Delete → DebouncedJsonFile.Delete | 检查是否绕过统一写入模块或把文件提交移出锁；仅停 Timer 不会等待已经开始的回调 |
| 菜单/命令/工具视图显示资源键 | 声明的 ResourceType、资源类型全名/程序集与键 | 来源资源集中无键时返回原键；不会搜索其他模块。标题已固化在 singleton，修正语言后需重启 |
| 菜单节点显示稳定 Id | MenuGroup 是否有标题声明、PathTitle 是否非 null | 只引用路径或隐式祖先没有标题是合法回退；后到所有者声明应覆盖 Id 显示，但不得覆盖已有非 null 标题 |
| 设置组重复或错误归组 | SettingGroupContribution.Id 与 SettingItemContribution.Group | 身份按稳定 Id 匹配，与名称键无关；同 Id 首个声明来源生效，未声明 Id 直接显示 |
| 枚举选项显示组合键 | SettingItemContribution.ResourceType 中的 `Name + 成员名` 键 | 缺键回退完整键名，不是裸成员名；翻译键与枚举持久化值互不影响 |


## 架构修复后的排查入口

- 菜单/命令宿主构造失败：在对应模块 Prepare 内捕获，启动台显示失败；继续后不再解析该批工厂。部分 RegisterTypes 失败同样隐藏已登记的贡献。
- 日志 Required module ... is unavailable：依赖模块未通过准备。后继未执行 LoadModule，不是新的容器解析失败。
- Contribution registration for ... is closed：模块在完成 RegisterTypes 或被拒绝后仍有后台登记，修正模块登记时机。
- 宿主基础贡献、PrepareShell 建树/布局恢复失败：Ready 尚未发布，按序列级 Fatal 退出。该路径不假装成某个可跳过模块。
- 重复设置 Id：SettingCatalog 英文 Warning 指出被丢弃的后到声明。服务默认值、页面编辑器与重启属性保持首个有效声明；检查跨模块 Id，不通过读取其他 Id 刷新缓存。
