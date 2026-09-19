# Abstractions — 文件结构与功能

相对 `Core/Abstractions/` 的目录树（含 csproj；无子目录嵌套超过一层，无测试、无资源文件）：

```
Abstractions.csproj
Commands/
├── CommandAttribute.cs
└── ICommandContribution.cs
Contributions/
├── ToolViewAttribute.cs
├── ToolViewContribution.cs
├── IMainViewContribution.cs
└── IStatusBarItemContribution.cs
Menus/
├── IMenuItemContribution.cs
├── MenuGroupAttribute.cs
└── MenuItemAttribute.cs
Plugins/
└── PluginAttribute.cs
Regions/
├── ShellRegions.cs
└── WellKnownViews.cs
Settings/
├── SettingGroupAttribute.cs
├── SettingItemAttribute.cs
├── SettingGroupContribution.cs
├── SettingItemContribution.cs
└── ISettingsService.cs
WindowManager/
├── IWindowManager.cs
├── IMainWindowManager.cs
└── IWindowManagerExtenstion.cs
```

## 逐文件说明

### Abstractions.csproj

项目文件。`Microsoft.NET.Sdk`，`net10.0`，`ImplicitUsings` + `Nullable` 开启；唯一依赖 `Avalonia 11.3.20`。无 `ProjectReference`。

### Commands/CommandAttribute.cs

定义 `CommandAttribute(Type resourceType, string title)`，`AttributeTargets.Method`；`ResourceType` 标明标题来源，`Title` 为键。命名属性 `Id?`/`Icon?`/`Gesture?`/`Order` 不变。无需类级 attribute，公共实例无参 `void`/`Task` 方法经 Framework 的 `RegisterCommands` 扫描注册；默认 Id 为「声明类全名.方法名」。

### Commands/ICommandContribution.cs

`using System.Windows.Input;`。定义接口 `ICommandContribution`（`Id`/`Title`（已解析，非资源键）/`Gesture?`/`IconPath?`/`Order`/`Command`）——模块向全局命令列表贡献命令的契约（[ADR-0005](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0005-command-registration-palette.md)），扁平模型：有稳定 `Id`（MRU 记忆与键绑定引用的依据，全局唯一），无路径/分组；图标可选，惯例同菜单。命名空间 `DigitalWorkstation.Core.Abstractions.Commands`。

### Regions/ShellRegions.cs

Prism Region 名称常量。定义 `public static class ShellRegions`，含 5 个 `public const string`：`ActivityBar`、`SideBar`、`MainContent`、`AuxiliaryPanel`、`BottomPanel`（值均 `nameof(自身)`）。命名空间 `DigitalWorkstation.Core.Abstractions.Regions`。目前全仓零消费方，属存量公共契约，目录拆分时只挪位置、类型名不变。

### Regions/WellKnownViews.cs

