# Abstractions — 不变量与陷阱

## 隐含不变量（全部仅由 XML 注释约定，代码无法强制）

1. **`Id` 唯一性均为全局唯一**（最易错）：
   - `IMainViewContribution.Id`（Contributions/IMainViewContribution.cs 第 14 行）：**跨模块全局唯一**，注释建议以模块名做前缀（如 `dashboard.overview`），shell 按 Id 索引全部贡献——撞 Id 会被覆盖/错乱。
   - `ToolViewAttribute.Id`（Contributions/ToolViewAttribute.cs 第 35 行）：稳定标识，**全局唯一**，约定模块名前缀（如 `"shell.outline"`）；同一程序集内 Id 重复时 `RegisterToolViews` 记 `Logger.Warning` 并跳过（ADR-0002），跨程序集撞 Id 无防护。
   - `IStatusBarItemContribution.Id`：全局唯一。`IMenuItemContribution` **无 `Id`**（ADR-0001：菜单链路无消费方，定位完全由路径 + 分组 + 位次表达）。
2. **`Order` 排序小者靠前**：全部贡献契约一致（「排序权重，小者靠前」），但排序作用域不同——`ToolViewContribution.Order` 在同一 Bar 内（`Placement` 只是默认归属，用户拖拽后以持久化布局为准，ADR-0002）；`IMenuItemContribution` 的 `Order` 只作用于**同一（子）菜单的同一组内**，其上有 `GroupOrder`（组间）与 `NodeOrder`（顶层菜单/末端子菜单节点在父级中）两级位次，冲突时多处声明取最小值（规则全文见 api.md 与 ADR-0001）。
3. **`IconPath`/`Icon` 格式**：必须是 `StreamGeometry` 的 path 字符串，由 `PathIcon` 消费并随主题变色（`IStatusBarItemContribution.IconPath`、`IMenuItemContribution.IconPath`、`ToolViewContribution.IconPath`、`ToolViewAttribute.Icon` 注释一致）；传图像路径/资源 key 会静默不显示。
4. **视图类型必须注册到 DI 容器**：`ViewType` 注释为「经容器解析以支持依赖注入」；返回未注册的 `Type` 会在 shell 侧解析时失败。工具视图的 View 类型由 `RegisterToolViews` 扫描时自动 `Register(viewType)`，无需模块另行注册；主视图的 `ViewType` 仍需模块自己注册。
5. **注册方式约定**：主视图/状态栏两接口注释一致——模块须「在 `Prism.Ioc.IContainerRegistry` 中以本接口注册实现」，以**接口**而非具体类型注册，shell 才能按接口收集。工具视图（ADR-0002）：不手写贡献类，View 类标 `ToolViewAttribute` 后在 `RegisterTypes` 调 `RegisterToolViews(Assembly)`，由 Framework 侧扫描生成 `ToolViewContribution` 元数据（单例注册）并注册 View 类型；非可实例化 `Control` 的标注类记 `Logger.Warning` 跳过。菜单例外（ADR-0001）：`IMenuItemContribution` 通常不手写实现，模块类标注 `MenuGroupAttribute`/`MenuItemAttribute` 后在 `RegisterTypes` 调 `RegisterMenus(Assembly)`，由 Framework 侧扫描生成实现并以接口注册。
6. **面板收起行为**：「面板收起期间其 tab 的激活操作会被 ShellLayoutState 拒绝」（Contributions/ToolViewContribution.cs 注释）——激活操作不是异常而是被拒绝，调用方不要依赖激活必然生效。
7. **菜单路径与分组语义**（ADR-0001）：路径段为 Language 资源键，各段 Trim 后按序精确匹配（Ordinal 大小写敏感）；含空段（`"A//B"`）的路径整体非法，扫描时记日志跳过；`MenuGroupAttribute` 的 `Group`/`GroupOrder`/`Order` 在单段路径与多段路径下语义不同（单段=方法项分组 + 顶层位次；多段=末端子菜单节点分组与位次，方法项进默认组）——深层子菜单内部分组必须拆类声明，一个 attribute 装不下两套分组参数。`MenuGroupAttribute.Order` 缺省 `int.MaxValue`（未声明 = 「无位次意见」，排最后且不参与取最小）——不要凭「数值缺省 0」的直觉推断位次，也别指望不写 `Order` 的类能抢前；`GroupOrder` 无此特例，缺省就是 0。

