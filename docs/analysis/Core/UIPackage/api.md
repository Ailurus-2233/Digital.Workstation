# UIPackage — 对外接口与调用方式

模块共 3 个公开类型，全部位于命名空间 `DigitalWorkstation.Core.UIPackage`。模块无接口、无抽象类、无事件、无服务注册——API 面就是"一个 Styles 子类 + 一个静态写入方法 + 一组 const 字符串"。

## 1. `WorkstationTheme`（`WorkstationThemes.cs:8`）

```csharp
public class WorkstationTheme : Styles
{
    public WorkstationTheme();   // 构造函数内 Add 四个主题包
}
```

- **输入**：无参数。**输出**：一个已填充的 `Styles` 集合，按序包含 `SemiTheme`（Semi.Avalonia）、`Ursa.Themes.Semi.SemiTheme`、`ColorPickerSemiTheme`、`DataGridSemiTheme`（`WorkstationThemes.cs:12-15`）。
- **典型调用**（真实代码，`Core/Framework/FrameworkApplication.cs:25`）：

```csharp
RequestedThemeVariant = ThemeVariant.Dark;   // 先固定 Dark
Styles.AddRange(new WorkstationTheme());      // 装载主题
VSCodePalette.ApplyTo(Resources);             // 再覆盖调色板
```

- **生命周期要求**：必须在应用初始化期（`Application.Initialize()` 内、`base.Initialize()` 之前）加入 `Application.Styles`；它不是为运行时反复创建/销毁设计的。

## 2. `VSCodePalette`（`VSCodePalette.cs:11`）

```csharp
public static class VSCodePalette
{
    public static void ApplyTo(IResourceDictionary resources);
}
```

- **输入**：任意 `IResourceDictionary`，设计上要求传**应用级资源**（`Application.Resources`），注释明确"使查找先于各主题包命中"（`VSCodePalette.cs:14`）。传入其他层级的字典虽能编译运行，但覆盖优先级可能达不到预期（见 pitfalls.md）。
- **输出**：无返回值；副作用是向字典写入约 20 个键，分四组：

| 组 | 键 | 值 | 语义（注释出处） |
|---|---|---|---|
| Semi 语义背景 | `SemiColorBackground0/1/2` | `#121314` / `#191A1B` / `#252526` | 主面板底色 / ActivityBar·SideBar·面板底色 / 弹出层抬升表面（`VSCodePalette.cs:19-21`） |
| Semi 边框/导航 | `SemiColorBorder`、`SemiColorNavBackground` | `#2B2B2B`、`#191A1B` | 区域分隔线；标题栏直接复用 NavBackground 与窗口同色（`VSCodePalette.cs:22,23,31`） |
| Semi 文本三级 | `SemiColorText0/1/2` | `#CCCCCC` / `#9D9D9D` / `#858585` | 主要 / 次要 / 辅助文本（`VSCodePalette.cs:24-26`） |
| Semi 填充 | `SemiColorFill1/2` | `#2A2D2E` / `#37373D` | 悬停填充 / 选中·按下填充（`VSCodePalette.cs:27-28`） |
| 标题栏 | `CaptionButtonForeground` | `#CCCCCC` | 标题栏窗管按钮（`VSCodePalette.cs:29`） |
| chrome 专属 | `ChromeStatusBarBackground`、`ChromeStatusBarForeground`、`ChromeActivityBarItemActiveForeground`、`ChromeSashHoverBrush` | `#3994BC`、`#FFFFFF`、`#FFFFFF`、`#3994BC` | 状态栏蓝及其前景、ActivityBar 活动前景、面板分隔条悬停高亮（`VSCodePalette.cs:32-35`） |
| 菜单密度 | `MenuItemPadding`、`MenuFlyoutFontSize` | `Thickness(16, 1.5)`、`12.0` | 对齐 VS Code 22px 行高（`VSCodePalette.cs:37-39`） |
| 菜单配色 | `MenuFlyoutBackground`、`MenuFlyoutBorderBrush` | `#1F1F1F`、`#454545` | 弹出层底色与边线（`VSCodePalette.cs:41-42`） |

