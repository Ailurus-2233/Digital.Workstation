# Abstractions — 对外接口与调用方式

命名空间六组：`DigitalWorkstation.Core.Abstractions.Contributions`（Contributions/ 目录，主视图/状态栏两接口 + 工具视图枚举/attribute/元数据三类型）、`DigitalWorkstation.Core.Abstractions.Menus`（Menus/ 目录，菜单路径/分组模型三类型）、`DigitalWorkstation.Core.Abstractions.Commands`（Commands/ 目录，命令契约 + 注册 attribute 两类型，ADR-0005）、`DigitalWorkstation.Core.Abstractions.Regions`（Regions/ 目录，`ShellRegions` 与 `WellKnownViews` 两个常量类）、`DigitalWorkstation.Core.Abstractions.Settings`（Settings/ 目录，设置分组/设置项两 attribute + 两元数据类 + `ISettingsService`，ADR-0006）与 `DigitalWorkstation.Core.Abstractions.WindowManager`（WindowManager/ 目录）。全部为 `public`；项目无 internal 类型。

## Shell 贡献契约（Contributions/、Menus/ 与 Commands/，Region 与主视图 Id 常量在 Regions/）

### `ShellRegions`（static class，Regions/ShellRegions.cs）

Prism Region 名称常量，值均经 `nameof` 生成：

| 常量 | 值 | 语义 |
|---|---|---|
| `ActivityBar` | `"ActivityBar"` | 工作区最左侧竖向导航栏 |
| `SideBar` | `"SideBar"` | ActivityBar 右侧容器，显示当前选中导航项内容 |
| `MainContent` | `"MainContent"` | 工作区中央主 Region，单视图切换 |
| `AuxiliaryPanel` | `"AuxiliaryPanel"` | 工作区右侧 tab + 容器区域 |
| `BottomPanel` | `"BottomPanel"` | 工作区底部 tab + 容器区域 |

注意：`ShellRegions` 目前**全仓零消费方**，属存量公共契约；本次目录拆分只挪位置，类型名（`ShellRegions`）与常量值不变。

### `WellKnownViews`（static class，Regions/WellKnownViews.cs）

shell 与模块共同知晓的主视图 Id 常量（ADR-0006 决策 5）：shell 侧导航按钮与贡献主视图的模块都引用本常量，从而互不依赖。与 `ShellRegions` 的 `nameof` 惯例不同，值为字面量字符串（主视图 Id 不是代码标识符）：

| 常量 | 值 | 语义 |
|---|---|---|
| `Settings` | `"settings.main"`（第 13 行） | 设置页主视图 Id：由 Settings 模块以 `IMainViewContribution` 贡献，shell 左下角"设置"导航按钮经 `OpenMainViewEvent` 以本 Id 打开 |

### `ToolViewPlacement`（enum，Contributions/ToolViewAttribute.cs 第 6-22 行）

工具视图默认栖身的 Bar：`ActivityBar`（内容显示在 SideBar）/ `AuxiliaryPanel`（右侧面板）/ `BottomPanel`（底部面板）。

### `ToolViewAttribute`（Contributions/ToolViewAttribute.cs）

`[AttributeUsage(AttributeTargets.Class)]`（第 29 行），声明一个 View 类是**工具视图（Tool View）**（ADR-0002 `docs/adr/0002-toolview-drag-persistence.md`）：带图标与标题的可停靠界面单元。模块在 `RegisterTypes` 调 `RegisterToolViews(Assembly)`（Framework 侧 `ToolViewRegistration`）扫描注册；`Default` 只是默认归属——用户拖拽后的实际归属以持久化布局为准。主构造参 `string id, string titleKey`（第 30 行）。