shell 与模块共知的主视图 Id 常量（[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 5）。定义 `public static class WellKnownViews`（第 7 行），目前含 1 个 `public const string`：`Settings = "settings.main"`（第 13 行）——设置页主视图 Id，由 Settings 模块以 `IMainViewContribution` 贡献、shell 左下角"设置"导航按钮经 `OpenMainViewEvent` 引用，双方借本常量互不依赖。命名空间 `DigitalWorkstation.Core.Abstractions.Regions`。与 `ShellRegions` 的 `nameof` 惯例不同，值为字面量字符串。

### Contributions/ToolViewAttribute.cs

定义 `ToolViewPlacement`（`ActivityBar`/`AuxiliaryPanel`/`BottomPanel`）与 `ToolViewAttribute(string id, Type resourceType, string titleKey)`。资源来源显式传入；命名属性 `Icon?`/`Default`（缺省 `AuxiliaryPanel`）/`Order`/`AllowMove`（缺省 `true`）保留。标注 View 类，经 `RegisterToolViews(Assembly)` 扫描注册。

### Contributions/ToolViewContribution.cs

定义 `public sealed class ToolViewContribution`（7 个 `required init` 属性：`Id`/`Title`（已解析，非资源键）/`IconPath?`/`Order`/`Placement`/`AllowMove`/`ViewType`）——工具视图的贡献元数据，由 Framework 侧扫描 `ToolViewAttribute` 生成并以单例注册进容器，模块不手写。

### Contributions/IMainViewContribution.cs

定义接口 `IMainViewContribution`（`Id`/`ViewType`）——模块向 MainContent 贡献主视图的契约；配合 shell 侧 `OpenMainViewEvent`（负载 Id）使用。

### Contributions/IStatusBarItemContribution.cs

定义接口 `IStatusBarItemContribution`（`Id`/`Title`/`IconPath`/`Order`）——模块向状态栏追加「图标 + 文本」状态指示的契约；无定位枚举、无行为字段。

### Menus/IMenuItemContribution.cs

定义接口 `IMenuItemContribution`：`Title`/`IconPath?`/`Path`/`PathTitle?`/`Group?`/`GroupOrder`/`NodeOrder`/`Order`/`Command`。稳定 `Path` 定位，`PathTitle` 提供已解析末端标题，null 表示无标题意见；菜单项本身无 Id、无定位枚举。

### Menus/MenuGroupAttribute.cs

定义 `MenuGroupAttribute(string path)`（仅引用路径）和 `MenuGroupAttribute(string path, Type resourceType, string titleKey)`（声明末端标题），`AttributeTargets.Class`。引用形式的 `ResourceType`/`TitleKey` 都为 null；声明形式成对传入。命名属性 `Group?`/`GroupOrder`/`Order` 保留。模块可引用稳定路径而不引用所有者资源，详见 api.md。

### Menus/MenuItemAttribute.cs

定义 `MenuItemAttribute(Type resourceType, string title)`，`AttributeTargets.Method`，命名属性 `Order`/`Icon?`。标题来源独立于类级 `MenuGroupAttribute`；仅支持公共实例无参 `void`/`Task` 方法，非法签名扫描时记日志跳过。

### Settings/SettingGroupAttribute.cs

定义 `SettingGroupAttribute(string id, Type resourceType, string name)`，`AttributeTargets.Class`、`AllowMultiple=true`。稳定 `Id` 为跨模块归并依据，`Name` 是所属资源的键；`Order` 缺省 0。同 Id 保留首个声明的来源与名称、位次取最小。

### Settings/SettingItemAttribute.cs

定义 `SettingItemAttribute(string group, Type resourceType, string name)`，标注公共静态可读属性。`Group` 是稳定分组 Id，`ResourceType` 提供设置项/枚举名称来源；命名属性 `Id?`/`DefaultValue?`/`Order`/`RequiresRestart` 保留。扫描不执行属性体，值读写经 `ISettingsService`。

### Settings/SettingGroupContribution.cs

定义 `SettingGroupContribution`，4 个 `required init` 属性：`Id`/`ResourceType: Type?`/`Name`/`Order`。扫描生成，收集时按 Id 合并；仅无声明的隐式分组使用 `ResourceType=null`、`Name=Id`、`Order=0`。

### Settings/SettingItemContribution.cs

定义 `SettingItemContribution`，8 个 `required init` 属性：`Id`/`Group`/`ResourceType: Type`/`Name`/`ValueType`/`DefaultValue?`/`Order`/`RequiresRestart`。`Group` 是稳定分组 Id，`Name` 保留资源键；设置页使用同一来源解析名称与枚举选项，`ValueType` 决定编辑器与反序列化类型。

### Settings/ISettingsService.cs

定义接口 `ISettingsService`（3 个方法：`T? Get<T>(string settingId)`、`void Set<T>(string settingId, T value)`、`bool IsPendingRestart(string settingId)`）——设置值读写服务契约（[ADR-0006](https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 3）：Framework 实现，启动一次加载入内存；读纯走内存（未修改取声明默认值），写 = 更新内存 + 防抖落盘 settings.json + 广播 `SettingChangedEvent`（事件契约在 Core/Models）；`IsPendingRestart` 报告本次进程内值是否偏离启动时生效值（决策 7「重启后生效」判定依据）。

### WindowManager/IWindowManager.cs

`using Avalonia.Controls;`。定义接口 `IWindowManager`（11 个方法）：`GetWindow(Type)`；`ShowWindow` 与 `ShowDialog` 各 4 个重载（`Type`/`Type+dataContext`/`Window`/`Window+dataContext`）；`CloseWindow(Type)`；`HideWindow(Type)`。窗口显隐管理契约。

### WindowManager/IMainWindowManager.cs

定义接口 `IMainWindowManager`（4 个方法）：`HandleMainWindow()`、`HideMainWindow()`、`ShowMainWindow()`、`CloseWindowsExceptMain()`。主窗口显隐与批量关闭契约。

### WindowManager/IWindowManagerExtenstion.cs

`using Avalonia.Controls;`。定义 `public static class WindowManagerExtenstion`（"Extenstion" 为源码原始拼写），7 个泛型扩展方法：`GetWindow<TWindow>`、`ShowWindow<TWindow>`（两个重载）、`ShowDialog<TWindow>`（两个重载）、`HideWindow<TWindow>`、`CloseWindow<TWindow>`，全部转发 `IWindowManager` 的 `Type` 版方法。带 dataContext 的两个重载无 `where TWindow : Window` 约束。

### Plugins/PluginAttribute.cs

无参插件入口标记（AttributeTargets.Class、AllowMultiple=false、Inherited=false）；DependsOn 字符串数组声明依赖的内置模块名。纯元数据，不引用 Prism，不负责发现或实例化。
