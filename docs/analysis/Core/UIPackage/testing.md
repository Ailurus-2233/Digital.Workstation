# UIPackage — 验证方式

## 现状：本模块没有任何测试

- 仓库中没有针对 `Core/UIPackage` 的测试项目或测试目录；`Digital.Workstation.slnx` 的 `/Core/` 文件夹只列了 `Models`、`Resource`、`UIPackage` 等项目，无测试项目引用本模块。
- 模块内 4 个文件（`UIPackage.csproj`、`WorkstationThemes.cs`、`VSCodePalette.cs`、`Icons.cs`）不含任何测试代码。

## 为什么没有测试（仓库约定）

本仓库是纯 Avalonia 桌面端，约定**不做 UI 自动化测试验收**，单元测试聚焦数据检测（数据读写、转换、校验、计算逻辑）。UIPackage 的全部内容是 UI 资源：主题包装载（`WorkstationTheme`）、画刷/Thickness 资源写入（`VSCodePalette.ApplyTo`）、图标 path 文本（`Icons`）——没有可脱离 UI 独立断言的数据逻辑，因此按约定无测试是如实状态而非遗漏。

## 改完代码后的最小验证集（冒烟验证）

改本模块后用以下人工/冒烟步骤验证，而不是跑测试：

1. **编译**：`dotnet build Core/UIPackage/UIPackage.csproj`——主题包 API 变更（如升级 Semi.Avalonia 后 `SemiTheme` 构造签名变化）会在编译期暴露。
2. **启动应用**：主题与调色板在 `FrameworkApplication.Initialize`（`Core/Framework/FrameworkApplication.cs:24-27`）装载，任何 `Color.Parse` 格式错误或主题包构造失败都会在启动期立刻抛异常——能正常进主窗口即覆盖了 `WorkstationTheme` 与 `ApplyTo` 的全部执行路径。
3. **目视检查数据点**：
   - 状态栏是否为 `#3994BC` 蓝（`ChromeStatusBarBackground`，`VSCodePalette.cs:32`）；
   - 菜单弹出层行高是否约 22px（`MenuItemPadding = Thickness(16,1.5)` + `MenuFlyoutFontSize = 12.0`，`VSCodePalette.cs:38-39`）；
   - 弹出层底色/边线是否 `#1F1F1F`/`#454545`（`VSCodePalette.cs:41-42`）；
   - 面板分隔条悬停是否变蓝（`ChromeSashHoverBrush`，`VSCodePalette.cs:35`）。
4. **图标改动**：触达引用该图标的界面（导航项/面板 tab/菜单项/收起按钮），确认图标渲染且不抛 path 解析异常；若消费方在另一程序集，须整体 rebuild（const 内联，见 pitfalls.md）。

## 若将来要加测试

按仓库约定只值得测**数据检测**类内容，可测点例如：反射枚举 `Icons` 的全部 `public const string` 并断言每个都能被 `StreamGeometry.Parse` 无异常解析（path 文本的数据校验）；或对 `VSCodePalette.ApplyTo` 传入空字典后断言约定键（如 `ChromeStatusBarBackground`）已写入且值类型正确。View/主题装载效果仍按约定不做 UI 自动化。命名/夹具/mock 边界目前仓库无本模块先例可循，新增时应对齐仓库届时其他 Core 模块测试项目的既有约定。