## 易错改法

1. **给贡献接口加属性**：本模块是解决方案最底层契约，接口加成员会同时破坏所有模块的实现类与 shell 的 mock/测试替身，且 C# 接口无默认实现时编译错误散布全解决方案。改前先全解决方案编译评估。
2. **修「拼写错误」`WindowManagerExtenstion` / `IWindowManagerExtenstion.cs`**（WindowManager/ 目录）：类名与文件名的 "Extenstion" 是既有拼写，所有调用方以该名引用；贸然改名是跨程序集破坏性变更，收益仅是美观。
3. **`ShowWindow<TWindow>(this IWindowManager, object)` 与 `ShowDialog<TWindow>(this IWindowManager, object)` 补泛型约束**（IWindowManagerExtenstion.cs 第 35、43 行）：这两个重载缺 `where TWindow : Window`（同文件其余方法都有），看似「明显疏漏」；补约束是正确修复，但属签名收紧，已用非 Window 类型调用的代码会编译失败。
4. **「统一」`GetWindow` 可空性**：`IWindowManager.GetWindow(Type)`（WindowManager/IWindowManager.cs 第 20 行）返回非空 `Window`，`WindowManagerExtenstion.GetWindow<TWindow>`（第 22 行）返回 `Window?`。把接口也改成 `Window?` 会让所有实现方收到可空性告警；把扩展改成非空则掩盖实现可能返回 null 的事实。
5. **改 `ShellRegions` 常量值**：值经 `nameof` 生成，改标识符即改字符串值；若 shell 布局 XAML 或持久化布局状态中以字符串引用 Region 名，会静默失配（编译期不报错）。
6. **以为 `ShowWindow(Window)` 实例版注释「对话框窗口」是语义**：IWindowManager.cs 中 `ShowWindow(Window window)` 的 XML 注释误写为「显示指定类型的对话框窗口」（与 ShowDialog 注释雷同），实际是非对话框的实例版重载——按注释理解行为会被误导。
7. **`[MenuItem("...")]` 短名与 Avalonia `MenuItem` 控件歧义**：`MenuItemAttribute`（Menus/MenuItemAttribute.cs）的 attribute 短名 `MenuItem` 与 `Avalonia.Controls.MenuItem` 控件同名——菜单类文件若同时 `using Avalonia.Controls;`（例如需要 `Window`/`Separator` 等类型时），`[MenuItem(...)]` 编译报歧义。规避：菜单类文件避免 `using Avalonia.Controls;`（现有菜单类如 Modules/Workstation 的 Menus/FileMenus.cs 只 using `Avalonia` 与 `Avalonia.Controls.ApplicationLifetimes`），或写全名 `[MenuItemAttribute(...)]`。

## 历史踩坑线索

- `IWindowManagerExtenstion.cs` 类注释自称「窗口管理器扩展方法，提供泛型版本的窗口管理操作」，但文件名前缀是 `I`（`IWindowManagerExtenstion.cs`）而类名无 `I`——文件命名与类命名不一致，按文件名找接口会扑空。
- 防御性线索：泛型扩展层用 `Window?` 返回（`GetWindow<TWindow>`），暗示历史上 `GetWindow(Type)` 实现确实返回过 null，接口的非空标注与实际行为脱节。
- 注释中引用的 `Prism.Ioc.IContainerRegistry`、`ShellLayoutState`、`OpenMainViewEvent` 都不在本程序集（本模块未引用 Prism），`<see cref>` 无法解析——文档注释与程序集引用脱节，说明这些注释是写给实现侧读者的约定，不是编译期可验证的事实。
