# UIPackage — 不变量与陷阱

## 隐含不变量

1. **装载顺序不变量**：必须先在 `Application.Styles` 装入 `WorkstationTheme`，再对 `Application.Resources` 调 `VSCodePalette.ApplyTo`——真实顺序见 `FrameworkApplication.cs:24-27`（先 `RequestedThemeVariant = Dark`，再 `Styles.AddRange`，最后 `ApplyTo(Resources)`）。`ApplyTo` 的注释点明机制："把调色板写入应用级资源，使查找先于各主题包命中"（`VSCodePalette.cs:14`）；若把键写进某个 Style 内部字典或控件级资源，覆盖优先级就失去保证。
2. **`ApplyTo` 的参数必须是应用级 `Resources`**：方法签名只是 `IResourceDictionary`（`VSCodePalette.cs:16`），编译器不阻止你传任何字典，但语义上只有 `Application.Resources` 满足设计意图。
3. **固定 Dark 主题假设**：全部色值是深色专用（`VSCodePalette.cs:9` 注释"随应用固定 Dark 主题启用"）；`RequestedThemeVariant` 被 Framework 固定为 `ThemeVariant.Dark`（`FrameworkApplication.cs:24`）。模块没有任何按主题变体分支的逻辑。
4. **主题包 Add 顺序**：`WorkstationTheme` 构造函数中 `Semi.Avalonia.SemiTheme` 在最先（`WorkstationThemes.cs:12`），Ursa 的 Semi 皮肤、ColorPicker、DataGrid 主题在后（行 13-15）——后三者都建立在 Semi 基础主题之上，调整顺序可能让 Ursa/ColorPicker 控件拿不到基础样式。
5. **包版本同组对齐**：Semi.Avalonia 三个包必须同版本（当前均 11.3.7.3），Irihi.Ursa 两包同版本（当前均 1.15.1），且整体与 Avalonia 11.3.20 匹配（`UIPackage.csproj:10-15`）。单独升级其中一个包是典型的样式丢失/启动崩溃来源。
6. **UI 线程亲和性**：`ApplyTo` 写资源字典、`WorkstationTheme` 挂载都假定在 UI 线程的应用初始化期执行；模块没有任何线程安全措施，不应在后台线程调用。

## 易错改法（看似合理但静默破坏）

1. **`public const string` 内联陷阱**：`Icons` 的全部成员是 `const`（如 `Icons.cs:12`），C# 编译器会把 const 值**拷贝进每个引用程序集的 IL**。只重编 UIPackage 而不重编消费方（DashBoard、Workstation、Framework），旧图标值仍留在消费方程序集中——改了图标"没生效"时先整体 rebuild。
2. **新增/改资源键名不产生任何编译期检查**：`ApplyTo` 用字符串索引器写键（`resources["ChromeStatusBarBackground"] = …`，`VSCodePalette.cs:32`），消费方以 `DynamicResource` 按字符串取键。键名拼错两边都编译通过，运行时 Avalonia 静默回退为 unset——表现为"某块颜色不见了"，无异常无日志。改键名必须全局搜索字符串引用。
3. **把颜色键改成 `Color` 而不是 `SolidColorBrush`**：消费方绑定的是 Brush；`ApplyTo` 里所有颜色都经 `Brush()` 帮助函数包装成 `SolidColorBrush`（`VSCodePalette.cs:45-48`）。绕过帮助函数直接赋 `Color` 或字符串会导致绑定类型不匹配而静默失色。注意键值类型本来就混合（Brush/Thickness/double），新增键时类型须与消费方约定一致。
4. **以为调色板能覆盖一切**：`ApplyTo` 只覆盖它显式写出的约 20 个键；Semi/Ursa 主题里其余成百上千个键仍是默认值。想改一个不在清单内的颜色，必须先确认目标控件实际引用哪个键，再往 `ApplyTo` 加对应覆盖。
5. **运行时反复调用 `ApplyTo` 或反复 `new WorkstationTheme()`**：设计上是一次性启动期写入；重复 `AddRange(new WorkstationTheme())` 会向 `Styles` 叠加重复主题实例，重复 `ApplyTo` 虽只是幂等覆盖同键，但都偏离设计前提且无收益。

## 历史踩坑（代码注释中透露的坑）

- **菜单密度对齐**：`VSCodePalette.cs:37` 注释记录了推算过程——"Semi 默认 16,8 内边距 + 14px 字 ≈ 33px"，而 VS Code 行高 22px，因此 `MenuItemPadding` 设为 `Thickness(16, 1.5)`、`MenuFlyoutFontSize` 设为 `12.0`（行 38-39）。改这两个值前先理解这组换算，否则菜单行高会重新失控。
- **标题栏与窗口同色**：`VSCodePalette.cs:31` 注释说明标题栏直接使用 `SemiColorNavBackground`，"与窗口背景同色"——不要为标题栏另造色键，否则窗口会出现两截颜色。
- **sash 高亮对标 VS Code**：`ChromeSashHoverBrush` 注释指明对应 VS Code 的 `sash.hoverBorder`（`VSCodePalette.cs:35`），取状态栏同款蓝 `#3994BC`；这是有意的品牌一致性而非随手选色。
- **图标集中管理是有意约束**：`Icons.cs:5` 注释"贡献类经本类引用图标，不在各自类中硬编码 path"——绕过本类在消费方内联 path 字符串是被明确禁止的写法。
