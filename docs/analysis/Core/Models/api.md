# Models — 对外接口与调用方式

本模块的"API 面"= 7 个事件类 + 3 个负载 record + 3 个枚举，全部 public，全部位于命名空间 `DigitalWorkstation.Core.Models.Events`。模块无方法、无服务、无生命周期管理，调用方式统一为 Prism `IEventAggregator.GetEvent<TEvent>()` 的发布/订阅。

## 事件类（7 个，均为空子类：6 个 `PubSubEvent<T>` + 1 个无负载的非泛型 `PubSubEvent`）

| 类型 | 定义 | 负载 | 发布方（真实调用点） | 订阅方（真实调用点） |
|---|---|---|---|---|
| `StartupProgressEvent` | Events/StartupProgressEvent.cs:7 | `StartupProgress` | `Core/Framework/FrameworkApplication.cs` 第 73 行 `eventAggregator.GetEvent<StartupProgressEvent>()`，第 78/89/108 行 `Publish` | `Modules/DashBoard/ViewModels/Windows/DashBoardWindowViewModel.cs` 第 19 行 `Subscribe(OnProgress, ThreadOption.UIThread, true)` |
| `ModuleLoadFailedEvent` | Events/ModuleLoadFailedEvent.cs:7 | `ModuleLoadFailure` | FrameworkApplication.cs 第 97-98 行 | DashBoardWindowViewModel.cs 第 20 行 `Subscribe(OnModuleFailed, ThreadOption.UIThread, true)` |
| `StartupFailureActionEvent` | Events/StartupFailureActionEvent.cs:7 | `StartupFailureAction` | DashBoardWindowViewModel.cs 第 72 行（`Continue`）与第 81 行（`Exit`） | FrameworkApplication.cs `WaitForFailureActionAsync` 第 123-125 行（一次性订阅，收到后 `Unsubscribe(token)`） |
| `OpenMainViewEvent` | Events/OpenMainViewEvent.cs:7 | `string`（主视图 Id，即 `IMainViewContribution.Id`） | `Modules/Workstation/MainWindowViewModel.cs` `OpenSettings` 第 305 行（`WellKnownViews.Settings`，shell 左下角"设置"导航按钮，ADR-0006 决策 6）；原 DashBoard 导航视图的两个发布点已随演示视图删除 | `Modules/Workstation/MainWindowViewModel.cs` 第 48 行 `Subscribe(OpenMainView)`（默认线程选项） |
| `TogglePanelVisibilityEvent` | Events/TogglePanelVisibilityEvent.cs:7 | `TogglePanelTarget` | `Modules/Workstation/Menus/ViewPanelMenus.cs` 第 17/23/29 行（三个 `[MenuItem]` 方法体内分别 `Publish(SideBar/BottomPanel/AuxiliaryPanel)`） | MainWindowViewModel.cs 第 49 行 `Subscribe(TogglePanel)`（默认线程选项） |
| `ResetLayoutEvent` | Events/ResetLayoutEvent.cs:7 | 无（非泛型 `PubSubEvent`，ADR-0002） | `Modules/Workstation/Menus/ViewLayoutMenus.cs` 第 16 行（`ResetLayout` 方法体内 `Publish()`） | MainWindowViewModel.cs 第 51 行 `Subscribe(ResetLayout)`（默认线程选项） |
| `SettingChangedEvent` | Events/SettingChangedEvent.cs:7 | `SettingChanged`（ADR-0006 决策 3） | `Core/Framework/Settings/SettingsService.cs` `Set` 第 128 行（更新内存 + 防抖落盘后广播） | 设置页与需重启 UX 等消费方（工单 03 就位；当前源码尚无订阅调用点） |

事件类均无成员体（分号体声明），机制完全继承 Prism 基类：泛型 `Prism.Events.PubSubEvent<T>` 提供 `Publish(T payload)`、`Subscribe(Action<T> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive)`、`Unsubscribe(...)`；无负载的 `ResetLayoutEvent` 继承非泛型 `PubSubEvent`，对应无参形态 `Publish()`/`Subscribe(Action)`。

## 负载 record（3 个，位置 record，不可变、值相等）

### `StartupProgress`（Events/StartupProgress.cs:10）

```csharp
public record StartupProgress(StartupPhase Phase, string? ModuleName, int ModuleIndex, int ModuleCount);
```

