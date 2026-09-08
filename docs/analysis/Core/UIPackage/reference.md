# UIPackage — 模块关系链

## 依赖关系（本模块依赖什么）

**项目依赖：无。** `UIPackage.csproj` 不含任何 `ProjectReference`，是 Core 层最底层的项目之一（与 `Core/Resource` 并列被其他项目引用）。

**NuGet 包依赖**（`UIPackage.csproj:9-16`）：

| 包 | 版本 | 提供的能力 | 模块内使用点 |
|---|---|---|---|
| `Avalonia` | 11.3.20 | `IResourceDictionary`、`Styles`、`SolidColorBrush`/`Color.Parse`、`Thickness` | `VSCodePalette.cs:1-3`、`WorkstationThemes.cs:1` |
| `Semi.Avalonia` | 11.3.7.3 | `SemiTheme` 基础主题及全部 `SemiColorXxx` 语义色键 | `WorkstationThemes.cs:2,12`；`VSCodePalette` 覆盖的键来自该主题 |
| `Semi.Avalonia.ColorPicker` | 11.3.7.3 | `ColorPickerSemiTheme` | `WorkstationThemes.cs:3,14` |
| `Semi.Avalonia.DataGrid` | 11.3.7.3 | `DataGridSemiTheme` | `WorkstationThemes.cs:4,15` |
| `Irihi.Ursa` | 1.15.1 | Ursa 控件库本体（传递依赖） | 间接 |
| `Irihi.Ursa.Themes.Semi` | 1.15.1 | `Ursa.Themes.Semi.SemiTheme`（Ursa 控件的 Semi 皮肤） | `WorkstationThemes.cs:13` |

目标框架 `net10.0`，`ImplicitUsings` 与 `Nullable` 均 enable（`UIPackage.csproj:4-6`）。

## 被依赖关系（谁依赖本模块）

| 消费方 | 引用方式 | 消费内容 |
|---|---|---|
| `Core/Framework/Framework.csproj:13` | ProjectReference | `FrameworkApplication.cs:25` `Styles.AddRange(new WorkstationTheme())`；`FrameworkApplication.cs:27` `VSCodePalette.ApplyTo(Resources)`——主题与调色板的唯一装载点 |
| `Modules/DashBoard/DashBoard.csproj:23` | ProjectReference | `Icons.DashBoard`（`DashBoardNavigationItem.cs:17`、`DashBoardStatusBarItem.cs:17`、`DashBoardMenus.cs:17` 的 `[MenuItem]` `Icon` 命名属性）、`Icons.Tasks`（`DashBoardTasksPanelTab.cs:17`） |
| `Modules/Workstation`（经 `Core/Framework` 传递 + using） | using `DigitalWorkstation.Core.UIPackage` | 几乎全部 shell 贡献类与 attribute 菜单类：`Icons.Settings`（`Contributions/SettingsNavigationItem.cs:17`）、`Icons.Properties/Outline/Output/Log`（`Contributions/` 各 PanelTab.cs:17）、`Icons.Ready`（`Contributions/ReadyStatusBarItem.cs:16`）、`Icons.Exit`（`Menus/FileMenus.cs:18`）、`Icons.About`（`Menus/HelpMenus.cs:17`）、`Icons.PanelLeft/PanelBottom/PanelRight`（`Menus/ViewPanelMenus.cs:14、20、26`）、`Icons.AlignLeft/AlignRight/AlignCenter/AlignJustify`（`Menus/ViewAlignmentMenus.cs:15、21、27、33`，菜单类均为 `[MenuItem]` 的 `Icon` 命名属性）、`StreamGeometry.Parse(Icons.ChevronDown/ChevronRight)`（`MainWindowViewModel.cs:95、100`）；`MainWindowViewModel.cs:11` 直接 using 本命名空间 |

场景归纳：`WorkstationTheme` 与 `VSCodePalette` 只被 Framework 的应用初始化使用一次；`Icons` 被所有模块的贡献类（导航项/面板 tab/状态栏项的 `IconPath` 属性、菜单项的 `[MenuItem]` `Icon` 命名属性）广泛引用。

## 核心内部数据结构

模块无自定义数据结构；关键类型及相互关系：

```
Avalonia.Styling.Styles
  └── WorkstationTheme            (WorkstationThemes.cs:8)  聚合 4 个第三方 Styles

static VSCodePalette              (VSCodePalette.cs:11)
  ├── ApplyTo(IResourceDictionary)(VSCodePalette.cs:16)  写约 20 个资源键
  └── Brush(string)               (VSCodePalette.cs:45)  SolidColorBrush(Color.Parse(color))

static Icons                      (Icons.cs:7)
  └── 15 × public const string    (Icons.cs:12-87)  StreamGeometry path 文本
```

- `WorkstationTheme` 内部持有的只是基类 `Styles` 的子样式列表，无自有字段。
- `VSCodePalette` 写入的值类型混合：`SolidColorBrush`（绝大多数）、`Thickness`（`MenuItemPadding`）、`double`（`MenuFlyoutFontSize`）。
- `Icons` 的 const 与 Core/Resource 的 `Language.XxxTitle` 文案键在贡献类中成对使用（图标 + 标题），但两模块间无代码依赖——配对关系由消费方维持。
