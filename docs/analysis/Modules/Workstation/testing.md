# Workstation — 验证方式

## 本模块的测试在哪

**本模块没有任何测试项目。** 解决方案中唯一的测试项目是 `UnitTest/Framework/Framework.csproj`（xUnit 2.4.2 + Microsoft.NET.Test.Sdk 17.6.0，net10.0），它只测 `Core/Framework` 的 `ShellLayoutState.Resize` 一族转换（`UnitTest/Framework/ShellLayoutStateResizeTests.cs`，11 个 `[Fact]`），不引用、不测试 `Modules/Workstation` 的任何类型（Workstation.csproj 也不在它的 ProjectReference 里）。

## 怎么跑（对本模块相关的部分）

本模块改动的**数据检测**落点在 Framework 的 `ShellLayoutState`——本模块 `MainWindowViewModel` 的全部状态转换（SelectActivity/Toggle*/Activate*/Resize）都只是调用它，因此改布局语义后应跑：

```powershell
# 仓库根目录
dotnet test UnitTest/Framework
# 只跑布局状态测试
dotnet test UnitTest/Framework --filter "FullyQualifiedName~ShellLayoutStateResizeTests"
```

## 改完代码后的最小验证集

本仓库是纯桌面端项目，约定**不做 UI 自动化测试验收**（根 AGENTS.md"验证约定"：本机 `dotnet run` 启动应用手动验证）。本模块的 UI 面（View/ViewModel 绑定、窗口交互、拖拽手势、菜单渲染）按约定跳过单元测试；单元测试聚焦数据检测——状态读写、转换、校验、计算逻辑。

按改动面分：

| 改动 | 验证方式 |
|---|---|
| 布局状态语义（显隐、clamp、tab 激活拒绝） | 实际逻辑在 `ShellLayoutState`：`dotnet test UnitTest/Framework` 全绿（11 用例，检测点映射见 docs/analysis/Core/Framework/testing.md） |
| `MainWindowViewModel` 交互逻辑（贡献收集、缓存、命令转发、布局持久化接线） | 无单元测试覆盖；手动 `dotnet run` 冒烟：启动 → 点 ActivityBar 导航项（SideBar 展开/再点收起；底部段应只见 shell 内置"设置"导航按钮，点击应打开设置页主视图；钉住区为空）→ Ctrl+B/Ctrl+J/Ctrl+Alt+B → 拖三条分隔条（边界应停在 Min/Max）→ 视图菜单三个切换项 → 文件>退出、帮助>关于；持久化：改布局后退出重进应恢复 → 视图菜单 Layout 组"重置布局"后应回全默认且 %AppData%/Digital.Workstation/layout.json 被删除 |
| 工具视图 `[ToolView]` attribute 与菜单类 attribute（Id/Order/图标/文案/默认位置/分组位次） | 手动验证渲染位置与排序（ActivityBar 顶部段/钉住区/两个面板，ADR-0002）；Id 类问题看 error.md 排查表，条目缺席看注册日志（工具视图：非可实例化 `Control`/同程序集重复 Id 被 `ToolViewRegistration` 记 `Logger.Warning` 跳过；菜单：非法签名/空段路径被跳过） |
| MainWindow.axaml 样式/布局 | 纯视觉，启动目验 |

## 测试约定（若将来为本模块新增测试）

沿袭 `UnitTest/Framework` 的约定（详见 docs/analysis/Core/Framework/testing.md）：xUnit、`[Fact]`、命名 `方法_场景_期望`、AAA、无夹具无 mock；数据检测类逻辑（如把某段转换抽成纯函数）才值得测，View/ViewModel 绑定不测。`MainWindowViewModel` 逻辑上可测的部分（`OpenMainView` 的 Id 查找、缓存命中）需要 `IContainerProvider` 与 `IEventAggregator` 桩，现状无此先例。