| 成员 | 类型 | 语义 |
|---|---|---|
| `Id`（构造参，get-only） | `string` | 稳定标识，全局唯一，约定模块名前缀（如 `"shell.outline"`） |
| `TitleKey`（构造参，get-only） | `string` | 显示标题的 Language 资源键，注册时解析，缺键回退键名本身 |
| `Icon`（命名属性） | `string?` | 图标的 StreamGeometry path 字符串（取 `Icons` 常量）；`null` = 无图标 |
| `Default`（命名属性） | `ToolViewPlacement` | 默认栖身的 Bar；**缺省 `AuxiliaryPanel`**（第 50 行） |
| `Order`（命名属性） | `int` | 同一 Bar 内的默认排序权重，小者靠前 |
| `AllowMove`（命名属性） | `bool` | 是否允许用户拖拽迁移；**缺省 `true`**（第 60 行）；`false` 且 `Default` 为 `ActivityBar` 时钉在 ActivityBar 底部段（钉住项，Pinned Item） |

### `IMainViewContribution`（Contributions/IMainViewContribution.cs）

模块向 MainContent 贡献主视图。

- `string Id { get; }` — 稳定标识，**跨模块全局唯一**；shell 按 Id 索引全部贡献；注释建议以模块名做前缀（如 `dashboard.overview`）
- `Type ViewType { get; }` — 打开时 MainContent 显示的视图类型，经容器解析

调用链：SideBar 内交互或 shell"设置"导航按钮发出 `OpenMainViewEvent`（负载为 `Id`，设置按钮用 `WellKnownViews.Settings`，ADR-0006 决策 6）→ shell 找到对应贡献 → 容器解析 `ViewType` → 替换 MainContent 当前视图。

### `ToolViewContribution`（sealed class，Contributions/ToolViewContribution.cs）

工具视图的贡献元数据（ADR-0002）：**不由模块手写**，由 Framework 侧 `RegisterToolViews` 扫描 `ToolViewAttribute` 生成并以单例注册进容器；shell 收集后渲染到 `Placement` 对应的 Bar；激活时经容器解析 `ViewType` 显示内容。全部属性为 `required init`（第 14-44 行）。

| 属性 | 类型 | 语义 |
|---|---|---|
| `Id` | `string` | 稳定标识，全局唯一 |
| `Title` | `string` | 显示标题，**已按当前 UI 区域性解析**（非资源键） |
| `IconPath` | `string?` | 图标 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色；`null` = 无图标 |
| `Order` | `int` | 同一 Bar 内的默认排序权重，小者靠前 |
| `Placement` | `ToolViewPlacement` | 默认栖身的 Bar；用户拖拽后的实际归属以持久化布局为准 |
| `AllowMove` | `bool` | 是否允许用户拖拽迁移 |
| `ViewType` | `Type` | 内容视图类型，经容器解析以支持依赖注入 |

注释声明不变量：面板收起期间其 tab 的激活操作会被 `ShellLayoutState` 拒绝（ToolViewContribution.cs 第 7 行）。

### `IMenuItemContribution`（Menus/IMenuItemContribution.cs）

模块向菜单栏贡献菜单项的契约（路径/分组模型，见 ADR-0001 `docs/adr/0001-attribute-menu-registration.md`）。文件 `using System.Windows.Input;`。**通常不直接实现本接口**：模块用 `MenuGroupAttribute`/`MenuItemAttribute` 标注普通类（见下两节），经 Framework 侧 `MenuRegistration.RegisterMenus` 扫描后生成本契约的实现注册进容器；shell 收集全部实现后由 `MenuTreeBuilder` 建树（分组排序、组间分隔线）并渲染。

