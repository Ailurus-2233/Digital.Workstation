# Framework — 验证方式

## 本地化与稳定标识验收

现有 resize 测试不覆盖此链路。集成完成后用临时 smoke 场景调用真实 ResourceText、扫描器、收集器与建树器，完成后移除临时场景：

- 用两个资源所属类型的同名键验证查找隔离；检查 en-US 与中性中文、缺键返回原键，枚举缺键返回完整组合键。
- 同稳定菜单路径先给无标题引用、再给所有者标题，及先给子路径、再给父路径标题；最终应采用首个非 null 末端标题，后到较小位次仍生效。不同路径同键/同译文不得合并，未声明祖先显示 Id。
- 设置组按 Id 合并，保留首个来源/名称而取最小 Order；不同 Id 同名称不得归并。无分组声明与零注册具体类都不应产生幽灵条目；只引用的组直接显示 Id，排序按 Order/Id，设置项仍按 Order/Name。
- 启动实际应用，切换语言后重启，检查产品名、菜单、命令面板、工具视图与设置页；当前进程不做热切换。菜单引用模块无需导入所有者资源。

## 命令面板快捷键标签手动回归

按 `docs/agents/verification.md`，启动 `dotnet run --project Launcher/Launcher.csproj -c Debug`，不以自动化测试验收此 UI 改动：

1. 按 Ctrl+P 打开命令面板，查看“首选项”，右侧应显示 `Ctrl+,`，不得显示 `Ctrl+OemComma`。
2. 查看三个面板切换命令，标签仍为 `Ctrl+B`、`Ctrl+J`、`Ctrl+Alt+B`；“回到主页”无快捷键标签。
3. 按 Esc 关闭命令面板，再按 Ctrl+,，应打开设置页。重新打开命令面板，标签仍应正确。

原因：`Gesture` 是按键声明；`CommandPalette.FormatGesture` 必须使用 Avalonia 平台格式化生成显示文案，不直接输出声明字符串。最终验收由用户确认。

## 多屏最大化菜单手动回归

启动 `dotnet run --project Launcher/Launcher.csproj -c Debug`，使用 Windows 扩展桌面，不需要硬件或仿真器：

1. 将主窗口移到右侧屏幕并最大化，点击“文件”；整份菜单应显示在该屏幕内、按钮下方，不能跳到左屏。
2. 切换“视图”“帮助”，应正常向下展开；按 Esc 或点击菜单外部应关闭菜单。
3. 还原窗口，在同一屏幕内移动后重新打开“文件”；菜单应仍贴合按钮下方。
4. 移到左侧屏幕最大化，重复“文件”；菜单不能越过该屏幕边界。
5. 若有上下排列或不同缩放比例的屏幕，在各屏幕重复以上操作；不得依赖固定像素补偿。

诊断时可用 UI Automation 比较菜单项 BoundingRectangle 与 Screen Bounds；坐标检查不替代最终视觉验收。根因与定位约束见 pitfalls.md“最大化菜单跨屏”。最终验收由用户确认。

## 测试在哪、用什么框架

测试项目：`UnitTest/Framework/Framework.csproj`（解决方案 `Digital.Workstation.slnx` 的 `/UnitTest/` 文件夹，第 15 行），是**全仓唯一的测试项目**。

框架：**xUnit 2.4.2** + `Microsoft.NET.Test.Sdk` 17.6.0 + `xunit.runner.visualstudio` 2.4.5（`UnitTest/Framework/Framework.csproj:11-13`），目标框架 `net10.0`，直接 ProjectReference 被测项目 `..\..\Core\Framework\Framework.csproj`（第 17 行）。

测试源码只有一个文件：`UnitTest/Framework/ShellLayoutStateResizeTests.cs`，11 个 `[Fact]`，全部针对 `ShellLayoutState.Resize` 及其与 `Toggle*` 的交互。

## 怎么跑

```powershell
# 仓库根目录
dotnet test UnitTest/Framework
# 或显式项目文件
dotnet test UnitTest/Framework/Framework.csproj
```

筛选只跑某个测试类/方法：

```powershell
dotnet test UnitTest/Framework --filter "FullyQualifiedName~ShellLayoutStateResizeTests"
dotnet test UnitTest/Framework --filter "FullyQualifiedName~ResizeSideBar_ClampsAtMaxWidth"
```

