# Models — 文件结构与功能

## 目录树（相对 `Core/Models`）

```
Models.csproj                  项目文件：net10.0、ImplicitUsings/Nullable、仅引用 ..\Common\Common.csproj
Events/
  StartupPhase.cs              启动阶段枚举（ADR-0004）
  StartupProgress.cs           启动进度负载 record
  StartupProgressEvent.cs      启动进度事件
  ModuleLoadFailure.cs         模块加载失败负载 record
  ModuleLoadFailedEvent.cs     模块加载失败事件
  StartupFailureAction.cs      用户决策枚举（继续/退出）
  StartupFailureActionEvent.cs 用户决策事件
  OpenMainViewEvent.cs         打开主视图请求事件
  TogglePanelTarget.cs         目标面板枚举
  TogglePanelVisibilityEvent.cs 面板显隐请求事件
  ResetLayoutEvent.cs          重置布局请求事件（ADR-0002，无负载）
  SettingChanged.cs            设置项变更负载 record（ADR-0006 决策 3）
  SettingChangedEvent.cs       设置项变更事件（ADR-0006 决策 3）
obj/、Output/                  构建产物（不入库语义；obj 下的 Models.GlobalUsings.g.cs 是 Prism global using 的证据）
```

全部源码文件归属命名空间 `DigitalWorkstation.Core.Models.Events`（file-scoped namespace，每文件第 1 行）。

## 逐文件功能

### Models.csproj

项目定义：`<TargetFramework>net10.0</TargetFramework>`、`<ImplicitUsings>enable</ImplicitUsings>`、`<Nullable>enable</Nullable>`（第 4-6 行）；唯一 ProjectReference 指向 `..\Common\Common.csproj`（第 10 行），经传递获得 Prism 程序集与 `Prism.Events` 等 global using。无包引用、无编译项定制。

### Events/StartupPhase.cs

`public enum StartupPhase`（第 6 行）——三成员：`CoreServices`（初始化核心服务）、`LoadingModules`（逐模块加载）、`Ready`（全部就绪）。注释声明 Launcher 引导（Avalonia 启动前）不在进度覆盖范围内。

### Events/StartupProgress.cs

`public record StartupProgress(StartupPhase Phase, string? ModuleName, int ModuleIndex, int ModuleCount)`（第 10 行）——启动进度负载。约定：`ModuleName` 仅 LoadingModules 阶段非 null；`ModuleIndex` 从 1 开始。

### Events/StartupProgressEvent.cs

`public class StartupProgressEvent : PubSubEvent<StartupProgress>;`（第 7 行）——启动进度事件，覆盖核心服务初始化与逐模块加载；发布方为框架启动序列，订阅方为启动台。

### Events/ModuleLoadFailure.cs

`public record ModuleLoadFailure(string ModuleName, int ModuleIndex, int ModuleCount, string ErrorMessage)`（第 10 行）——模块加载失败负载，`ModuleIndex` 从 1 开始。

### Events/ModuleLoadFailedEvent.cs

`public class ModuleLoadFailedEvent : PubSubEvent<ModuleLoadFailure>;`（第 7 行）——模块加载失败事件（ADR-0004）；启动序列捕获异常后发布，启动台订阅显示错误并提供"继续/退出"。

### Events/StartupFailureAction.cs

`public enum StartupFailureAction`（第 6 行）——两成员：`Continue`（跳过失败模块继续加载）、`Exit`（终止应用）。

### Events/StartupFailureActionEvent.cs

`public class StartupFailureActionEvent : PubSubEvent<StartupFailureAction>;`（第 7 行）——用户决策事件；仅由启动台"继续/退出"按钮发布，启动序列等待该决策。

### Events/OpenMainViewEvent.cs

`public class OpenMainViewEvent : PubSubEvent<string>;`（第 7 行）——请求 MainContent 打开指定主视图，负载为主视图 Id（`IMainViewContribution.Id`）。注释明确边界：**由 SideBar 内交互与 shell 的"设置"导航按钮（ADR-0006 决策 6）发布；ActivityBar 导航切换不发布本事件，MainContent 保持不变**。

### Events/TogglePanelTarget.cs

`public enum TogglePanelTarget`（第 6 行）——三成员：`SideBar`、`AuxiliaryPanel`、`BottomPanel`。

### Events/TogglePanelVisibilityEvent.cs

`public class TogglePanelVisibilityEvent : PubSubEvent<TogglePanelTarget>;`（第 7 行）——请求翻转指定面板可见性；由视图菜单的面板显隐切换项（`Modules/Workstation/Menus/ViewPanelMenus.cs` 的三个 `[MenuItem]` 方法）发布，主窗口订阅后与快捷键走同一状态转换（`MainWindowViewModel.TogglePanel`）。

### Events/ResetLayoutEvent.cs

`public class ResetLayoutEvent : PubSubEvent;`（第 7 行）——请求重置布局（ADR-0002）：删除持久化布局配置并按 attribute 默认重建 shell 布局。无负载，是模块中唯一继承非泛型 `PubSubEvent` 的事件；由视图菜单的"重置布局"项（`Modules/Workstation/Menus/ViewLayoutMenus.cs` 第 16 行）发布，主窗口（`MainWindowViewModel` 构造函数第 51 行）订阅后重建 State。

### Events/SettingChanged.cs

`public record SettingChanged(string SettingId, object? NewValue)`（第 9 行）——设置项变更负载（ADR-0006 决策 3）：`SettingId` 为设置项 Id（`SettingItemContribution.Id`），`NewValue` 为装箱后的新值（类型由设置项声明的 `ValueType` 决定）。

### Events/SettingChangedEvent.cs

`public class SettingChangedEvent : PubSubEvent<SettingChanged>;`（第 7 行）——设置项变更事件（ADR-0006 决策 3）；由 Framework 的 `SettingsService.Set`（`Core/Framework/Settings/SettingsService.cs` 第 128 行，更新内存 + 防抖落盘后）广播，订阅方为设置页与需重启 UX 等消费方（工单 03 就位，当前源码尚无订阅调用点）。