| 属性 | 类型 | 语义与排序规则 |
|---|---|---|
| `Title` | `string` | 显示标题，**已按当前 UI 区域性解析**（非资源键） |
| `IconPath` | `string?` | 图标 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色；`null` = 无图标 |
| `Path` | `string` | 完整菜单路径：`"/"` 分隔，段为 Language 资源键，首段为顶层菜单；支持任意深度子菜单 |
| `Group` | `string?` | 单段路径：本条目在该菜单内的组；多段路径：末端子菜单节点在其父菜单内的组。`null` = 默认组（排在命名组之前） |
| `GroupOrder` | `int` | 组的排序权重，小者靠前；同名组多处声明冲突时取最小值 |
| `NodeOrder` | `int` | 顶层菜单（单段路径）或末端子菜单节点（多段路径）在父级中的排序权重；多处声明取最小值 |
| `Order` | `int` | 条目在组内的排序权重，小者靠前；同 `Order` 按解析后的 `Title` 字典序（Ordinal） |
| `Command` | `ICommand` | 点击菜单项执行的命令（`System.Windows.Input.ICommand`） |

与旧模型的差异（ADR-0001）：删除 `Id`（菜单链路无任何消费方）与「追加到哪个顶层菜单」的封闭枚举定位；定位完全由 `Path` + `Group`/`GroupOrder` + `NodeOrder`/`Order` 表达，可表达多级子菜单、命名分组与组间自动分隔线。建树与排序语义（顶层不分组、子菜单组间插分隔线、位次冲突取最小）在 Framework 侧 `MenuTreeBuilder`，不在本程序集。

### `MenuGroupAttribute`（Menus/MenuGroupAttribute.cs）

`[AttributeUsage(AttributeTargets.Class)]`（第 12 行），声明一个菜单类：类中标注 `MenuItemAttribute` 的公共实例方法成为菜单项，经 `MenuRegistration.RegisterMenus` 扫描注册。主构造参 `string path`（第 13 行）。

| 成员 | 类型 | 语义 |
|---|---|---|
| `Path`（构造参，get-only） | `string` | 菜单路径：`"/"` 分隔的多级 Language 资源键，首段为顶层菜单；各段 Trim 后按序精确匹配（Ordinal 大小写敏感） |
| `Group`（命名属性） | `string?` | 分组名；`null` = 默认组（GroupOrder 视为 0，排在命名组之前） |
| `GroupOrder`（命名属性） | `int` | 组的排序权重，小者靠前；同名组多处声明冲突时取最小值 |
| `Order`（命名属性） | `int` | 顶层菜单或末端子菜单节点在父级中的排序权重；多处声明取最小值。**缺省 `int.MaxValue`**（MenuGroupAttribute.cs 第 34 行）：未声明视为「无位次意见」，排最后，且不参与多处声明取最小——防止忘写 `Order` 的类以缺省 0 把所在菜单钉到最前 |

路径段数决定三个命名属性的语义（ADR-0001 第 10 条）：**单段路径**（如 `"MenuFileTitle"`）时 `Group`/`GroupOrder` 描述方法项在该菜单内的分组，`Order` 描述顶层菜单在菜单栏的位次；**多段路径**（如 `"MenuFileTitle/Export"`）时三者描述末端子菜单节点在其父菜单内的分组与位次，方法项进入末端菜单的默认组。一个 attribute 只有一套分组参数，深层子菜单内部分组需拆类声明。含空段（`"A//B"`）的路径整体非法，扫描时记日志跳过。

### `MenuItemAttribute`（Menus/MenuItemAttribute.cs）

`[AttributeUsage(AttributeTargets.Method)]`（第 7 行），声明一个菜单项，标注在菜单类（`MenuGroupAttribute`）的公共实例方法上。主构造参 `string title`（第 8 行）。

| 成员 | 类型 | 语义 |
|---|---|---|
| `Title`（构造参，get-only） | `string` | 显示标题的 Language 资源键，运行时解析，缺键回退键名本身 |
| `Order`（命名属性） | `int` | 同组内的排序权重，小者靠前；同 `Order` 按解析后的标题字典序 |
| `Icon`（命名属性） | `string?` | 图标的 StreamGeometry path 字符串（取 `Icons` 常量）；`null` = 无图标 |

方法签名仅支持无参 `void M()` 与 `Task M()`；非法签名（带参、返回值非 `void`/`Task`）在扫描时记 `Logger.Warning` 跳过（ADR-0001 第 7 条）。