## 改完代码后的最小验证集

整个 `ShellLayoutStateResizeTests` 类（11 个用例）即最小验证集——目前它是本模块**唯一**被测试覆盖的行为面。按数据检测点分组：

| 检测点 | 用例 |
|---|---|
| Resize 按 delta 增减宽度/高度 | `ResizeSideBar_IncreasesWidthByDelta`、`ResizeSideBar_DecreasesWidthByDelta`、`ResizeAuxiliaryPanel_AdjustsWidthAndClampsToLimits`、`ResizeBottomPanel_AdjustsHeightAndClampsToLimits` |
| clamp 边界取自各 record 的 Min/Max 常量 | `ResizeSideBar_ClampsAtMaxWidth`（`SideBarState.MaxWidth`）、`ResizeSideBar_ClampsAtMinWidth`（`SideBarState.MinWidth`）、上面两个 Adjusts 用例同时验证 AuxiliaryPanel 的 Min/MaxWidth 与 BottomPanel 的 Min/MaxHeight |
| Resize 只动目标区域 | `ResizeSideBar_LeavesOtherRegionsUntouched`（断言 AuxiliaryPanel/BottomPanel/MainContent/SelectedActivity 值相等）、`ResizeBottomPanel_LeavesWidthsUntouched` |
| 收起→恢复后尺寸保留 | `CollapsedSideBar_KeepsResizedWidth_WhenRestored`、`CollapsedBottomPanel_KeepsResizedHeight_WhenRestored`、`CollapsedAuxiliaryPanel_KeepsResizedWidth_WhenRestored` |

改动 `ShellLayoutState` 的任何转换方法、或改动三个区域 record 的 Min/Max/默认值后，必须这 11 个用例全绿（或同步更新断言）。改动 `FrameworkApplication`/`FrameworkWindowManager`/`ShellContributionCollector`/`ToolViewRegistration`/`ShellLayoutDto`/`LayoutPersistence` 目前**没有任何测试会失败**——它们不在测试覆盖内，靠编译与手工冒烟验证（`LayoutPersistence` 涉及真实文件系统与 Timer 防抖，属下方"按约定不测"一类）。

## 测试约定（从现有测试归纳）

- **命名**：`方法_场景_期望`，如 `ResizeSideBar_ClampsAtMaxWidth`、`CollapsedSideBar_KeepsResizedWidth_WhenRestored`。
- **结构**：AAA 三段——从 `ShellLayoutState.Initial` 出发，调转换方法得到 `next`，`Assert.Equal` 比较标量或整个 record（record 值相等使 `Assert.Equal(state.AuxiliaryPanel, next.AuxiliaryPanel)` 可直接用）。
- **无夹具、无 mock**：被测对象是纯不可变 record 转换，不需要 `IClassFixture`/Moq；clamp 边界直接引用被测 record 的公开 const（`SideBarState.MaxWidth` 等），不硬编码数字。
- **新增一个测试**：在 `UnitTest/Framework/` 下建 `XxxTests.cs`，`using DigitalWorkstation.Core.Framework.Layout;` + `using Xunit;`，命名空间 `DigitalWorkstation.UnitTest.Framework`，用 `[Fact]` 标注；数据检测类逻辑（如新的状态转换）照此模式即可直接测，不需要任何 UI 宿主。

## C# 桌面端约定：哪些 UI 部分按约定不测

- **不测**：`FrameworkApplication` 启动序列（依赖 Avalonia `ApplicationLifetime`、真实窗口与事件聚合器）、`FrameworkWindowManager` 全部窗口操作（需要真实 `Window` 实例与 UI 线程）、`ConfigureViewModelLocator`（依赖 Avalonia 程序集加载上下文）、`ShellContributionCollector` 之外的 View/ViewModel 绑定与窗口交互。
- **聚焦测**：数据检测——状态读写、转换、校验、计算逻辑。`ShellLayoutState` 一族 record 是当前唯一符合该定位的被测对象；`ShellContributionCollector` 逻辑上可测（容器解析+排序），`ToolViewRegistration` 的扫描/跳过规则同理（反射+容器注册），但现状均无测试，新增时需要能构造 `IContainerProvider`/`IContainerRegistry` 桩。
