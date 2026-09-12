# UIPackage — 术语表

## 模块特有术语与缩写

| 术语 | 定义 | 首次出现位置 |
|---|---|---|
| **UIPackage** | 项目/命名空间名（`DigitalWorkstation.Core.UIPackage`），指"UI 资源包"这一件事：主题聚合 + 调色板 + 图标常量的集合。不是泛指"UI 相关的包"，也不是控件库 | `UIPackage.csproj`；`WorkstationThemes.cs:6` |
| **WorkstationTheme** | 本模块定义的应用主题类（`Styles` 子类），聚合 Semi + Ursa + ColorPicker + DataGrid 四个第三方主题。是"应用唯一主题"的代名词 | `WorkstationThemes.cs:8` |
| **Semi 语义色键** | Semi.Avalonia 主题定义的 `SemiColorXxx` 资源键体系（背景 0-2 级、文本 0-2 级、Fill 1-2 级、Border、NavBackground）。本模块不定义这些键，只**覆盖**它们的值 | `VSCodePalette.cs:19-28` |
| **chrome 专属色键** | 本模块自创的 `ChromeXxx` 前缀资源键（`ChromeStatusBarBackground` 等 4 个），指窗口"边框件"（状态栏、ActivityBar、分隔条）的配色，借用了浏览器 chrome 的含义 | `VSCodePalette.cs:31-35` |
| **VS Code Dark+** | 微软 VS Code 默认深色主题；本模块调色板的取色蓝本与语义分层参照物 | `VSCodePalette.cs:8` |
| **sash** | 可拖拽的面板分隔条。`ChromeSashHoverBrush` 注释直接对标 VS Code 的 `sash.hoverBorder` 主题键 | `VSCodePalette.cs:35` |
| **ActivityBar** | VS Code 式窗口最左侧的窄图标栏（导航项容器）；`ChromeActivityBarItemActiveForeground` 服务于此 | `VSCodePalette.cs:20,26,34` |
| **StreamGeometry path 字符串** | SVG path 标记语法文本（`M…L…Z`），`Icons` 常量的值类型；可被 `StreamGeometry.Parse` 解析或由 `PathIcon.Data` 消费 | `Icons.cs:4,12` |
| **PathIcon** | Avalonia 控件，用几何数据渲染图标并随前景色（主题）变色——图标"随主题变色"的机制 | `Icons.cs:4`（类注释） |
| **贡献类（Contribution）** | 模块向 shell 贡献界面条目的类或 attribute 声明（如 `ReadyStatusBarItem`、`Views/` 下的 `[ToolView]` View 类），经 `IconPath => Icons.Xxx` 或 attribute 的 `Icon` 命名属性消费本模块图标 | `Icons.cs:5`（类注释） |

## 与同名通用概念的区别

- **"主题（Theme）"**：本模块的 `WorkstationTheme` 不是"一套配色"，而是一个 `Styles` 容器类——配色部分由 `VSCodePalette` 单独负责（写进应用级资源），主题类只负责控件样式模板。两者合起来才是完整观感。
- **"Icons"**：不是图片文件也不是字体图标，而是 path 标记**字符串常量**；渲染由消费方的 `PathIcon`/`StreamGeometry.Parse` 完成，本模块不参与绘制。
- **"chrome"**：此处指窗口边框件（状态栏/标题栏/分隔条），不是 Chrome 浏览器，也不是 Avalonia 的 Chrome 包。
- **"Dark"**：固定主题指 `ThemeVariant.Dark`（由 `FrameworkApplication.cs:24` 设置），模块内不出现该词以外的主题切换逻辑；`VSCodePalette` 不是"深色模式支持"，而是"仅深色"。
- **`MenuItemPadding`**：写入的值 `Thickness(16, 1.5)` 是 Avalonia 的 `Thickness`（左,上,右,下 省略为左右,上下两参），不要误读为四个方向均为 16/1.5。

## 类名 ↔ 业务概念对照

| 类/成员 | 业务概念 |
|---|---|
| `WorkstationTheme`（`WorkstationThemes.cs:8`） | 应用整体皮肤：一行装载全部第三方控件主题 |
| `VSCodePalette.ApplyTo`（`VSCodePalette.cs:16`） | 产品品牌深色配色的一次性写入 |
| `Icons.Settings`（`Icons.cs:12`） | shell 预置"设置"导航项的齿轮图标 |
| `Icons.DashBoard`（`Icons.cs:18`） | DashBoard 启动台（四宫格） |
| `Icons.ChevronDown` / `ChevronRight`（`Icons.cs:22,27`） | BottomPanel / AuxiliaryPanel 的收起按钮箭头 |
| `Icons.PanelLeft` / `PanelBottom` / `PanelRight`（`Icons.cs:31-41`） | 视图菜单中 SideBar / BottomPanel / AuxiliaryPanel 显隐切换项 |
| `Icons.Exit` / `About`（`Icons.cs:46,51`） | 文件菜单"退出" / 帮助菜单"关于" |
| `Icons.Ready`（`Icons.cs:57`） | shell 预置状态栏"就绪"项的勾选圆圈 |