### `ICommandContribution`（Commands/ICommandContribution.cs）

模块向全局命令列表贡献命令的契约（扁平模型，见 ADR-0005 `docs/adr/0005-command-registration-palette.md`）。文件 `using System.Windows.Input;`。**通常不直接实现本接口**：模块用 `CommandAttribute` 标注普通类的方法（见下节），经 Framework 侧 `CommandRegistration.RegisterCommands` 扫描后生成本契约的实现注册进容器；shell 收集全部实现后交给命令面板呈现，并为带 `Gesture` 的命令生成窗口级 KeyBinding。

| 属性 | 类型 | 语义与排序规则 |
|---|---|---|
| `Id` | `string` | 稳定标识：默认「声明类全名.方法名」，可经 attribute 覆盖；全局唯一，冲突时后注册者被丢弃并记日志。MRU 记忆与键绑定引用的依据 |
| `Title` | `string` | 显示标题，**已按当前 UI 区域性解析**（非资源键） |
| `Gesture` | `string?` | 快捷键文本（如 `"Ctrl+Shift+P"`）；`null` = 无快捷键。解析为窗口级 KeyBinding 由 Framework 侧 `FrameworkWindow.RegisterCommandGestures` 负责 |
| `IconPath` | `string?` | 图标 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色；`null` = 无图标（惯例同菜单，ADR-0001 决策 8） |
| `Order` | `int` | 命令列表中的排序权重，小者靠前；同 `Order` 按解析后的 `Title` 字典序（Ordinal） |
| `Command` | `ICommand` | 执行命令（`System.Windows.Input.ICommand`），命令面板选中或快捷键触发时调用 |

与菜单契约的差异（ADR-0005 决策 1）：命令是扁平列表成员——有稳定 `Id`、无 `Path`/`Group` 定位；两套体系互不相干，菜单项不进命令面板。图标惯例与菜单一致（`IconPath?`，`null` = 无图标）。

### `CommandAttribute`（Commands/CommandAttribute.cs）

`[AttributeUsage(AttributeTargets.Method)]`（第 8 行），声明一个命令，标注在**任何类**的公共实例方法上（免类级 attribute，ADR-0005 决策 2）。主构造参 `string title`（第 9 行）。

| 成员 | 类型 | 语义 |
|---|---|---|
| `Title`（构造参，get-only） | `string` | 显示标题的 Language 资源键，收集时解析，缺键回退键名本身 |
| `Id`（命名属性） | `string?` | 稳定标识；`null` = 默认「声明类全名.方法名」 |
| `Icon`（命名属性） | `string?` | 图标的 StreamGeometry path 字符串（取 `Icons` 常量）；`null` = 无图标 |
| `Gesture`（命名属性） | `string?` | 快捷键文本（如 `"Ctrl+Shift+P"`）；`null` = 无快捷键 |
| `Order`（命名属性） | `int` | 命令列表中的排序权重，小者靠前；同 `Order` 按解析后的标题字典序 |

方法签名仅支持无参 `void M()` 与 `Task M()`；非法签名（带参、返回值非 `void`/`Task`）在扫描时记 `Logger.Warning` 跳过（与菜单同规则）。

### `IStatusBarItemContribution`（Contributions/IStatusBarItemContribution.cs）

模块向状态栏追加条目，shell 渲染为「图标 + 文本」的状态指示。

- `string Id { get; }` — 稳定标识，全局唯一
- `string Title { get; }` — 显示文本
- `string IconPath { get; }` — 同上图标约定
- `int Order { get; }` — 排序权重，小者靠前

（此接口**没有**定位枚举和行为字段——状态栏只有一个区域，且条目是纯展示无点击行为。）

## 设置契约（Settings/）

