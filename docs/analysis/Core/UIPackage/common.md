# UIPackage — 模块简述

## 模块做什么

`Core/UIPackage`（命名空间 `DigitalWorkstation.Core.UIPackage`，项目文件 `Core/UIPackage/UIPackage.csproj`）是 Digital.Workstation 的**共享 UI 资源包**。它把三件东西收敛到一个无项目依赖的类库中，供 shell 与所有业务模块统一消费：

1. **应用主题聚合**：`WorkstationTheme`（`WorkstationThemes.cs:8`）继承 `Avalonia.Styling.Styles`，在构造函数中依次 `Add` 四个第三方主题包——`Semi.Avalonia.SemiTheme`、`Ursa.Themes.Semi.SemiTheme`、`Semi.Avalonia.ColorPicker.ColorPickerSemiTheme`、`Semi.Avalonia.DataGrid.DataGridSemiTheme`，使消费方一行 `Styles.AddRange(new WorkstationTheme())` 即可装齐全部主题（真实调用点：`Core/Framework/FrameworkApplication.cs:25`）。
2. **产品深色调色板**：`VSCodePalette.ApplyTo(IResourceDictionary)`（`VSCodePalette.cs:16`）以 VS Code Dark+ 的语义分层为蓝本，覆盖 Semi 语义色键（背景/边框/文本/填充三级），并新增 chrome 专属色键（状态栏蓝 `ChromeStatusBarBackground`、`ChromeSashHoverBrush` 等）以及菜单弹出层的密度与配色键（`MenuItemPadding`、`MenuFlyoutBackground` 等）。
3. **共享图标几何**：`Icons`（`Icons.cs:7`）静态类集中存放 15 个 `public const string` 的 StreamGeometry path 字符串（`Settings`、`DashBoard`、`ChevronDown`……），各模块的贡献类（导航项、面板 tab、菜单项、状态栏项）通过 `IconPath => Icons.Xxx` 引用，不在各自类中硬编码 path。

## 核心设计逻辑

- **为什么聚合为一个 Styles 子类**：Avalonia 的主题以 `Styles` 为单位挂载。把四个主题包的 `Add` 调用固化进 `WorkstationTheme` 构造函数（`WorkstationThemes.cs:10-16`），消费方无需知道主题包清单与装载顺序，也避免各处重复 `Add` 漏装某一个。
- **为什么调色板写到应用级资源而非主题内**：`ApplyTo` 的 XML 注释明确说明意图——"把调色板写入应用级资源，使查找先于各主题包命中"（`VSCodePalette.cs:14`）。Avalonia 资源查找沿逻辑树向上先到 `Application.Resources`，再进 `Application.Styles`；把键写进 `Resources`（真实调用 `VSCodePalette.ApplyTo(Resources)`，`FrameworkApplication.cs:27`）即可稳定覆盖主题包自带的同名 Semi 色键，而不必修改第三方主题。
- **为什么图标用 const string 而不是资源字典**：图标由 `PathIcon` 或 `StreamGeometry.Parse` 消费（如 `Modules/Workstation/MainWindowViewModel.cs:91` 的 `StreamGeometry.Parse(Icons.ChevronDown)`），path 字符串是最小公共表示；`const` 让消费方零依赖资源查找、编译期可得。代价是 const 会被内联进引用程序集（见 pitfalls.md）。
- **固定 Dark 主题**：调色板注释写明"随应用固定 Dark 主题启用"（`VSCodePalette.cs:9`）；`FrameworkApplication.cs:24` 先设 `RequestedThemeVariant = ThemeVariant.Dark` 再装载主题与调色板。模块本身不处理亮色变体，所有色值都是深色专用。

## 状态流转

本模块无运行时状态、无事件、无生命周期管理；数据流是**一次性的启动期写入**：

```
FrameworkApplication.Initialize()
  → RequestedThemeVariant = ThemeVariant.Dark        (FrameworkApplication.cs:24)
  → Styles.AddRange(new WorkstationTheme())           (FrameworkApplication.cs:25)
       → 构造函数 Add 4 个主题包                       (WorkstationThemes.cs:12-15)
  → VSCodePalette.ApplyTo(Resources)                  (FrameworkApplication.cs:27)
       → resources["SemiColorBackground0"] = Brush("#121314") 等约 20 个键
       → Brush(color) = new SolidColorBrush(Color.Parse(color))  (VSCodePalette.cs:45-48)
```

之后 Avalonia 样式系统按需读取这些画刷/Thickness/double 资源；`Icons` 的 const 字符串在贡献类构造期被读出并解析为几何。模块自身不持有任何可变字段。

## 常见修改场景

1. **要调整某个界面颜色（如状态栏蓝）**：改 `VSCodePalette.cs` 中 `ApplyTo` 内对应的 `Brush("#RRGGBB")` 字面量（状态栏蓝在 `VSCodePalette.cs:32` `ChromeStatusBarBackground`）。无需改任何控件——消费方通过 DynamicResource/键查找取值。
2. **要新增一个共享图标（如新导航项图标）**：在 `Icons.cs` 追加一个 `public const string Xxx = "M…Z";`（仿照 `Icons.cs:12` `Settings` 的写法与 XML 注释规范），然后贡献类里 `IconPath => Icons.Xxx`。注意 const 内联陷阱（见 pitfalls.md）。
3. **要新增/升级一个第三方主题包**：改 `UIPackage.csproj` 加 `PackageReference`，并在 `WorkstationThemes.cs:10-16` 构造函数中按依赖顺序 `Add(new XxxTheme())`。Semi 基础主题须保持在 Ursa 的 Semi 皮肤之前。
4. **要改菜单弹出层的行高密度**：改 `VSCodePalette.cs:38-39` 的 `MenuItemPadding = new Thickness(16, 1.5)` 与 `MenuFlyoutFontSize = 12.0`（注释解释了取值依据：Semi 默认 16,8 内边距 + 14px 字 ≈ 33px，VS Code 为 22px 行高）。
5. **要支持亮色主题**：这不是改一个文件能完成的——`VSCodePalette` 全部色值为深色写死，需引入按 `ThemeVariant` 分支的第二套色值，并重新审视"固定 Dark"假设（`FrameworkApplication.cs:24`）。
