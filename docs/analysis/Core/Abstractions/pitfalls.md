# Abstractions — 不变量与陷阱

## 隐含不变量（全部仅由 XML 注释约定，代码无法强制）

1. **`Id` 唯一性分级**（最易错）：
   - `INavigationItemContribution.Id`（Shell/INavigationItemContribution.cs）：**同一模块内**唯一即可。
   - `IMainViewContribution.Id`（Shell/IMainViewContribution.cs）：**跨模块全局唯一**，注释建议以模块名做前缀（如 `dashboard.overview`），shell 按 Id 索引全部贡献——撞 Id 会被覆盖/错乱。
   - `IPanelTabContribution.Id`（Shell/IPanelTabContribution.cs）：**跨两个面板**全局唯一（Auxiliary 与 Bottom 共享 Id 空间）。
   - `IMenuItemContribution.Id`、`IStatusBarItemContribution.Id`：全局唯一。
2. **`Order` 排序小者靠前**：五个贡献接口一致（「排序权重，小者靠前」），但排序作用域不同——`INavigationItemContribution` 在同一 `Placement` 内、`IPanelTabContribution` 在同一面板内、`IMenuItemContribution` 在同一菜单内。
3. **`IconPath` 格式**：必须是 `StreamGeometry` 的 path 字符串，由 `PathIcon` 消费并随主题变色（五个接口注释一致）；传图像路径/资源 key 会静默不显示。
4. **视图类型必须注册到 DI 容器**：`ContentViewType`/`ViewType` 注释均为「经容器解析以支持依赖注入」；返回未注册的 `Type` 会在 shell 侧解析时失败。
5. **注册方式约定**：模块须「在 `Prism.Ioc.IContainerRegistry` 中以本接口注册实现」（五个接口注释一致）——以**接口**而非具体类型注册，shell 才能按接口收集。
6. **面板收起行为**：「面板收起期间其 tab 的激活操作会被 ShellLayoutState 拒绝」（Shell/IPanelTabContribution.cs 注释）——激活操作不是异常而是被拒绝，调用方不要依赖激活必然生效。

## 易错改法

1. **给贡献接口加属性**：本模块是解决方案最底层契约，接口加成员会同时破坏所有模块的实现类与 shell 的 mock/测试替身，且 C# 接口无默认实现时编译错误散布全解决方案。改前先全解决方案编译评估。
2. **修「拼写错误」`WindowManagerExtenstion` / `IWindowManagerExtenstion.cs`**（WindowManager/ 目录）：类名与文件名的 "Extenstion" 是既有拼写，所有调用方以该名引用；贸然改名是跨程序集破坏性变更，收益仅是美观。
3. **`ShowWindow<TWindow>(this IWindowManager, object)` 与 `ShowDialog<TWindow>(this IWindowManager, object)` 补泛型约束**（IWindowManagerExtenstion.cs 第 35、43 行）：这两个重载缺 `where TWindow : Window`（同文件其余方法都有），看似「明显疏漏」；补约束是正确修复，但属签名收紧，已用非 Window 类型调用的代码会编译失败。
4. **「统一」`GetWindow` 可空性**：`IWindowManager.GetWindow(Type)`（WindowManager/IWindowManager.cs 第 20 行）返回非空 `Window`，`WindowManagerExtenstion.GetWindow<TWindow>`（第 22 行）返回 `Window?`。把接口也改成 `Window?` 会让所有实现方收到可空性告警；把扩展改成非空则掩盖实现可能返回 null 的事实。
5. **改 `ShellRegions` 常量值**：值经 `nameof` 生成，改标识符即改字符串值；若 shell 布局 XAML 或持久化布局状态中以字符串引用 Region 名，会静默失配（编译期不报错）。
6. **以为 `ShowWindow(Window)` 实例版注释「对话框窗口」是语义**：IWindowManager.cs 中 `ShowWindow(Window window)` 的 XML 注释误写为「显示指定类型的对话框窗口」（与 ShowDialog 注释雷同），实际是非对话框的实例版重载——按注释理解行为会被误导。

## 历史踩坑线索

- `IWindowManagerExtenstion.cs` 类注释自称「窗口管理器扩展方法，提供泛型版本的窗口管理操作」，但文件名前缀是 `I`（`IWindowManagerExtenstion.cs`）而类名无 `I`——文件命名与类命名不一致，按文件名找接口会扑空。
- 防御性线索：泛型扩展层用 `Window?` 返回（`GetWindow<TWindow>`），暗示历史上 `GetWindow(Type)` 实现确实返回过 null，接口的非空标注与实际行为脱节。
- 注释中引用的 `Prism.Ioc.IContainerRegistry`、`ShellLayoutState`、`OpenMainViewEvent` 都不在本程序集（本模块未引用 Prism），`<see cref>` 无法解析——文档注释与程序集引用脱节，说明这些注释是写给实现侧读者的约定，不是编译期可验证的事实。