设置项注册契约（ADR-0006 决策 1）：模块类标 `SettingGroupAttribute` 声明分组、公共静态可读属性标 `SettingItemAttribute` 声明设置项，经 Framework 侧 `SettingRegistration.RegisterSettings(Assembly)` 扫描生成 `SettingGroupContribution`/`SettingItemContribution` 元数据（单例注册）；设置值读写一律经 `ISettingsService`（ADR-0006 决策 3），声明属性体不被执行。

### `SettingGroupAttribute`（Settings/SettingGroupAttribute.cs）

`[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]`（第 9 行），声明一个设置分组，标注在任何类上。主构造参 `string name`（第 10 行）。

| 成员 | 类型 | 语义 |
|---|---|---|
| `Name`（构造参，get-only，第 15 行） | `string` | 分组显示名的 Language 资源键，运行时解析，缺键回退键名本身；同名分组全局合并 |
| `Order`（命名属性，第 20 行） | `int` | 分组在设置页分组树中的排序权重，小者靠前；同名多处声明冲突时取最小值（同 ADR-0001 决策 4）。缺省 `0` |

仅被设置项引用而无本 attribute 声明的分组也可用——由设置项引用隐式产生，位次视为 0（收集侧补出）。

### `SettingItemAttribute`（Settings/SettingItemAttribute.cs）

`[AttributeUsage(AttributeTargets.Property)]`（第 9 行），声明一个设置项，标注在公共静态可读属性上。主构造参 `string group, string name`（第 10 行）。**属性只是声明锚点**：属性类型即设置值类型，扫描不读取属性值，读写一律经 `ISettingsService`；非公共/非静态/无 getter 的属性静默忽略（同菜单/命令扫描惯例）。

| 成员 | 类型 | 语义 |
|---|---|---|
| `Group`（构造参，get-only，第 21 行） | `string` | 所属分组的名称键（`SettingGroupAttribute.Name`），按名称全局合并归组 |
| `Name`（构造参，get-only，第 26 行） | `string` | 设置项显示名的 Language 资源键，运行时解析，缺键回退键名本身（ADR-0006 决策 2） |
| `Id`（命名属性，第 16 行） | `string?` | 稳定标识；`null` = 默认「声明类全名.属性名」（仿命令 Id 规则，ADR-0005）；全局唯一，是 settings.json 的 key 与 `ISettingsService` 读写的依据 |
| `DefaultValue`（命名属性，第 33 行） | `object?` | 默认值：用户从未修改时 `Get<T>` 的返回值（ADR-0006 决策 3）；必须是属性类型的编译期常量（attribute 实参限制），类型不匹配扫描时记日志跳过；枚举成员显示名走 Language 资源键，键按「设置项名称键 + 成员名」约定生成（决策 8）。缺省 `null` |
| `Order`（命名属性，第 38 行） | `int` | 同分组内的排序权重，小者靠前；同 `Order` 按名称键字典序。缺省 `0` |
| `RequiresRestart`（命名属性，第 43 行） | `bool` | 是否需重启生效：修改后值立即落盘、当前进程行为不变、下次启动由消费方读取生效（ADR-0006 决策 7）。缺省 `false` |

### `SettingGroupContribution`（sealed class，Settings/SettingGroupContribution.cs）

设置分组的贡献元数据：**不由模块手写**，由 Framework 侧 `RegisterSettings` 扫描 `SettingGroupAttribute` 生成并注册进容器；收集时按 `Name` 全局合并（多处声明取最小 `Order`），仅被设置项引用而无声明的分组由收集侧补出（`Order` 视为 0）。全部属性为 `required init`（第 13、18 行）。

| 属性 | 类型 | 语义 |
|---|---|---|
| `Name` | `string` | 分组显示名的 Language 资源键（**非已解析文案**），运行时经 Language.Get 解析，缺键回退键名本身 |
| `Order` | `int` | 分组在设置页分组树中的排序权重，小者靠前；同名多处声明取最小值 |

### `SettingItemContribution`（sealed class，Settings/SettingItemContribution.cs）

