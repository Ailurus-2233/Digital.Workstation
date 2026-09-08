# Framework — 验证方式

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

改动 `ShellLayoutState` 的任何转换方法、或改动三个区域 record 的 Min/Max/默认值后，必须这 11 个用例全绿（或同步更新断言）。改动 `FrameworkApplication`/`FrameworkWindowManager`/`ShellContributionCollector` 目前**没有任何测试会失败**——它们不在测试覆盖内，靠编译与手工冒烟验证。

## 测试约定（从现有测试归纳）

- **命名**：`方法_场景_期望`，如 `ResizeSideBar_ClampsAtMaxWidth`、`CollapsedSideBar_KeepsResizedWidth_WhenRestored`。
- **结构**：AAA 三段——从 `ShellLayoutState.Initial` 出发，调转换方法得到 `next`，`Assert.Equal` 比较标量或整个 record（record 值相等使 `Assert.Equal(state.AuxiliaryPanel, next.AuxiliaryPanel)` 可直接用）。
- **无夹具、无 mock**：被测对象是纯不可变 record 转换，不需要 `IClassFixture`/Moq；clamp 边界直接引用被测 record 的公开 const（`SideBarState.MaxWidth` 等），不硬编码数字。
- **新增一个测试**：在 `UnitTest/Framework/` 下建 `XxxTests.cs`，`using DigitalWorkstation.Core.Framework.Layout;` + `using Xunit;`，命名空间 `DigitalWorkstation.UnitTest.Framework`，用 `[Fact]` 标注；数据检测类逻辑（如新的状态转换）照此模式即可直接测，不需要任何 UI 宿主。

## C# 桌面端约定：哪些 UI 部分按约定不测

- **不测**：`FrameworkApplication` 启动序列（依赖 Avalonia `ApplicationLifetime`、真实窗口与事件聚合器）、`FrameworkWindowManager` 全部窗口操作（需要真实 `Window` 实例与 UI 线程）、`ConfigureViewModelLocator`（依赖 Avalonia 程序集加载上下文）、`ShellContributionCollector` 之外的 View/ViewModel 绑定与窗口交互。
- **聚焦测**：数据检测——状态读写、转换、校验、计算逻辑。`ShellLayoutState` 一族 record 是当前唯一符合该定位的被测对象；`ShellContributionCollector` 逻辑上可测（容器解析+过滤+排序），但现状无测试，新增时需要能构造 `IContainerProvider` 桩。