- `Phase`：当前启动阶段，见 `StartupPhase`。
- `ModuleName`：正在加载的模块名；**仅 `StartupPhase.LoadingModules` 阶段非 null**，其余阶段为 null（注释约定，类型不强制）。
- `ModuleIndex`：当前模块序号，**从 1 开始**；非加载模块阶段传 0（FrameworkApplication.cs 第 78 行传 `0, 0`）。
- `ModuleCount`：模块总数；Ready 阶段发布 `total, total`（第 108 行），CoreServices 阶段发布 `0, 0`。

### `ModuleLoadFailure`（Events/ModuleLoadFailure.cs:10）

```csharp
public record ModuleLoadFailure(string ModuleName, int ModuleIndex, int ModuleCount, string ErrorMessage);
```

- `ModuleName`：失败的模块名。
- `ModuleIndex`：失败模块序号，**从 1 开始**。
- `ModuleCount`：模块总数。
- `ErrorMessage`：错误详情（发布方填 `ex.Message`，FrameworkApplication.cs 第 98 行）。

### `SettingChanged`（Events/SettingChanged.cs:9）

```csharp
public record SettingChanged(string SettingId, object? NewValue);
```

- `SettingId`：设置项 Id（`SettingItemContribution.Id`）。
- `NewValue`：新值（装箱后的设置值，类型由设置项声明的 `ValueType` 决定）。

## 枚举（3 个）

### `StartupPhase`（Events/StartupPhase.cs:6-22）

| 成员 | 语义 | 发布点 |
|---|---|---|
| `CoreServices` | 初始化核心服务（窗口管理器登记主窗口、模块目录校验） | FrameworkApplication.cs:78 |
| `LoadingModules` | 逐模块加载（唯一携带模块名与 i/N 的阶段） | FrameworkApplication.cs:89 |
| `Ready` | 全部模块就绪，启动台即将关闭、工作区显示 | FrameworkApplication.cs:108 |

注释明确：Launcher 引导发生在 Avalonia 启动前，不在进度覆盖范围内。

### `StartupFailureAction`（Events/StartupFailureAction.cs:6-17）

| 成员 | 语义 | 消费行为 |
|---|---|---|
| `Continue` | 跳过失败模块，加载其余模块并进入工作区 | `WaitForFailureActionAsync` 返回 true（FrameworkApplication.cs:128），启动序列继续 |
| `Exit` | 终止应用 | 返回 false → `Shutdown()`（FrameworkApplication.cs:101） |

### `TogglePanelTarget`（Events/TogglePanelTarget.cs:6-22）

| 成员 | 语义 |
|---|---|
| `SideBar` | 侧边栏 |
| `AuxiliaryPanel` | 辅助面板 |
| `BottomPanel` | 底部面板 |

## 典型调用序列

### 发布/订阅启动进度（现有代码形态）

```csharp
// 发布方：Core/Framework/FrameworkApplication.cs 启动序列内
var progressEvent = eventAggregator.GetEvent<StartupProgressEvent>();
progressEvent.Publish(new StartupProgress(StartupPhase.LoadingModules, module.ModuleName, i + 1, total));

// 订阅方：DashBoardWindowViewModel 构造函数（第 19 行）
eventAggregator.GetEvent<StartupProgressEvent>().Subscribe(OnProgress, ThreadOption.UIThread, true);
```

启动台订阅必须指定 `ThreadOption.UIThread`（UI 绑定更新），且传 `keepSubscriberReferenceAlive: true`；主窗口订阅（MainWindowViewModel 第 48-51 行）用默认线程选项，因为发布方当前都在 UI 线程上下文发布。

### 一次性请求/响应（失败决策）

```csharp
// FrameworkApplication.cs 第 121-129 行：发布失败事件后阻塞等待决策
var actionEvent = eventAggregator.GetEvent<StartupFailureActionEvent>();
var completion = new TaskCompletionSource<StartupFailureAction>(TaskCreationOptions.RunContinuationsAsynchronously);
var token = actionEvent.Subscribe(action => completion.TrySetResult(action));
var action = await completion.Task;
actionEvent.Unsubscribe(token);
return action == StartupFailureAction.Continue;
```

这是把 PubSub 事件当一次性 RPC 用的范式：订阅 → await → 退订。`TaskCreationOptions.RunContinuationsAsynchronously` 避免续体在发布者线程内联执行。

## 初始化/生命周期要求

- 无任何初始化要求；事件类型是纯声明，`GetEvent<T>()` 由 Prism 在首次调用时创建实例。
- 订阅者生命周期：DashBoard 侧显式传 `keepSubscriberReferenceAlive: true`；MainWindowViewModel 用默认（弱引用）——订阅方需保证自身生命周期长于事件源，否则弱引用订阅会被 GC 静默回收（见 pitfalls.md）。