- 所有颜色经私有帮助函数 `Brush(string color)`（`VSCodePalette.cs:45-48`）转为 `SolidColorBrush(Color.Parse(color))`；键的值类型因此不统一（Brush / Thickness / double 混合），消费方按各键约定类型取用。

## 3. `Icons`（`Icons.cs:7`）

```csharp
public static class Icons
{
    public const string Settings  = "M12 15.5A3.5 …";   // 设置齿轮（Icons.cs:12）
    public const string DashBoard = "M3 3h8v8H3V3…";    // 四宫格启动台（Icons.cs:18）
    public const string Properties= "M3 17v2h6v-2…";    // 滑杆（Icons.cs:22）
    public const string Outline   = "M5 9.5 7.5 14…";   // 层级列表（Icons.cs:28）
    public const string Output    = "M20 19V7H4v12…";   // 终端（Icons.cs:34）
    public const string Log       = "M6 2a2 2 0 0 0…";  // 文本文件（Icons.cs:40）
    public const string Tasks     = "M19 3h-4.18…";     // 勾选清单（Icons.cs:46）
    public const string ChevronDown  = "M7.41 8.58…";   // 向下箭头（Icons.cs:51）
    public const string ChevronRight = "M8.59 16.58…";  // 向右箭头（Icons.cs:56）
    public const string PanelLeft    = "M20 3H4a2 2…";  // 左侧面板（Icons.cs:60）
    public const string PanelBottom  = "M4 3h16a2 2…";  // 底部面板（Icons.cs:64）
    public const string PanelRight   = "M4 3h16a2 2…";  // 右侧面板（Icons.cs:70）
    public const string Exit    = "M19 6.41 17.59…";    // 关闭叉号（Icons.cs:75）
    public const string About   = "M11 9h2V7h-2…";      // 信息圆圈（Icons.cs:80）
    public const string Ready   = "M12 2C6.5 2 2 6.5…"; // 勾选圆圈（Icons.cs:86）
    public const string AlignLeft   = "M3 3h18v2H3V3…";   // 左对齐横线组（Icons.cs:92）
    public const string AlignRight  = "M3 3h18v2H3V3…";   // 右对齐横线组（Icons.cs:97）
    public const string AlignCenter = "M3 3h18v2H3V3…";   // 居中横线组（Icons.cs:102）
    public const string AlignJustify= "M3 3h18v2H3V3…";   // 两端对齐横线组（Icons.cs:107）
}
```

- **数据语义**：每个 const 是 SVG/StreamGeometry 兼容的 path 标记字符串（24×24 视窗的 Material Design 图标风格），类注释说明"由 PathIcon 消费并随主题变色"（`Icons.cs:4`）。
- **两种真实消费方式**：
  1. 贡献类暴露 path 字符串：`public string IconPath => Icons.DashBoard;`（`Modules/DashBoard/DashBoardStatusBarItem.cs:17`；同型还有 `Modules/Workstation/Contributions/ReadyStatusBarItem.cs:16`）；工具视图经 `[ToolView]` attribute 的 `Icon` 命名属性引用（`Modules/DashBoard/Views/DashBoardNavigationView.axaml.cs:14`、`Modules/Workstation/Views/OutputView.axaml.cs:10` 等，ADR-0002）；菜单项经 `[MenuItem]` attribute 的 `Icon` 命名属性引用（`Modules/Workstation/Menus/FileMenus.cs:18` 等）。
  2. ViewModel 直接解析为几何：`StreamGeometry.Parse(Icons.ChevronDown)`（`Modules/Workstation/MainWindowViewModel.cs:136,141,146`——两个面板收起按钮与"设置"导航按钮 `SettingsIcon`，ADR-0006）。
- **约定**：注释要求"贡献类经本类引用图标，不在各自类中硬编码 path"（`Icons.cs:5`）。`Icons.Xxx` 与本地化文案 `Language.XxxTitle`（Core/Resource 模块）在贡献类中成对出现。

## 对外公开的数据结构

模块不定义任何结构体/记录/DTO；唯一的"数据结构"是 `ApplyTo` 写入的资源键集合（见上表）——这些键名本身就是跨模块契约，chrome 专属四键（`ChromeStatusBarBackground` 等）由 shell 的样式以 `DynamicResource` 引用。
