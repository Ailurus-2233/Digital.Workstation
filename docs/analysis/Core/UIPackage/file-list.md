# UIPackage — 文件结构与功能

## 目录树（相对 `Core/UIPackage/`）

```
Core/UIPackage/
├── UIPackage.csproj        项目文件：net10.0，6 个 UI 相关 PackageReference，无 ProjectReference
├── WorkstationThemes.cs    应用主题聚合类
├── VSCodePalette.cs        VS Code Dark+ 深色调色板写入器
└── Icons.cs                共享图标 path 常量集
```

模块共 4 个文件，无子目录、无 .axaml/主题资源文件、无测试文件、无 AssemblyInfo。全部源码均为 C#，总代码量约 200 行。

## 逐文件说明

### `UIPackage.csproj`
- 功能：声明项目骨架。`<TargetFramework>net10.0</TargetFramework>`、`ImplicitUsings`/`Nullable` enable（行 4-6）。
- 关键内容：6 个 `PackageReference`（行 10-15）：`Avalonia 11.3.20`、`Semi.Avalonia 11.3.7.3`、`Semi.Avalonia.ColorPicker 11.3.7.3`、`Semi.Avalonia.DataGrid 11.3.7.3`、`Irihi.Ursa 1.15.1`、`Irihi.Ursa.Themes.Semi 1.15.1`。**没有任何 ProjectReference**。

### `WorkstationThemes.cs`（17 行）
- 功能：把四个第三方主题包聚合成一个可整体挂载的应用主题。
- 关键类型：`public class WorkstationTheme : Styles`（行 8）；唯一成员是无参构造函数（行 10-16），依次 `Add(new SemiTheme())`、`Add(new Ursa.Themes.Semi.SemiTheme())`、`Add(new ColorPickerSemiTheme())`、`Add(new DataGridSemiTheme())`。

### `VSCodePalette.cs`（49 行）
- 功能：以 VS Code Dark+ 语义分层覆盖 Semi 语义色键，并提供 chrome 专属色键与菜单弹出层密度/配色键。
- 关键类型：`public static class VSCodePalette`（行 11）。
- 关键入口：`ApplyTo(IResourceDictionary resources)`（行 16-43），写入约 20 个资源键；私有帮助函数 `Brush(string color)`（行 45-48）返回 `new SolidColorBrush(Color.Parse(color))`。
- 文件无状态、无字段，纯启动期写入器。

### `Icons.cs`（108 行）
- 功能：集中存放共享图标的 StreamGeometry path 字符串，供各模块贡献类经 `IconPath => Icons.Xxx` 引用，禁止在各自类中硬编码 path（类注释，行 4-5）。
- 关键类型：`public static class Icons`（行 7），含 19 个 `public const string`：
  `Settings`(12)、`DashBoard`(18)、`Properties`(22)、`Outline`(28)、`Output`(34)、`Log`(40)、`Tasks`(46)、`ChevronDown`(51)、`ChevronRight`(56)、`PanelLeft`(60)、`PanelBottom`(64)、`PanelRight`(70)、`Exit`(75)、`About`(80)、`Ready`(86)、`AlignLeft`(92)、`AlignRight`(97)、`AlignCenter`(102)、`AlignJustify`(107)。
- 每个常量带 XML 注释说明图形含义与预置用途（如 `ChevronDown` 注释为"向下箭头，BottomPanel 收起按钮"）。