设置项的贡献元数据：**不由模块手写**，由 Framework 侧 `RegisterSettings` 扫描 `SettingItemAttribute` 生成并注册进容器；设置页据此渲染编辑器（控件由 `ValueType` 推断，ADR-0006 决策 8），`ISettingsService` 据此取 `DefaultValue` 作为未修改时的读值。全部属性为 `required init`（第 13-43 行）。

| 属性 | 类型 | 语义 |
|---|---|---|
| `Id` | `string` | 稳定标识，全局唯一：默认「声明类全名.属性名」；settings.json 的 key 与 `ISettingsService` 读写的依据 |
| `Group` | `string` | 所属分组的名称键（`SettingGroupAttribute.Name`），按名称全局合并归组 |
| `Name` | `string` | 设置项显示名的 Language 资源键（**非已解析文案**），缺键回退键名本身 |
| `ValueType` | `Type` | 设置值类型（声明属性的类型）：编辑器推断与 JSON 反序列化的依据 |
| `DefaultValue` | `object?` | 默认值：用户从未修改时的取值，不是单独存储层（ADR-0006 决策 3） |
| `Order` | `int` | 同分组内的排序权重，小者靠前；同 `Order` 按 `Name` 键字典序 |
| `RequiresRestart` | `bool` | 是否需重启生效：修改后值立即落盘、当前进程行为不变、下次启动生效（ADR-0006 决策 7） |

### `ISettingsService`（Settings/ISettingsService.cs）

设置值读写服务（ADR-0006 决策 3）：Framework 实现，启动时一次性把 settings.json 加载入内存；读纯走内存——已修改取用户值，未修改取声明的默认值（默认值不是单独存储层）；写 = 更新内存 + 防抖落盘 + 广播 `SettingChangedEvent`（事件契约在 Core/Models）。另承载「重启后生效」判定（决策 7）。

| 成员 | 签名（行号） | 语义 |
|---|---|---|
| `Get` | `T? Get<T>(string settingId)`（第 15 行） | 读取设置值：已修改返回用户值，未修改返回声明的默认值，未声明（含持久化值反序列化失败回退后仍无声明）返回 `T` 的默认值并记日志 |
| `Set` | `void Set<T>(string settingId, T value)`（第 21 行） | 写入设置值：立即更新内存、防抖落盘 settings.json、广播变更事件；对 `RequiresRestart` 的设置项，当前进程行为不变，下次启动生效 |
| `IsPendingRestart` | `bool IsPendingRestart(string settingId)`（第 28 行） | 本次进程内该设置项的值是否已偏离进程启动时的生效值（决策 7）：「重启后生效」项级标记与重启横幅的判定依据；改回启动值即恢复为 false。调用方自行结合 `SettingItemContribution.RequiresRestart` 过滤——服务不感知该元数据 |

## 视图类型属性（ViewType）

「激活时要显示哪个视图」这一能力由 `System.Type` 属性承载（注释均为「经容器解析以支持依赖注入」）。原 `ContentViewType`（已删的导航项/面板 tab 接口）已不复存在，命名统一为 `ViewType`：

| 属性名 | 所在类型（文件） | 激活效果 |
|---|---|---|
| `ViewType` | `ToolViewContribution`（Contributions/ToolViewContribution.cs 第 44 行） | 点击导航项时 SideBar 显示、激活面板 tab 时面板内容区显示解析出的视图 |
| `ViewType` | `IMainViewContribution`（Contributions/IMainViewContribution.cs 第 19 行） | 打开时**替换** MainContent 当前视图 |

其余贡献类型（`IMenuItemContribution` 用 `ICommand Command`、`IStatusBarItemContribution` 无行为字段）不携带视图类型。

## 窗口管理契约（WindowManager/）

### `IWindowManager`（WindowManager/IWindowManager.cs）

以「窗口显示与隐藏管理」为职责，11 个方法，`using Avalonia.Controls;`：

