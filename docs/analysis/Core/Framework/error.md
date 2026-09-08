# Framework — 异常与排查

## 模块可能抛出的异常

| 异常类型 | 触发条件 | 抛出位置 |
|---|---|---|
| `ArgumentException("TargetWindow type is illegal")` | `IoC.Provider.Resolve(type)` 的解析结果为 null 或不是 `Window`（类型未注册到容器，或注册的不是窗口类型） | `FrameworkWindowManager.GetWindow`，`WindowManager/FrameworkWindowManager.cs:37` |
| `InvalidOperationException($"Window of type {type} is already registered.")` | 同一运行时类型的窗口在已注册（尚未关闭）时再次 `ShowWindow`/`ShowDialog` | `FrameworkWindowManager.InitializeWindow`，`FrameworkWindowManager.cs:49` |
| `InvalidOperationException("Main window is not set. Cannot show window.")`（常量 `NullMainWindowError`，第 27 行） | `_mainWindow` 为 null 时调用 `ShowWindow(Window)`/`ShowWindow(Window, object)`（第 89、110 行）；`_mainWindow` 为 null 或不活跃（`IsActive=false`）时调用 `ShowDialog` 实例版（第 130、140 行）；`HandleMainWindow()` 取不到主窗口**或主窗口已在 `_windowMap` 中**（第 166 行） | `FrameworkWindowManager` 各处 |
| `KeyNotFoundException($"No window of type {type} is currently open.")` | 对未打开（或已关闭被移除映射）的窗口类型调 `HideWindow(Type)` | `FrameworkWindowManager.HideWindow`，`FrameworkWindowManager.cs:155` |
| `InvalidOperationException("IoC is already initialized")` | 进程内第二次调用 `IoC.Initialize`（由 Common 模块抛出；正常路径下只有 `RegisterFrameworkServices` 调一次） | 经 `FrameworkApplication.cs:144` 触发，定义在 Core/Common/IoC.cs |
| `NullReferenceException` | `IoC.Initialize` 之前任何代码访问 `IoC.Provider`/`IoC.Registry`（字段以 `null!` 抑制编译警告） | 经 `FrameworkWindowManager.GetWindow`（第 37 行）等，根源在 Core/Common |
| 模块加载任意异常 | `moduleManager.LoadModule` 抛出的任何异常 | 不向上抛——`RunStartupSequenceAsync` 的 catch（`FrameworkApplication.cs:90`）捕获 |
| `InvalidOperationException($"布局模板资源缺失：{key}")` | `FrameworkWindow` 构造或 `PanelAlignment` 切换时，对应档位的 `WindowLayout*` 布局模板经 `TryGetResource` 查不到（模板资源未编译进程序集，或键名与 axaml 漂移）——窗口构造期即失败 | `FrameworkWindow.UpdateLayoutTemplate`，`Windows/FrameworkWindow.cs:94` |

## 错误处理路径

### 启动序列（FrameworkApplication.cs:64-112）

```
RunStartupSequenceAsync
├── 单模块加载失败（catch 第 90 行）
│     → Logger.Error(ex, $"模块 {name} 加载失败")（第 92 行，Console sink）
│     → Publish ModuleLoadFailedEvent(ModuleLoadFailure(name, i+1, total, ex.Message))（第 93-94 行）
│     → WaitForFailureActionAsync() 等待启动台决策（第 95 行）
│         Continue → 跳过该模块继续循环
│         Exit     → (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown()（第 97 行）
└── 序列级异常（阶段 1/3 或意料外异常，catch 第 107 行）
      → Logger.Fatal(ex, "启动序列执行失败")（第 109 行）
      → Shutdown()（第 110 行）——应用直接退出，无重试
```

注意：`RunStartupSequenceAsync` 是从 `OnFrameworkInitializationCompleted` 以 `_ =` fire-and-forget 启动的（第 37 行），若最外层 catch 也失效（极端情况），异常会成为未观察的 Task 异常。

### 窗口管理

窗口 API 不做防御性吞异常——非法调用（主窗口未登记、重复注册、隐藏未打开窗口、类型非窗口）全部直接抛给调用方。唯一的静默路径是 `CloseWindow(Type)`：类型未命中 `_windowMap` 时不抛也不记日志（`FrameworkWindowManager.cs:144-148`）。

### Shell 布局状态

`ShellLayoutState` 不抛任何异常：非法操作（面板收起时 `ActivateAuxTab`/`ActivateBottomTab`、未知 `PanelResizeTarget`）返回等值状态（`return this`，`ShellLayoutState.cs:73、86、130`）；`Resize` 用 `Clamp`（第 134 行）把越界值钳到 Min/Max。错误语义是"拒绝并保持现状"，由调用方（MainWindowViewModel）自然忽略。

## 排查方式

| 症状 | 看哪里 | 常见原因 |
|---|---|---|
| 启动即退出、无窗口 | Console 日志找 `[FTL] 启动序列执行失败`（`Logger.Fatal`，第 109 行） | 阶段 1 失败：`HandleMainWindow` 抛异常（`CreateShell` 解析 `TWindow` 失败导致 `PrismApplication.MainWindow` 为 null）；模块目录校验失败 |
| 启动台停在某个模块 | 日志找 `模块 X 加载失败`（第 92 行）；启动台收到 `ModuleLoadFailedEvent` | 模块的 `IModule.Initialize`/`RegisterTypes` 抛异常；决策事件 `StartupFailureActionEvent` 无人发布会**永久挂起**在 `WaitForFailureActionAsync`（第 122 行 await） |
| `ShowDialog` 抛 `InvalidOperationException` | `FrameworkWindowManager.cs:127、137` 的 `_mainWindow is { IsActive: true }` 检查 | 主窗口被 Hide 或未激活时弹对话框；或 `HandleMainWindow()` 尚未执行（启动序列阶段 1 之前） |
| 第二次打开同类窗口抛"already registered" | `InitializeWindow`（第 45-51 行）与 `Closing` 移除逻辑 | 上一个实例未触发 `Closing`（如进程异常路径）或调用方重复 Show 未先 Close |
| 主窗口登记失败但 MainWindow 明明存在 | `HandleMainWindow`（第 158-167 行）——`_mainWindow != null` 且已在 `_windowMap` 也走 else 抛异常 | **重复调用 `HandleMainWindow()`**（第二次调用时主窗口已注册，直接抛） |
| 面板 tab 点了没反应 | `ShellLayoutState.ActivateAuxTab/ActivateBottomTab` 的 `Visible` 检查（第 71、84 行） | 面板处于收起状态，激活被拒绝是设计行为，先展开面板 |
| 拖分隔条尺寸不动/跳变 | `Resize` 的 Clamp（第 103-132 行）与各 record 的 Min/Max 常量 | delta 累计后被钳在边界；或消费方未用返回的新实例替换旧状态 |
| 窗口构造即抛"布局模板资源缺失：{key}" | `FrameworkWindow.UpdateLayoutTemplate`（`Windows/FrameworkWindow.cs:82-95`）的键映射 vs `Windows/FrameworkWindowTheme.axaml` 的 `WindowLayout*` 资源键 | 键名漂移（改了一侧没改另一侧），或 axaml 未作为编译资源进程序集（`FrameworkWindowTheme.cs` 经 `StyleInclude` 从 `avares://` 加载，构造时已强制 `Loaded`） |