- `Window GetWindow(Type type)` — 从容器解析得到指定类型窗口实例
- `void ShowWindow(Type type)` / `void ShowWindow(Type type, object dataContext)`
- `void ShowWindow(Window window)` / `void ShowWindow(Window window, object dataContext)` — 实例版重载（XML 注释写的是「对话框窗口」，与 `ShowDialog` 实例版注释雷同，属注释瑕疵）
- `void ShowDialog(Type type)` / `void ShowDialog(Type type, object dataContext)`
- `void ShowDialog(Window window)` / `void ShowDialog(Window window, object dataContext)`
- `void CloseWindow(Type type)` — 关闭指定类型窗口
- `void HideWindow(Type type)` — 隐藏指定类型窗口

返回类型注意：`GetWindow(Type)` 返回非空 `Window`（`WindowManagerExtenstion.GetWindow<TWindow>` 却声明返回 `Window?`——可空性标注在两层不一致，见 pitfalls.md）。

### `IMainWindowManager`（WindowManager/IMainWindowManager.cs）

- `void HandleMainWindow()` — 处理主窗口的显示与隐藏
- `void HideMainWindow()` — 隐藏主窗口
- `void ShowMainWindow()` — 显示主窗口
- `void CloseWindowsExceptMain()` — 关闭除主窗口外的所有窗口

### `WindowManagerExtenstion`（static class，WindowManager/IWindowManagerExtenstion.cs）

文件名与类名中 "Extenstion" 为源码原始拼写（Extension 的拼写错误）。泛型便捷方法，全部转发到 `IWindowManager` 的 `Type` 版方法：

- `Window? GetWindow<TWindow>(this IWindowManager manager) where TWindow : Window` → `manager.GetWindow(typeof(TWindow))`
- `void ShowWindow<TWindow>(this IWindowManager manager) where TWindow : Window`
- `void ShowWindow<TWindow>(this IWindowManager manager, object dataContext)` — **无** `where TWindow : Window` 约束
- `void ShowDialog<TWindow>(this IWindowManager manager) where TWindow : Window`
- `void ShowDialog<TWindow>(this IWindowManager manager, object dataContext)` — **无**约束
- `void HideWindow<TWindow>(this IWindowManager manager) where TWindow : Window`
- `void CloseWindow<TWindow>(this IWindowManager manager) where TWindow : Window`

## 调用方式与生命周期

- **贡献契约**：无主动调用方 API。接口类贡献（`IMainViewContribution`/`IStatusBarItemContribution`）由模块实现接口并在 `Prism.Ioc.IContainerRegistry` 以接口注册（生命周期由模块注册方式决定），shell 收集消费；工具视图/菜单/命令/设置例外——模块在 `RegisterTypes` 分别调 `RegisterToolViews(Assembly)`（View 类标 `ToolViewAttribute`）、`RegisterMenus(Assembly)`（菜单类标 `MenuGroupAttribute`/`MenuItemAttribute`）、`RegisterCommands(Assembly)`（方法标 `CommandAttribute`，免类级标记）、`RegisterSettings(Assembly)`（类标 `SettingGroupAttribute`、公共静态可读属性标 `SettingItemAttribute`），由 Framework 侧扫描生成 `ToolViewContribution` 元数据/`IMenuItemContribution`/`ICommandContribution` 实现/`SettingGroupContribution`/`SettingItemContribution` 元数据并注册；设置值读写则由消费方注入 `ISettingsService` 调 `Get<T>`/`Set<T>`。本模块内无调用点——本程序集是纯定义层，典型调用序列发生在 shell/Framework 与其他模块（不在本模块范围）。
- **窗口管理**：调用方注入 `IWindowManager`/`IMainWindowManager`，调 `ShowWindow<MyDialog>(vm)` 这类泛型扩展或直接 `ShowWindow(typeof(MyDialog), vm)`。窗口实例来源是 DI 容器（`GetWindow` 注释：「从容器中解析得到的窗口实例」）。
- **数据结构**：本模块不定义任何 DTO/记录类；对外数据完全由上述接口属性承载，字段语义见上。
